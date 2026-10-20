using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quiosques.Domain.Entities;

namespace Quiosques.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public const string CustomerIndex = "ux_conta_cliente_id";
    public const string LoginEmailIndex = "ux_conta_email_login";

    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("conta");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.CustomerId).HasColumnName("cliente_id");
        builder.Property(a => a.LoginEmail).HasColumnName("email_login").HasMaxLength(254).IsRequired();
        builder.Property(a => a.PasswordHash).HasColumnName("senha_hash").HasMaxLength(100).IsRequired();
        builder.Property(a => a.CreatedAt).HasColumnName("criado_em");

        builder.HasOne(a => a.Customer)
            .WithOne(c => c.Account)
            .HasForeignKey<Account>(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // RN09: uma conta por CPF.
        builder.HasIndex(a => a.CustomerId).IsUnique().HasDatabaseName(CustomerIndex);
        builder.HasIndex(a => a.LoginEmail).IsUnique().HasDatabaseName(LoginEmailIndex);
    }
}
