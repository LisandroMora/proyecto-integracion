using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nomina.Domain.Entities;

namespace Nomina.Infrastructure.Persistence.Configurations;

public class TipoIngresoConfiguration : IEntityTypeConfiguration<TipoIngreso>
{
    public void Configure(EntityTypeBuilder<TipoIngreso> builder)
    {
        builder.ToTable("TiposIngreso");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nombre).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Porcentaje).HasColumnType("decimal(5,2)");
        builder.Property(x => x.Estado).HasConversion<int>();
    }
}
