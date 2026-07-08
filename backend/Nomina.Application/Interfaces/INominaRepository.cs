using NominaEntity = Nomina.Domain.Entities.Nomina;

namespace Nomina.Application.Interfaces;

public interface INominaRepository
{
    Task<List<NominaEntity>> ListAsync(CancellationToken ct = default);
}
