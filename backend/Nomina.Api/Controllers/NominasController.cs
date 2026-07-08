using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nomina.Application.DTOs;
using Nomina.Application.Interfaces;

namespace Nomina.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/nominas")]
public class NominasController : ControllerBase
{
    private readonly INominaService _service;
    public NominasController(INominaService service) => _service = service;

    [HttpGet]
    public Task<List<NominaDto>> List(CancellationToken ct) => _service.ListAsync(ct);
}
