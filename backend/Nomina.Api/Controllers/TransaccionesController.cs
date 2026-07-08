using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nomina.Application.Common;
using Nomina.Application.DTOs;
using Nomina.Application.Interfaces;

namespace Nomina.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/transacciones")]
public class TransaccionesController : ControllerBase
{
    private readonly ITransaccionService _service;
    public TransaccionesController(ITransaccionService service) => _service = service;

    [HttpGet]
    public Task<List<TransaccionDto>> List(
        [FromQuery] EstadoFilter estado = EstadoFilter.Activos,
        CancellationToken ct = default) =>
        _service.ListAsync(estado, ct);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TransaccionDto>> GetById(int id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<TransaccionDto>> Create([FromBody] TransaccionCreateDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TransaccionDto>> Update(int id, [FromBody] TransaccionUpdateDto dto, CancellationToken ct)
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
