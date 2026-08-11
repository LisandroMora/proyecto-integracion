using Nomina.Domain.Enums;

namespace Nomina.Application.DTOs;

public record AsientoContableDetalleDto(
    int Cuenta,
    string CuentaCodigo,
    string CuentaNombre,
    TipoMovimiento TipoMovimiento,
    decimal Monto);

public record AsientoContableDto(
    int Id,
    int Anio,
    int Mes,
    TipoTransaccion TipoTransaccion,
    int ConceptoId,
    string ConceptoNombre,
    string Descripcion,
    decimal Monto,
    DateTime FechaAsiento,
    int CantidadTransacciones,
    EstadoRegistro Estado,
    EstadoEnvioAsiento EstadoEnvio,
    int? NumeroAsiento,
    DateTime? FechaEnvio,
    string? MensajeError,
    EstadoVerificacionAsiento EstadoVerificacion,
    DateTime? FechaVerificacion,
    string? MensajeVerificacion,
    List<AsientoContableDetalleDto> Detalles);

/// <summary>
/// Lo que se enviaría del período: un renglón por concepto con transacciones aún
/// no contabilizadas. Lo ya enviado no aparece aquí, sino en el historial.
/// </summary>
public record AsientoPreviewDto(
    TipoTransaccion TipoTransaccion,
    int ConceptoId,
    string ConceptoNombre,
    decimal Monto,
    int CantidadTransacciones,
    string Descripcion,
    /// <summary>El concepto ya tuvo un cierre previo: este sería un asiento complementario.</summary>
    bool EsComplementario,
    /// <summary>Mensaje del intento anterior si quedó fallido.</summary>
    string? MensajeError);

public record PeriodoRequest(int Anio, int Mes);

/// <summary>Cómo quedó un asiento nuestro al cruzarlo contra Contabilidad.</summary>
public record VerificacionAsientoDto(
    int AsientoId,
    string ConceptoNombre,
    string Descripcion,
    decimal MontoLocal,
    /// <summary>Monto que tiene Contabilidad; null si no lo encontró.</summary>
    decimal? MontoContabilidad,
    int? NumeroAsiento,
    EstadoVerificacionAsiento EstadoVerificacion,
    string? Mensaje);

/// <summary>
/// Asiento del período que Contabilidad tiene bajo nuestro auxiliar y que no
/// corresponde a ninguno de los nuestros. Casi siempre es un envío duplicado.
/// </summary>
public record EntradaHuerfanaDto(
    int? NumeroAsiento,
    string Descripcion,
    decimal Monto,
    string Estado);

public record VerificacionPeriodoDto(
    int Anio,
    int Mes,
    int Confirmados,
    int NoEncontrados,
    int Divergentes,
    List<VerificacionAsientoDto> Asientos,
    List<EntradaHuerfanaDto> Huerfanas);

/// <summary>Resultado de devolver a pendientes las transacciones de un asiento perdido.</summary>
public record ReaperturaDto(int AsientoId, int TransaccionesReabiertas);
