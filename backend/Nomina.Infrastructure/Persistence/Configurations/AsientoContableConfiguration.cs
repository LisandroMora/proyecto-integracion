using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nomina.Domain.Entities;

namespace Nomina.Infrastructure.Persistence.Configurations;

public class AsientoContableConfiguration : IEntityTypeConfiguration<AsientoContable>
{
    public void Configure(EntityTypeBuilder<AsientoContable> builder)
    {
        builder.ToTable("AsientosContables");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ConceptoNombre).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Descripcion).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Monto).HasColumnType("decimal(18,2)");
        builder.Property(x => x.FechaAsiento).HasColumnType("datetime2");
        builder.Property(x => x.FechaEnvio).HasColumnType("datetime2");
        builder.Property(x => x.MensajeError).HasMaxLength(1000);
        builder.Property(x => x.TipoTransaccion).HasConversion<int>();
        builder.Property(x => x.Estado).HasConversion<int>();
        builder.Property(x => x.EstadoEnvio).HasConversion<int>();

        builder.HasMany(x => x.Detalles)
            .WithOne(d => d.AsientoContable)
            .HasForeignKey(d => d.AsientoContableId)
            .OnDelete(DeleteBehavior.Cascade);

        // Sin índice único sobre (período, tipo, concepto): un concepto puede tener
        // más de un asiento cuando se registran transacciones después de un cierre.
        // El control de duplicados no vive aquí sino en Transaccion.AsientoContableId:
        // una transacción ya contabilizada nunca vuelve a entrar en un asiento.
        builder.HasIndex(x => new { x.Anio, x.Mes, x.TipoTransaccion, x.ConceptoId });
    }
}
