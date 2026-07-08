using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nomina.Application.Common;
using Nomina.Application.DTOs;
using Nomina.Application.Interfaces;

namespace Nomina.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tipos-ingreso")]
public class TiposIngresoController : ControllerBase
{
    private readonly ITipoIngresoService _service;
    public TiposIngresoController(ITipoIngresoService service) => _service = service;

    [HttpGet]
    public Task<List<TipoIngresoDto>> List(
        [FromQuery] EstadoFilter estado = EstadoFilter.Activos,
        CancellationToken ct = default) =>
        _service.ListAsync(estado, ct);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TipoIngresoDto>> GetById(int id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<TipoIngresoDto>> Create([FromBody] TipoIngresoCreateDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TipoIngresoDto>> Update(int id, [FromBody] TipoIngresoUpdateDto dto, CancellationToken ct)
    {
        var updated = await _service.UpdateAsync(id, dto, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> SoftDelete(int id, CancellationToken ct)
    {
        var ok = await _service.SoftDeleteAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }
}
