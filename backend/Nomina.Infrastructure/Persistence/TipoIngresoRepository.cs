using Microsoft.EntityFrameworkCore;
using Nomina.Application.Common;
using Nomina.Application.Interfaces;
using Nomina.Domain.Entities;
using Nomina.Domain.Enums;

namespace Nomina.Infrastructure.Persistence;

public class TipoIngresoRepository : ITipoIngresoRepository
{
    private readonly NominaDbContext _db;
    public TipoIngresoRepository(NominaDbContext db) => _db = db;

    public Task<List<TipoIngreso>> ListAsync(EstadoFilter filter = EstadoFilter.Activos, CancellationToken ct = default)
    {
        IQueryable<TipoIngreso> q = _db.TiposIngreso;
        q = filter switch
        {
            EstadoFilter.Activos => q.Where(x => x.Estado == EstadoRegistro.Activo),
            EstadoFilter.Inactivos => q.Where(x => x.Estado == EstadoRegistro.Inactivo),
            _ => q
        };
        return q.OrderBy(x => x.Nombre).ToListAsync(ct);
    }

    public Task<TipoIngreso?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.TiposIngreso.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(TipoIngreso entity, CancellationToken ct = default)
    {
        await _db.TiposIngreso.AddAsync(entity, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
