using Microsoft.EntityFrameworkCore;
using Nomina.Application.Common;
using Nomina.Application.Interfaces;
using Nomina.Domain.Entities;
using Nomina.Domain.Enums;

namespace Nomina.Infrastructure.Persistence;

public class TransaccionRepository : ITransaccionRepository
{
    private readonly NominaDbContext _db;
    public TransaccionRepository(NominaDbContext db) => _db = db;

    public Task<List<Transaccion>> ListAsync(EstadoFilter filter = EstadoFilter.Activos, CancellationToken ct = default)
    {
        IQueryable<Transaccion> q = _db.Transacciones.Include(t => t.Empleado);
        q = filter switch
        {
            EstadoFilter.Activos => q.Where(t => t.Estado == EstadoRegistro.Activo),
            EstadoFilter.Inactivos => q.Where(t => t.Estado == EstadoRegistro.Inactivo),
            _ => q
        };
        return q.OrderByDescending(t => t.Fecha).ThenByDescending(t => t.Id).ToListAsync(ct);
    }

    public Task<Transaccion?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Transacciones
            .Include(t => t.Empleado)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddAsync(Transaccion entity, CancellationToken ct = default)
    {
        await _db.Transacciones.AddAsync(entity, ct);
    }

    public Task<bool> EmpleadoExistsAsync(int empleadoId, CancellationToken ct = default) =>
        _db.Empleados.AnyAsync(e => e.Id == empleadoId, ct);

    public async Task<Dictionary<int, string>> GetTiposIngresoNamesAsync(CancellationToken ct = default)
    {
        var items = await _db.TiposIngreso
            .Select(x => new { x.Id, x.Nombre })
            .ToListAsync(ct);
        return items.ToDictionary(x => x.Id, x => x.Nombre);
    }

    public async Task<Dictionary<int, string>> GetTiposDeduccionNamesAsync(CancellationToken ct = default)
    {
        var items = await _db.TiposDeduccion
            .Select(x => new { x.Id, x.Nombre })
            .ToListAsync(ct);
        return items.ToDictionary(x => x.Id, x => x.Nombre);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
