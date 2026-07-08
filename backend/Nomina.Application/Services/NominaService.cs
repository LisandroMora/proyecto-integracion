using Nomina.Application.DTOs;
using Nomina.Application.Interfaces;

namespace Nomina.Application.Services;

public class NominaService : INominaService
{
    private readonly INominaRepository _repo;
    public NominaService(INominaRepository repo) => _repo = repo;

    public async Task<List<NominaDto>> ListAsync(CancellationToken ct = default)
    {
        var items = await _repo.ListAsync(ct);
        return items.Select(n => new NominaDto(n.Id, n.Nombre, n.Estado)).ToList();
    }
}
