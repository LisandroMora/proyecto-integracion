using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nomina.Domain.Entities;

namespace Nomina.Infrastructure.Persistence.Configurations;

public class TransaccionConfiguration : IEntityTypeConfiguration<Transaccion>
{
    public void Configure(EntityTypeBuilder<Transaccion> builder)
    {
        builder.ToTable("Transacciones");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TipoTransaccion).HasConversion<int>();
        builder.Property(x => x.Monto).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Fecha).HasColumnType("datetime2");
        builder.Property(x => x.Estado).HasConversion<int>();

        builder.HasOne(x => x.Empleado)
            .WithMany(e => e.Transacciones)
            .HasForeignKey(x => x.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TipoTransaccion, x.ConceptoId });
    }
}
