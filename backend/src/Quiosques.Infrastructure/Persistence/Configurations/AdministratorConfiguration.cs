using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quiosques.Domain.Entities;

namespace Quiosques.Infrastructure.Persistence.Configurations;

public class AdministratorConfiguration : IEntityTypeConfiguration<Administrator>
{
    public void Configure(EntityTypeBuilder<Administrator> builder)
    {
        builder.ToTable("administrador");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.Name).HasColumnName("nome").HasMaxLength(150).IsRequired();
        builder.Property(a => a.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
        builder.Property(a => a.PasswordHash).HasColumnName("senha_hash").HasMaxLength(100).IsRequired();
        builder.Property(a => a.CreatedAt).HasColumnName("criado_em");

        // RF11: login do administrador por e-mail.
        builder.HasIndex(a => a.Email).IsUnique().HasDatabaseName("ux_administrador_email");
    }
}
