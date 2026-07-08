using Microsoft.EntityFrameworkCore;
using Nomina.Application.Common;
using Nomina.Application.Interfaces;
using Nomina.Domain.Entities;
using Nomina.Domain.Enums;

namespace Nomina.Infrastructure.Persistence;

public class TipoDeduccionRepository : ITipoDeduccionRepository
{
    private readonly NominaDbContext _db;
    public TipoDeduccionRepository(NominaDbContext db) => _db = db;

    public Task<List<TipoDeduccion>> ListAsync(EstadoFilter filter = EstadoFilter.Activos, CancellationToken ct = default)
    {
        IQueryable<TipoDeduccion> q = _db.TiposDeduccion;
        q = filter switch
        {
            EstadoFilter.Activos => q.Where(x => x.Estado == EstadoRegistro.Activo),
            EstadoFilter.Inactivos => q.Where(x => x.Estado == EstadoRegistro.Inactivo),
            _ => q
        };
        return q.OrderBy(x => x.Nombre).ToListAsync(ct);
    }

    public Task<TipoDeduccion?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.TiposDeduccion.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(TipoDeduccion entity, CancellationToken ct = default)
    {
        await _db.TiposDeduccion.AddAsync(entity, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
