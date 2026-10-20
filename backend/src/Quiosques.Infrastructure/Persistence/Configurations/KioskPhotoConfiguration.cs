using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quiosques.Domain.Entities;

namespace Quiosques.Infrastructure.Persistence.Configurations;

public class KioskPhotoConfiguration : IEntityTypeConfiguration<KioskPhoto>
{
    public void Configure(EntityTypeBuilder<KioskPhoto> builder)
    {
        builder.ToTable("quiosque_foto");
        builder.HasKey(p => p.Id);

        // O id é gerado no código; sem isto, uma foto nova adicionada à lista de um quiosque já existente
        // seria tratada como existente (UPDATE em vez de INSERT).
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(p => p.KioskId).HasColumnName("quiosque_id");
        builder.Property(p => p.Url).HasColumnName("url").HasMaxLength(500).IsRequired();
        builder.Property(p => p.Order).HasColumnName("ordem");
    }
}
