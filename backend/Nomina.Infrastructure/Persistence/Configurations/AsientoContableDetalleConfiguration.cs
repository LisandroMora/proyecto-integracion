using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nomina.Domain.Entities;

namespace Nomina.Infrastructure.Persistence.Configurations;

public class AsientoContableDetalleConfiguration : IEntityTypeConfiguration<AsientoContableDetalle>
{
    public void Configure(EntityTypeBuilder<AsientoContableDetalle> builder)
    {
        builder.ToTable("AsientosContablesDetalle");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CuentaCodigo).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CuentaNombre).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Monto).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TipoMovimiento).HasConversion<int>();

        builder.HasIndex(x => x.AsientoContableId);
    }
}
