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

    Task<List<AsientoContableDto>> ListAsync(int? anio, int? mes, CancellationToken ct = default);
}
