using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quiosques.Domain.Entities;

namespace Quiosques.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    // RN01 / ADR-001: nome usado para identificar a violação (SQLSTATE 23505) e devolver "Data indisponível".
    public const string ActiveReservationIndex = "ux_reserva_quiosque_data_ativa";
    public const string CodeIndex = "ux_reserva_codigo";

    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reserva", table => table.HasCheckConstraint(
            "ck_reserva_status",
            $"status IN ('{StatusMapping.PendingPayment}', '{StatusMapping.Confirmed}', '{StatusMapping.Expired}', '{StatusMapping.Cancelled}')"));
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.Code).HasColumnName("codigo").HasMaxLength(10).IsRequired();
        builder.Property(r => r.CustomerId).HasColumnName("cliente_id");
        builder.Property(r => r.KioskId).HasColumnName("quiosque_id");
        builder.Property(r => r.ReservationDate).HasColumnName("data_reserva");
        builder.Property(r => r.Amount).HasColumnName("valor").HasPrecision(10, 2);
        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(StatusMapping.ReservationStatusConverter);
        builder.Property(r => r.ExpiresAt).HasColumnName("expira_em");
        builder.Property(r => r.CreatedAt).HasColumnName("criado_em");

        builder.HasOne(r => r.Customer)
            .WithMany(c => c.Reservations)
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Kiosk)
            .WithMany()
            .HasForeignKey(r => r.KioskId)
            .OnDelete(DeleteBehavior.Restrict);

        // RN10: consulta por código + CPF.
        builder.HasIndex(r => r.Code).IsUnique().HasDatabaseName(CodeIndex);

        // RN01: índice único parcial, só para reservas ativas (ADR-001).
        builder.HasIndex(r => new { r.KioskId, r.ReservationDate })
            .IsUnique()
            .HasFilter($"status IN ('{StatusMapping.PendingPayment}', '{StatusMapping.Confirmed}')")
            .HasDatabaseName(ActiveReservationIndex);
    }
}
