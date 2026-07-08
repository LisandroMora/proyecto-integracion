using Nomina.Application.Common;
using Nomina.Domain.Entities;

namespace Nomina.Application.Interfaces;

public interface ITipoIngresoRepository
{
    Task<List<TipoIngreso>> ListAsync(EstadoFilter filter = EstadoFilter.Activos, CancellationToken ct = default);
    Task<TipoIngreso?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(TipoIngreso entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
