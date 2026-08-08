using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nomina.Application.Exceptions;
using Nomina.Application.Interfaces;
using Nomina.Domain.Enums;

namespace Nomina.Infrastructure.Contabilidad;

/// <summary>
/// Cliente REST del Sistema de Contabilidad.
/// Contrato: POST /api/entradas con una sola línea; ellos generan el débito y el
/// crédito, el número de asiento, la fecha y el estado.
/// </summary>
internal class ContabilidadHttpClient : IContabilidadClient
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ContabilidadSettings _settings;
    private readonly CuentasContablesCache _cache;
    private readonly ILogger<ContabilidadHttpClient> _logger;

    public ContabilidadHttpClient(
        HttpClient http,
        IOptions<ContabilidadSettings> settings,
        CuentasContablesCache cache,
        ILogger<ContabilidadHttpClient> logger)
    {
        _http = http;
        _settings = settings.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<CuentasAsiento> ResolverCuentasAsync(TipoTransaccion tipo, CancellationToken ct = default)
    {
        var c = _settings.Cuentas;

        // Opción acordada con Contabilidad mientras no exista una cuenta de
        // retenciones propia:
        //   Ingreso   -> DB Gasto de Nómina      / CR Nómina por Pagar
        //   Deducción -> DB Nómina por Pagar     / CR Cuentas por Pagar
        // La deducción no es gasto de la empresa: reduce el neto a pagar al
        // empleado y crea una obligación con un tercero.
        var (codigoDebito, codigoCredito) = tipo == TipoTransaccion.Ingreso
            ? (c.GastoNomina, c.NominaPorPagar)
            : (c.NominaPorPagar, c.RetencionesPorPagar);

        var catalogo = await ObtenerCatalogoAsync(false, ct);

        if (!catalogo.ContainsKey(codigoDebito) || !catalogo.ContainsKey(codigoCredito))
        {
            // Puede que Contabilidad haya creado la cuenta después de que cacheamos.
            catalogo = await ObtenerCatalogoAsync(true, ct);
        }

        var debito = Buscar(catalogo, codigoDebito);
        var credito = Buscar(catalogo, codigoCredito);

        return new CuentasAsiento(
            debito.Id, codigoDebito, debito.Nombre ?? string.Empty,
            credito.Id, codigoCredito, credito.Nombre ?? string.Empty);
    }

    public async Task<AsientoRegistradoResponse> RegistrarAsientoAsync(
        int cuentaDebitoId,
        int cuentaCreditoId,
        string descripcion,
        decimal monto,
        CancellationToken ct = default)
    {
        // El contrato no incluye fecha: Contabilidad estampa la del sistema y
        // descarta en silencio cualquier campo que no conozca.
        var payload = new EntradaRequest(
            _settings.AuxiliarId, cuentaDebitoId, cuentaCreditoId, descripcion, monto);

        HttpResponseMessage res;
        try
        {
            res = await EnviarConReintentosAsync(payload, ct);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new DomainValidationException(
                "El Sistema de Contabilidad no respondió a tiempo. Su servidor gratuito puede tardar " +
                "más de un minuto en despertar; intente nuevamente.", 504);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Fallo de red al registrar el asiento en Contabilidad.");
            throw new DomainValidationException(
                $"No se pudo contactar al Sistema de Contabilidad: {ex.Message}", 502);
        }

        var body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
            throw new DomainValidationException(ExtraerError(body, res.StatusCode), 502);

        var dto = Deserializar<EntradaResponse>(body)
            ?? throw new DomainValidationException(
                "Contabilidad aceptó el asiento pero devolvió una respuesta ilegible.", 502);

        return new AsientoRegistradoResponse(
            dto.NumeroAsiento,
            dto.Fecha ?? DateTime.Today,
            dto.Estado ?? string.Empty,
            dto.Mensaje ?? string.Empty);
    }

    private async Task<HttpResponseMessage> EnviarConReintentosAsync(EntradaRequest payload, CancellationToken ct)
    {
        var intentos = Math.Max(1, _settings.ReintentosEnvio);
        for (var i = 1; ; i++)
        {
            try
            {
                return await _http.PostAsJsonAsync("/api/entradas", payload, JsonOpts, ct);
            }
            catch (Exception ex) when (i < intentos && ex is HttpRequestException or TaskCanceledException)
            {
                // Solo se reintenta cuando no hubo respuesta. Un 4xx/5xx sí llega
                // aquí como respuesta y no se reintenta: reenviar un asiento
                // aceptado lo duplicaría, y Contabilidad no controla duplicados.
                if (ct.IsCancellationRequested) throw;
                _logger.LogWarning(ex, "Intento {Intento} de envío a Contabilidad falló; reintentando.", i);
            }
        }
    }

    private async Task<Dictionary<string, CuentaContable>> ObtenerCatalogoAsync(bool forzar, CancellationToken ct)
    {
        if (!forzar && _cache.TryGet(out var cacheado)) return cacheado;

        List<CuentaContable>? cuentas;
        try
        {
            cuentas = await _http.GetFromJsonAsync<List<CuentaContable>>("/api/cuentas", JsonOpts, ct);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new DomainValidationException(
                "El Sistema de Contabilidad no respondió al consultar el catálogo de cuentas.", 504);
        }
        catch (HttpRequestException ex)
        {
            throw new DomainValidationException(
                $"No se pudo consultar el catálogo de cuentas de Contabilidad: {ex.Message}", 502);
        }

        if (cuentas is null || cuentas.Count == 0)
            throw new DomainValidationException("Contabilidad devolvió un catálogo de cuentas vacío.", 502);

        var mapa = cuentas
            .Where(c => !string.IsNullOrWhiteSpace(c.Codigo))
            .GroupBy(c => c.Codigo!)
            .ToDictionary(g => g.Key, g => g.First());

        _cache.Set(mapa);
        return mapa;
    }

    private static CuentaContable Buscar(IReadOnlyDictionary<string, CuentaContable> catalogo, string codigo)
    {
        if (!catalogo.TryGetValue(codigo, out var cuenta))
            throw new DomainValidationException(
                $"La cuenta con código {codigo} no existe en el catálogo de Contabilidad. " +
                "Verifique la configuración o pida al equipo de Contabilidad que la cree.", 502);

        if (!cuenta.PermiteTransacciones)
            throw new DomainValidationException(
                $"La cuenta {codigo} ({cuenta.Nombre}) no permite transacciones.", 502);

        return cuenta;
    }

    private static string ExtraerError(string body, System.Net.HttpStatusCode status)
    {
        try
        {
            var err = Deserializar<EntradaErrorResponse>(body);
            if (err?.Errores is { Count: > 0 })
                return "Contabilidad rechazó el asiento: " + string.Join("; ", err.Errores);
        }
        catch (JsonException)
        {
            /* respuesta no-JSON: cae al mensaje genérico */
        }

        return $"Contabilidad respondió {(int)status} al registrar el asiento.";
    }

    private static T? Deserializar<T>(string body) => JsonSerializer.Deserialize<T>(body, JsonOpts);

    private record EntradaRequest(
        int AuxiliarId,
        int CuentaDebitoId,
        int CuentaCreditoId,
        string Descripcion,
        decimal Monto);

    private record EntradaResponse(
        int NumeroAsiento,
        DateTime? Fecha,
        string? Descripcion,
        string? Auxiliar,
        string? CuentaDebito,
        string? CuentaCredito,
        decimal Monto,
        string? Estado,
        string? Mensaje);

    private record EntradaErrorResponse(
        [property: JsonPropertyName("status")] int Status,
        [property: JsonPropertyName("errores")] List<string>? Errores);
}
