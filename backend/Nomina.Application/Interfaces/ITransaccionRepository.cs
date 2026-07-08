using Nomina.Application.Common;
using Nomina.Domain.Entities;

namespace Nomina.Application.Interfaces;

public interface ITransaccionRepository
{
    Task<List<Transaccion>> ListAsync(EstadoFilter filter = EstadoFilter.Activos, CancellationToken ct = default);
    Task<Transaccion?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Transaccion entity, CancellationToken ct = default);
    Task<bool> EmpleadoExistsAsync(int empleadoId, CancellationToken ct = default);
    Task<Dictionary<int, string>> GetTiposIngresoNamesAsync(CancellationToken ct = default);
    Task<Dictionary<int, string>> GetTiposDeduccionNamesAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
