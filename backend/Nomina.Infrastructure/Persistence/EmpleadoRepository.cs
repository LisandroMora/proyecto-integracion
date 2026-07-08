using Microsoft.EntityFrameworkCore;
using Nomina.Application.Common;
using Nomina.Application.Interfaces;
using Nomina.Domain.Entities;
using Nomina.Domain.Enums;

namespace Nomina.Infrastructure.Persistence;

public class EmpleadoRepository : IEmpleadoRepository
{
    private readonly NominaDbContext _db;
    public EmpleadoRepository(NominaDbContext db) => _db = db;

    public Task<List<Empleado>> ListAsync(EstadoFilter filter = EstadoFilter.Activos, CancellationToken ct = default)
    {
        IQueryable<Empleado> q = _db.Empleados.Include(e => e.Nomina);
        q = filter switch
        {
            EstadoFilter.Activos => q.Where(e => e.Estado == EstadoRegistro.Activo),
            EstadoFilter.Inactivos => q.Where(e => e.Estado == EstadoRegistro.Inactivo),
            _ => q
        };
        return q.OrderBy(e => e.Nombre).ToListAsync(ct);
    }

    public Task<Empleado?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Empleados
            .Include(e => e.Nomina)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task AddAsync(Empleado entity, CancellationToken ct = default)
    {
        await _db.Empleados.AddAsync(entity, ct);
    }

    public Task<bool> ExistsCedulaAsync(string cedula, int? excludeId, CancellationToken ct = default) =>
        _db.Empleados.AnyAsync(
            e => e.Cedula == cedula && (excludeId == null || e.Id != excludeId),
            ct);

    public Task<bool> NominaExistsAsync(int nominaId, CancellationToken ct = default) =>
        _db.Nominas.AnyAsync(n => n.Id == nominaId, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
