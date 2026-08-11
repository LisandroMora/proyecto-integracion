using Nomina.Application.Common;
using Nomina.Application.DTOs;

namespace Nomina.Application.Interfaces;

public interface IAsientoContableService
{
    /// <summary>Qué se enviaría para el período, sin persistir ni enviar nada.</summary>
    Task<List<AsientoPreviewDto>> PreviewAsync(int anio, int mes, CancellationToken ct = default);

    /// <summary>
    /// Genera y envía a Contabilidad un asiento por cada concepto pendiente del período.
    /// Los conceptos ya enviados se omiten.
    /// </summary>
    Task<List<AsientoContableDto>> EnviarPeriodoAsync(int anio, int mes, CancellationToken ct = default);

    /// <summary>Reintenta el envío de un asiento que quedó fallido.</summary>
    Task<AsientoContableDto?> ReintentarAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Cruza los asientos ya enviados del período contra lo que Contabilidad tiene
    /// hoy y guarda el resultado en cada uno. No modifica transacciones.
    /// </summary>
    Task<VerificacionPeriodoDto> VerificarPeriodoAsync(int anio, int mes, CancellationToken ct = default);

    /// <summary>
    /// Da de baja un asiento que la verificación no encontró en Contabilidad y
    /// devuelve sus transacciones a pendientes para que entren en el próximo cierre.
    /// </summary>
    Task<ReaperturaDto?> ReabrirAsync(int id, CancellationToken ct = default);

    Task<List<AsientoContableDto>> ListAsync(
        int? anio, int? mes, EstadoFilter filter = EstadoFilter.Activos, CancellationToken ct = default);
}
