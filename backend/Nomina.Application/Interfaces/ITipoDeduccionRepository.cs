using Nomina.Application.Common;
using Nomina.Domain.Entities;

namespace Nomina.Application.Interfaces;

public interface ITipoDeduccionRepository
{
    Task<List<TipoDeduccion>> ListAsync(EstadoFilter filter = EstadoFilter.Activos, CancellationToken ct = default);
    Task<TipoDeduccion?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(TipoDeduccion entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
