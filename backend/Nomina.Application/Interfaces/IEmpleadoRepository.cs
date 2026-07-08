using Nomina.Application.Common;
using Nomina.Domain.Entities;

namespace Nomina.Application.Interfaces;

public interface IEmpleadoRepository
{
    Task<List<Empleado>> ListAsync(EstadoFilter filter = EstadoFilter.Activos, CancellationToken ct = default);
    Task<Empleado?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Empleado entity, CancellationToken ct = default);
    Task<bool> ExistsCedulaAsync(string cedula, int? excludeId, CancellationToken ct = default);
    Task<bool> NominaExistsAsync(int nominaId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
