using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nomina.Domain.Entities;

namespace Nomina.Infrastructure.Persistence.Configurations;

public class TipoDeduccionConfiguration : IEntityTypeConfiguration<TipoDeduccion>
{
    public void Configure(EntityTypeBuilder<TipoDeduccion> builder)
    {
        builder.ToTable("TiposDeduccion");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nombre).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Porcentaje).HasColumnType("decimal(5,2)");
        builder.Property(x => x.Estado).HasConversion<int>();
    }
}
