using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quiosques.Domain.Entities;

namespace Quiosques.Infrastructure.Persistence.Configurations;

public class KioskItemConfiguration : IEntityTypeConfiguration<KioskItem>
{
    public void Configure(EntityTypeBuilder<KioskItem> builder)
    {
        builder.ToTable("quiosque_item");
        builder.HasKey(i => i.Id);

        // O id é gerado no código; sem isto, um item novo adicionado à lista de um quiosque já existente
        // seria tratado como existente (UPDATE em vez de INSERT).
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(i => i.KioskId).HasColumnName("quiosque_id");
        builder.Property(i => i.Description).HasColumnName("descricao").HasMaxLength(150).IsRequired();
    }
}
