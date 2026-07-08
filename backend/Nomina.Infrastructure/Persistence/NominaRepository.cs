using Microsoft.EntityFrameworkCore;
using Nomina.Application.Interfaces;
using NominaEntity = Nomina.Domain.Entities.Nomina;

namespace Nomina.Infrastructure.Persistence;

public class NominaRepository : INominaRepository
{
    private readonly NominaDbContext _db;
    public NominaRepository(NominaDbContext db) => _db = db;

    public Task<List<NominaEntity>> ListAsync(CancellationToken ct = default) =>
        _db.Nominas.OrderBy(n => n.Nombre).ToListAsync(ct);
}
