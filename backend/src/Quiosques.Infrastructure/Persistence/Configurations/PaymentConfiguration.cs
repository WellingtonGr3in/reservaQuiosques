using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quiosques.Domain.Entities;

namespace Quiosques.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("pagamento", table =>
        {
            table.HasCheckConstraint(
                "ck_pagamento_status",
                $"status IN ('{StatusMapping.PaymentPending}', '{StatusMapping.PaymentApproved}', '{StatusMapping.PaymentRefunded}')");
            table.HasCheckConstraint(
                "ck_pagamento_metodo",
                $"metodo IN ('{StatusMapping.MethodPix}', '{StatusMapping.MethodAdministration}')");
        });
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.ReservationId).HasColumnName("reserva_id");
        builder.Property(p => p.GatewayTransactionId).HasColumnName("transacao_gateway_id").HasMaxLength(100).IsRequired();
        builder.Property(p => p.Amount).HasColumnName("valor").HasPrecision(10, 2);
        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(StatusMapping.PaymentStatusConverter);
        builder.Property(p => p.PaidAt).HasColumnName("pago_em");
        // Pagamentos anteriores a esta coluna eram todos via Pix.
        builder.Property(p => p.Method)
            .HasColumnName("metodo")
            .HasMaxLength(20)
            .HasConversion(StatusMapping.PaymentMethodConverter)
            .HasDefaultValue(PaymentMethod.Pix)
            .HasSentinel((PaymentMethod)(-1));

        builder.HasOne<Reservation>()
            .WithMany(r => r.Payments)
            .HasForeignKey(p => p.ReservationId)
            .OnDelete(DeleteBehavior.Restrict);

        // RNF03: a mesma confirmação do gateway não é processada duas vezes.
        builder.HasIndex(p => p.GatewayTransactionId).IsUnique().HasDatabaseName("ux_pagamento_transacao_gateway_id");
    }
}
