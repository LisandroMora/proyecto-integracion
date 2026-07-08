using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nomina.Application.Common;
using Nomina.Application.DTOs;
using Nomina.Application.Interfaces;

namespace Nomina.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/empleados")]
public class EmpleadosController : ControllerBase
{
    private readonly IEmpleadoService _service;
    public EmpleadosController(IEmpleadoService service) => _service = service;

    [HttpGet]
    public Task<List<EmpleadoDto>> List(
        [FromQuery] EstadoFilter estado = EstadoFilter.Activos,
        CancellationToken ct = default) =>
        _service.ListAsync(estado, ct);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmpleadoDto>> GetById(int id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<EmpleadoDto>> Create([FromBody] EmpleadoCreateDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EmpleadoDto>> Update(int id, [FromBody] EmpleadoUpdateDto dto, CancellationToken ct)
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
