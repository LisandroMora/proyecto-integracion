using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nomina.Application.DTOs;
using Nomina.Application.Interfaces;

namespace Nomina.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/asientos-contables")]
public class AsientosContablesController : ControllerBase
{
    private readonly IAsientoContableService _service;
    public AsientosContablesController(IAsientoContableService service) => _service = service;

    /// <summary>Historial de asientos generados, filtrable por período.</summary>
    [HttpGet]
    public Task<List<AsientoContableDto>> List(
        [FromQuery] int? anio,
        [FromQuery] int? mes,
        CancellationToken ct = default) =>
        _service.ListAsync(anio, mes, ct);

    /// <summary>Qué se enviaría para el período. No persiste ni envía nada.</summary>
    [HttpGet("preview")]
    public Task<List<AsientoPreviewDto>> Preview(
        [FromQuery] int anio,
        [FromQuery] int mes,
        CancellationToken ct = default) =>
        _service.PreviewAsync(anio, mes, ct);

    /// <summary>Genera y envía a Contabilidad un asiento por cada concepto del período.</summary>
    [HttpPost("enviar")]
    public Task<List<AsientoContableDto>> Enviar(
        [FromBody] PeriodoRequest request,
        CancellationToken ct = default) =>
        _service.EnviarPeriodoAsync(request.Anio, request.Mes, ct);

    [HttpPost("{id:int}/reintentar")]
    public async Task<ActionResult<AsientoContableDto>> Reintentar(int id, CancellationToken ct)
    {
        var result = await _service.ReintentarAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
