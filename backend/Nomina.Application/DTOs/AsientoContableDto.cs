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
