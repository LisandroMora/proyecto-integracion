using Microsoft.EntityFrameworkCore;
using Nomina.Domain.Entities;
using Nomina.Domain.Enums;
using NominaEntity = Nomina.Domain.Entities.Nomina;

namespace Nomina.Infrastructure.Persistence;

public class NominaDbContext : DbContext
{
    public NominaDbContext(DbContextOptions<NominaDbContext> options) : base(options) { }

    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<TipoIngreso> TiposIngreso => Set<TipoIngreso>();
    public DbSet<TipoDeduccion> TiposDeduccion => Set<TipoDeduccion>();
    public DbSet<Transaccion> Transacciones => Set<Transaccion>();
    public DbSet<NominaEntity> Nominas => Set<NominaEntity>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<AsientoContable> AsientosContables => Set<AsientoContable>();
    public DbSet<AsientoContableDetalle> AsientosContablesDetalle => Set<AsientoContableDetalle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NominaDbContext).Assembly);

        modelBuilder.Entity<Rol>().HasData(
            new Rol { Id = 1, Nombre = "Admin" }
        );

        modelBuilder.Entity<NominaEntity>().HasData(
            new NominaEntity { Id = 1, Nombre = "Nómina General", Estado = EstadoRegistro.Activo }
        );

        base.OnModelCreating(modelBuilder);
    }
}
