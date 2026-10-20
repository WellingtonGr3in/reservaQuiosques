using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quiosques.Domain.Entities;

namespace Quiosques.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("cliente");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.Cpf).HasColumnName("cpf").HasColumnType("char(11)").IsRequired();
        builder.Property(c => c.Name).HasColumnName("nome").HasMaxLength(150).IsRequired();
        builder.Property(c => c.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
        builder.Property(c => c.Phone).HasColumnName("telefone").HasMaxLength(20).IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("criado_em");

        builder.HasIndex(c => c.Cpf).IsUnique().HasDatabaseName("ux_cliente_cpf");
    }
}
