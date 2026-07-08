using Microsoft.EntityFrameworkCore;
using Nomina.Application.Interfaces;
using Nomina.Domain.Entities;

namespace Nomina.Infrastructure.Persistence;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly NominaDbContext _db;
    public UsuarioRepository(NominaDbContext db) => _db = db;

    public Task<Usuario?> FindActiveByEmailWithRolAsync(string email, CancellationToken ct = default) =>
        _db.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email == email, ct);
}
