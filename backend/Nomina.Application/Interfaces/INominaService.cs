using Nomina.Application.DTOs;

namespace Nomina.Application.Interfaces;

public interface INominaService
{
    Task<List<NominaDto>> ListAsync(CancellationToken ct = default);
}
