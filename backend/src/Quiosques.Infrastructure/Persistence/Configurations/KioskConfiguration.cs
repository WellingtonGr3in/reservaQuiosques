using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quiosques.Domain.Entities;

namespace Quiosques.Infrastructure.Persistence.Configurations;

public class KioskConfiguration : IEntityTypeConfiguration<Kiosk>
{
    public void Configure(EntityTypeBuilder<Kiosk> builder)
    {
        builder.ToTable("quiosque");
        builder.HasKey(k => k.Id);

        builder.Property(k => k.Id).HasColumnName("id");
        builder.Property(k => k.Name).HasColumnName("nome").HasMaxLength(100).IsRequired();
        builder.Property(k => k.Description).HasColumnName("descricao").HasColumnType("text").IsRequired();
        builder.Property(k => k.Capacity).HasColumnName("capacidade");
        builder.Property(k => k.DailyRate).HasColumnName("valor_diaria").HasPrecision(10, 2);
        builder.Property(k => k.PhotoUrl).HasColumnName("foto_url").HasMaxLength(500).IsRequired();
        builder.Property(k => k.UsageRules).HasColumnName("regras_uso").HasColumnType("text").IsRequired();
        builder.Property(k => k.IsActive).HasColumnName("ativo");

        builder.HasMany(k => k.Photos)
            .WithOne()
            .HasForeignKey(p => p.KioskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(k => k.Items)
            .WithOne()
            .HasForeignKey(i => i.KioskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
