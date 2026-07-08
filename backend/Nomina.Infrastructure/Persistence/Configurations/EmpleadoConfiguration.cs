using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nomina.Domain.Entities;

namespace Nomina.Infrastructure.Persistence.Configurations;

public class EmpleadoConfiguration : IEntityTypeConfiguration<Empleado>
{
    public void Configure(EntityTypeBuilder<Empleado> builder)
    {
        builder.ToTable("Empleados");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Cedula).IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.Cedula).IsUnique();
        builder.Property(x => x.Nombre).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Departamento).HasMaxLength(100);
        builder.Property(x => x.Puesto).HasMaxLength(100);
        builder.Property(x => x.SalarioMensual).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Estado).HasConversion<int>();

        builder.HasOne(x => x.Nomina)
            .WithMany(n => n.Empleados)
            .HasForeignKey(x => x.NominaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
