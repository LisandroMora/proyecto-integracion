using Nomina.Application.Common;
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
            var cuentas = await _contabilidad.ResolverCuentasAsync(ct);

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

        var cuentas = await _contabilidad.ResolverCuentasAsync(ct);

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

    public async Task<VerificacionPeriodoDto> VerificarPeriodoAsync(
        int anio, int mes, CancellationToken ct = default)
    {
        ValidarPeriodo(anio, mes);

        // Si la consulta falla, la excepción sube tal cual y no se marca nada.
        // "No pude preguntar" no es "no está": confundirlos llevaría a reabrir y
        // reenviar asientos que sí existen, y Contabilidad no rechaza duplicados.
        var remotas = await _contabilidad.ConsultarEntradasAsync(ct);

        var locales = (await _repo.ListByPeriodoAsync(anio, mes, ct))
            .Where(a => a.Estado == EstadoRegistro.Activo && a.EstadoEnvio == EstadoEnvioAsiento.Enviado)
            .OrderBy(a => a.Id)
            .ToList();

        // Su API no filtra por período, pero nuestra descripción lo lleva dentro,
        // así que sirve para quedarnos solo con lo de este cierre.
        var prefijo = PrefijoPeriodo(anio, mes);
        var disponibles = remotas
            .Where(e => e.Descripcion.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var ahora = DateTime.UtcNow;
        var resultados = new List<VerificacionAsientoDto>();

        foreach (var asiento in locales)
        {
            // Emparejamiento uno a uno: dos asientos complementarios del mismo
            // concepto comparten descripción, y cada uno tiene que casar con una
            // entrada distinta para que el segundo no confirme con la del primero.
            var homonimas = disponibles
                .Where(e => string.Equals(e.Descripcion, asiento.Descripcion, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var exacta = homonimas.FirstOrDefault(e => e.EstaActiva && e.Monto == asiento.Monto);
            if (exacta is not null)
            {
                disponibles.Remove(exacta);

                // El número se refresca: si su base se reinició, el que guardamos
                // al enviar ya no apunta a este asiento.
                asiento.NumeroAsiento = exacta.NumeroAsiento ?? asiento.NumeroAsiento;
                Marcar(asiento, EstadoVerificacionAsiento.Confirmado, null, ahora);
                resultados.Add(Resultado(asiento, exacta.Monto));
                continue;
            }

            var otroMonto = homonimas.FirstOrDefault(e => e.EstaActiva);
            if (otroMonto is not null)
            {
                disponibles.Remove(otroMonto);
                Marcar(asiento, EstadoVerificacionAsiento.Divergente,
                    $"Contabilidad lo tiene por {otroMonto.Monto:N2} y nosotros por {asiento.Monto:N2}.",
                    ahora);
                resultados.Add(Resultado(asiento, otroMonto.Monto));
                continue;
            }

            var anulada = homonimas.FirstOrDefault();
            if (anulada is not null)
            {
                disponibles.Remove(anulada);
                Marcar(asiento, EstadoVerificacionAsiento.NoEncontrado,
                    $"Contabilidad lo tiene en estado {anulada.Estado}.", ahora);
                resultados.Add(Resultado(asiento, anulada.Monto));
                continue;
            }

            Marcar(asiento, EstadoVerificacionAsiento.NoEncontrado,
                "Contabilidad no tiene ningún asiento con esta descripción.", ahora);
            resultados.Add(Resultado(asiento, null));
        }

        await _repo.SaveChangesAsync(ct);

        // Lo que sobra del período está en Contabilidad sin respaldo nuestro:
        // típicamente un envío que se duplicó. Se informa, no se toca.
        var huerfanas = disponibles
            .Select(e => new EntradaHuerfanaDto(e.NumeroAsiento, e.Descripcion, e.Monto, e.Estado))
            .ToList();

        return new VerificacionPeriodoDto(
            anio,
            mes,
            resultados.Count(r => r.EstadoVerificacion == EstadoVerificacionAsiento.Confirmado),
            resultados.Count(r => r.EstadoVerificacion == EstadoVerificacionAsiento.NoEncontrado),
            resultados.Count(r => r.EstadoVerificacion == EstadoVerificacionAsiento.Divergente),
            resultados,
            huerfanas);
    }

    public async Task<ReaperturaDto?> ReabrirAsync(int id, CancellationToken ct = default)
    {
        var asiento = await _repo.GetByIdAsync(id, ct);
        if (asiento is null) return null;

        if (asiento.Estado != EstadoRegistro.Activo)
            throw new DomainValidationException("El asiento ya fue reabierto.");

        // Se exige la verificación previa a propósito: reabrir sin evidencia de que
        // Contabilidad no lo tiene es la vía directa a contabilizar dos veces lo mismo.
        if (asiento.EstadoVerificacion != EstadoVerificacionAsiento.NoEncontrado)
            throw new DomainValidationException(
                "Solo se puede reabrir un asiento que la verificación marcó como no encontrado " +
                "en Contabilidad. Verifique el período primero.");

        var transacciones = await _repo.GetTransaccionesByAsientoAsync(asiento.Id, ct);
        foreach (var t in transacciones)
            t.AsientoContableId = null;

        // Baja lógica: queda como evidencia de que se envió y se perdió, pero deja
        // de contar como envío previo, así que el próximo cierre vuelve a tomar
        // estas transacciones y genera un asiento nuevo.
        asiento.Estado = EstadoRegistro.Inactivo;

        await _repo.SaveChangesAsync(ct);
        return new ReaperturaDto(asiento.Id, transacciones.Count);
    }

    public async Task<List<AsientoContableDto>> ListAsync(
        int? anio, int? mes, EstadoFilter filter = EstadoFilter.Activos, CancellationToken ct = default)
    {
        if (mes is int m && (m < 1 || m > 12))
            throw new DomainValidationException("El mes debe estar entre 1 y 12.");

        var items = await _repo.ListAsync(anio, mes, filter, ct);
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
        return $"{PrefijoPeriodo(anio, mes)} {etiqueta}: {concepto}{sufijo}";
    }

    /// <summary>
    /// La descripción es lo único que nos permite reconocer un asiento nuestro del
    /// lado de Contabilidad: su API no filtra por período y el número que devuelve
    /// se recicla cuando su base se reinicia. Si cambia este formato, la
    /// verificación deja de encontrar los asientos enviados con el formato viejo.
    /// </summary>
    private static string PrefijoPeriodo(int anio, int mes) => $"Nómina {anio:D4}-{mes:D2} ·";

    private static void Marcar(
        AsientoContable asiento, EstadoVerificacionAsiento estado, string? mensaje, DateTime cuando)
    {
        asiento.EstadoVerificacion = estado;
        asiento.MensajeVerificacion = mensaje;
        asiento.FechaVerificacion = cuando;
    }

    private static VerificacionAsientoDto Resultado(AsientoContable a, decimal? montoContabilidad) => new(
        a.Id,
        a.ConceptoNombre,
        a.Descripcion,
        a.Monto,
        montoContabilidad,
        a.NumeroAsiento,
        a.EstadoVerificacion,
        a.MensajeVerificacion);

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
        a.EstadoVerificacion,
        a.FechaVerificacion,
        a.MensajeVerificacion,
        a.Detalles
            .OrderBy(d => d.TipoMovimiento)
            .Select(d => new AsientoContableDetalleDto(
                d.Cuenta, d.CuentaCodigo, d.CuentaNombre, d.TipoMovimiento, d.Monto))
            .ToList());
}
