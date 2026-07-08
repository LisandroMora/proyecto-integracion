using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NominaEntity = Nomina.Domain.Entities.Nomina;

namespace Nomina.Infrastructure.Persistence.Configurations;

public class NominaConfiguration : IEntityTypeConfiguration<NominaEntity>
{
    public void Configure(EntityTypeBuilder<NominaEntity> builder)
    {
        builder.ToTable("Nominas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nombre).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Estado).HasConversion<int>();
    }
}
