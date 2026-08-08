using Nomina.Application.DTOs;
using Nomina.Application.Exceptions;
using Nomina.Application.Interfaces;
using Nomina.Domain.Entities;
using Nomina.Domain.Enums;

namespace Nomina.Application.Services;

public class AsientoContableService : IAsientoContableService
{
    private readonly IAsientoContableRepository _repo;
    private readonly IContabilidadClient _contabilidad;

    public AsientoContableService(IAsientoContableRepository repo, IContabilidadClient contabilidad)
    {
        _repo = repo;
        _contabilidad = contabilidad;
    }

    public async Task<List<AsientoPreviewDto>> PreviewAsync(int anio, int mes, CancellationToken ct = default)
    {
        ValidarPeriodo(anio, mes);

        var pendientes = await _repo.GetTransaccionesSinContabilizarAsync(anio, mes, ct);
        var existentes = await _repo.ListByPeriodoAsync(anio, mes, ct);
        var (ingresos, deducciones) = await GetCatalogosAsync(ct);

        var preview = new List<AsientoPreviewDto>();
        foreach (var grupo in Agrupar(pendientes))
        {
            var nombre = NombreConcepto(grupo.Tipo, grupo.ConceptoId, ingresos, deducciones);
            var complementario = HayEnvioPrevio(existentes, grupo.Tipo, grupo.ConceptoId);
            var fallido = BuscarFallido(existentes, grupo.Tipo, grupo.ConceptoId);

            preview.Add(new AsientoPreviewDto(
                grupo.Tipo,
                grupo.ConceptoId,
                nombre,
                grupo.Monto,
                grupo.Transacciones.Count,
                ConstruirDescripcion(anio, mes, grupo.Tipo, nombre, complementario),
                complementario,
                fallido?.MensajeError));
        }

        return preview
            .OrderBy(p => p.TipoTransaccion)
            .ThenBy(p => p.ConceptoNombre)
            .ToList();
    }

    public async Task<List<AsientoContableDto>> EnviarPeriodoAsync(int anio, int mes, CancellationToken ct = default)
    {
        ValidarPeriodo(anio, mes);

        var pendientes = await _repo.GetTransaccionesSinContabilizarAsync(anio, mes, ct);
        if (pendientes.Count == 0)
            throw new DomainValidationException(
                "El período no tiene transacciones pendientes de contabilizar.");

        var existentes = await _repo.ListByPeriodoAsync(anio, mes, ct);
        var (ingresos, deducciones) = await GetCatalogosAsync(ct);

        var procesados = new List<AsientoContable>();

        foreach (var grupo in Agrupar(pendientes))
        {
            var nombre = NombreConcepto(grupo.Tipo, grupo.ConceptoId, ingresos, deducciones);
            var complementario = HayEnvioPrevio(existentes, grupo.Tipo, grupo.ConceptoId);
            var cuentas = await _contabilidad.ResolverCuentasAsync(grupo.Tipo, ct);

            // Un intento anterior que quedó fallido se reutiliza en vez de acumular
            // registros muertos. Sus transacciones siguen sin marcar, así que el
            // monto se recalcula solo.
            var asiento = BuscarFallido(existentes, grupo.Tipo, grupo.ConceptoId);
            var esNuevo = asiento is null;
            asiento ??= new AsientoContable
            {
                Anio = anio,
                Mes = mes,
                TipoTransaccion = grupo.Tipo,
                ConceptoId = grupo.ConceptoId,
                Estado = EstadoRegistro.Activo
            };

            asiento.ConceptoNombre = nombre;
            asiento.Descripcion = ConstruirDescripcion(anio, mes, grupo.Tipo, nombre, complementario);
            asiento.Monto = grupo.Monto;
            asiento.CantidadTransacciones = grupo.Transacciones.Count;
            asiento.FechaAsiento = UltimoDiaDelMes(anio, mes);

            asiento.Detalles.Clear();
            asiento.Detalles.Add(new AsientoContableDetalle
            {
                Cuenta = cuentas.DebitoId,
                CuentaCodigo = cuentas.DebitoCodigo,
                CuentaNombre = cuentas.DebitoNombre,
                TipoMovimiento = TipoMovimiento.Debito,
                Monto = grupo.Monto
            });
            asiento.Detalles.Add(new AsientoContableDetalle
            {
                Cuenta = cuentas.CreditoId,
                CuentaCodigo = cuentas.CreditoCodigo,
                CuentaNombre = cuentas.CreditoNombre,
                TipoMovimiento = TipoMovimiento.Credito,
                Monto = grupo.Monto
            });

            if (esNuevo)
                await _repo.AddAsync(asiento, ct);

            try
            {
                var resp = await _contabilidad.RegistrarAsientoAsync(
                    cuentas.DebitoId, cuentas.CreditoId, asiento.Descripcion, asiento.Monto, ct);

                asiento.NumeroAsiento = resp.NumeroAsiento;
                asiento.FechaEnvio = resp.Fecha;
                asiento.EstadoEnvio = EstadoEnvioAsiento.Enviado;
                asiento.MensajeError = null;

                // Solo se marcan cuando Contabilidad aceptó. Si falla, quedan
                // pendientes y entran en el siguiente intento.
                foreach (var t in grupo.Transacciones)
                    t.AsientoContable = asiento;
            }
            catch (DomainValidationException ex)
            {
                asiento.EstadoEnvio = EstadoEnvioAsiento.Fallido;
                asiento.MensajeError = ex.Message;
            }

            // Se guarda asiento por asiento: si el tercero falla a mitad del
            // período, lo ya enviado queda registrado y no se reenvía.
            await _repo.SaveChangesAsync(ct);
            procesados.Add(asiento);
        }

        return procesados.Select(Map).ToList();
    }

    public async Task<AsientoContableDto?> ReintentarAsync(int id, CancellationToken ct = default)
    {
        var asiento = await _repo.GetByIdAsync(id, ct);
        if (asiento is null) return null;

        if (asiento.EstadoEnvio == EstadoEnvioAsiento.Enviado)
            throw new DomainValidationException(
                $"El asiento ya fue enviado a Contabilidad con el número {asiento.NumeroAsiento}.");

        // El monto se recalcula: entre el fallo y el reintento pueden haberse
        // registrado más transacciones del mismo concepto.
        var pendientes = await _repo.GetTransaccionesSinContabilizarAsync(asiento.Anio, asiento.Mes, ct);
        var grupo = Agrupar(pendientes)
            .FirstOrDefault(g => g.Tipo == asiento.TipoTransaccion && g.ConceptoId == asiento.ConceptoId);

        if (grupo is null)
            throw new DomainValidationException(
                "El concepto ya no tiene transacciones pendientes de contabilizar.");

        var cuentas = await _contabilidad.ResolverCuentasAsync(asiento.TipoTransaccion, ct);

        asiento.Monto = grupo.Monto;
        asiento.CantidadTransacciones = grupo.Transacciones.Count;
        foreach (var d in asiento.Detalles) d.Monto = grupo.Monto;

        try
        {
            var resp = await _contabilidad.RegistrarAsientoAsync(
                cuentas.DebitoId, cuentas.CreditoId, asiento.Descripcion, asiento.Monto, ct);

            asiento.NumeroAsiento = resp.NumeroAsiento;
            asiento.FechaEnvio = resp.Fecha;
            asiento.EstadoEnvio = EstadoEnvioAsiento.Enviado;
            asiento.MensajeError = null;

            foreach (var t in grupo.Transacciones)
                t.AsientoContable = asiento;
        }
        catch (DomainValidationException ex)
        {
            asiento.EstadoEnvio = EstadoEnvioAsiento.Fallido;
            asiento.MensajeError = ex.Message;
        }

        await _repo.SaveChangesAsync(ct);
        return Map(asiento);
    }

    public async Task<List<AsientoContableDto>> ListAsync(int? anio, int? mes, CancellationToken ct = default)
    {
        if (mes is int m && (m < 1 || m > 12))
            throw new DomainValidationException("El mes debe estar entre 1 y 12.");

        var items = await _repo.ListAsync(anio, mes, ct);
        return items.Select(Map).ToList();
    }

    private async Task<(Dictionary<int, string> Ingresos, Dictionary<int, string> Deducciones)> GetCatalogosAsync(
        CancellationToken ct)
    {
        var ingresos = await _repo.GetTiposIngresoNamesAsync(ct);
        var deducciones = await _repo.GetTiposDeduccionNamesAsync(ct);
        return (ingresos, deducciones);
    }

    private static void ValidarPeriodo(int anio, int mes)
    {
        if (mes < 1 || mes > 12)
            throw new DomainValidationException("El mes debe estar entre 1 y 12.");
        if (anio < 2000 || anio > 2100)
            throw new DomainValidationException("El año indicado no es válido.");
    }

    private static string NombreConcepto(
        TipoTransaccion tipo,
        int conceptoId,
        IReadOnlyDictionary<int, string> ingresos,
        IReadOnlyDictionary<int, string> deducciones)
    {
        var lookup = tipo == TipoTransaccion.Ingreso ? ingresos : deducciones;
        return lookup.TryGetValue(conceptoId, out var n) ? n : $"(id {conceptoId})";
    }

    private static string ConstruirDescripcion(
        int anio, int mes, TipoTransaccion tipo, string concepto, bool complementario)
    {
        var etiqueta = tipo == TipoTransaccion.Ingreso ? "Ingreso" : "Deducción";
        var sufijo = complementario ? " (complementario)" : string.Empty;
        return $"Nómina {anio:D4}-{mes:D2} · {etiqueta}: {concepto}{sufijo}";
    }

    /// <summary>Un concepto del período con sus transacciones aún no contabilizadas.</summary>
    private sealed record GrupoConcepto(
        TipoTransaccion Tipo,
        int ConceptoId,
        decimal Monto,
        List<Transaccion> Transacciones);

    private static List<GrupoConcepto> Agrupar(List<Transaccion> transacciones) =>
        transacciones
            .GroupBy(t => new { t.TipoTransaccion, t.ConceptoId })
            .Select(g => new GrupoConcepto(
                g.Key.TipoTransaccion,
                g.Key.ConceptoId,
                g.Sum(t => t.Monto),
                g.ToList()))
            .ToList();

    private static bool HayEnvioPrevio(
        IEnumerable<AsientoContable> existentes, TipoTransaccion tipo, int conceptoId) =>
        existentes.Any(a =>
            a.TipoTransaccion == tipo &&
            a.ConceptoId == conceptoId &&
            a.Estado == EstadoRegistro.Activo &&
            a.EstadoEnvio == EstadoEnvioAsiento.Enviado);

    private static AsientoContable? BuscarFallido(
        IEnumerable<AsientoContable> existentes, TipoTransaccion tipo, int conceptoId) =>
        existentes.FirstOrDefault(a =>
            a.TipoTransaccion == tipo &&
            a.ConceptoId == conceptoId &&
            a.Estado == EstadoRegistro.Activo &&
            a.EstadoEnvio != EstadoEnvioAsiento.Enviado);

    /// <summary>
    /// El asiento corresponde al período cerrado, no al día del envío. Contabilidad
    /// hoy no acepta la fecha y estampa la del sistema, pero la conservamos de
    /// nuestro lado para que el registro local sea correcto.
    /// </summary>
    private static DateTime UltimoDiaDelMes(int anio, int mes) =>
        new(anio, mes, DateTime.DaysInMonth(anio, mes));

    private static AsientoContableDto Map(AsientoContable a) => new(
        a.Id,
        a.Anio,
        a.Mes,
        a.TipoTransaccion,
        a.ConceptoId,
        a.ConceptoNombre,
        a.Descripcion,
        a.Monto,
        a.FechaAsiento,
        a.CantidadTransacciones,
        a.Estado,
        a.EstadoEnvio,
        a.NumeroAsiento,
        a.FechaEnvio,
        a.MensajeError,
        a.Detalles
            .OrderBy(d => d.TipoMovimiento)
            .Select(d => new AsientoContableDetalleDto(
                d.Cuenta, d.CuentaCodigo, d.CuentaNombre, d.TipoMovimiento, d.Monto))
            .ToList());
}
