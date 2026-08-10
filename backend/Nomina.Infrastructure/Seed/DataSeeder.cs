using Microsoft.EntityFrameworkCore;
using Nomina.Domain.Entities;
using Nomina.Infrastructure.Persistence;

namespace Nomina.Infrastructure.Seed;

public static class DataSeeder
{
    public const string AdminEmail = "admin@nomina.local";

    /// <summary>
    /// Contraseña de respaldo para desarrollo local. El servidor de prueba está
    /// expuesto a internet y su contraseña se pasa por configuración
    /// (<c>Seed:AdminPassword</c>), no desde el repositorio.
    /// </summary>
    public const string AdminInitialPassword = "Admin123$";

    public static async Task SeedAsync(
        NominaDbContext db,
        string? adminPassword = null,
        CancellationToken ct = default)
    {
        var adminRol = await db.Roles.FirstOrDefaultAsync(r => r.Nombre == "Admin", ct)
            ?? throw new InvalidOperationException("Rol 'Admin' no existe. Ejecute las migraciones primero.");

        var exists = await db.Usuarios.AnyAsync(u => u.Email == AdminEmail, ct);
        if (exists) return;

        var password = string.IsNullOrWhiteSpace(adminPassword)
            ? AdminInitialPassword
            : adminPassword;

        var usuario = new Usuario
        {
            Email = AdminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            RolId = adminRol.Id
        };

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);
    }
}
