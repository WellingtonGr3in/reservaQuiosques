using Quiosques.Application.Common;
using Quiosques.Application.Kiosks;
using Quiosques.Application.Reservations;
using Quiosques.Domain.Entities;
using Quiosques.Domain.ValueObjects;

namespace Quiosques.Application.Admin;

// RF12: filtros da lista de reservas do administrador. Campos nulos não filtram.
public record ReservationSearch(DateOnly? From, DateOnly? To, Guid? KioskId, ReservationStatus? Status, string? Cpf);

// Reserva vista pelo administrador: os dados da consulta (CPF continua mascarado, RNF07) mais o contato do cliente
// e como foi paga. "IsActive" indica se ainda dá para alterar (RF14) ou cancelar (RF15).
public record AdminReservation(
    ReservationDetails Details,
    Guid KioskId,
    string CustomerEmail,
    string CustomerPhone,
    PaymentMethod? PaidWith,
    bool IsActive,
    bool Completed)
{
    // Exige a reserva carregada com quiosque, cliente e pagamentos.
    public static AdminReservation From(Reservation reservation, DateTimeOffset now, DateOnly today)
    {
        var active = reservation.Status == ReservationStatus.Confirmed
            || (reservation.Status == ReservationStatus.PendingPayment && reservation.ExpiresAt > now);
        var paidWith = reservation.Payments
            .Where(p => p.Status is PaymentStatus.Approved or PaymentStatus.Refunded)
            .Select(p => (PaymentMethod?)p.Method)
            .FirstOrDefault();

        return new AdminReservation(
            ReservationDetails.From(reservation, now) with { CanCancel = active },
            reservation.KioskId,
            reservation.Customer.Email,
            reservation.Customer.Phone,
            paidWith,
            active,
            reservation.Status == ReservationStatus.Confirmed && reservation.ReservationDate < today);
    }
}

// RF12: lista todas as reservas, com filtros.
public class ListReservationsForAdministrator(IReservationRepository reservations, IClock clock)
{
    public async Task<IReadOnlyList<AdminReservation>> ExecuteAsync(ReservationSearch search, CancellationToken cancellationToken)
    {
        string? cpf = null;
        if (!string.IsNullOrWhiteSpace(search.Cpf))
        {
            cpf = Cpf.TryParse(search.Cpf, out var parsed) ? parsed.Value : throw new ValidationException(Cpf.InvalidMessage);
        }

        var now = clock.UtcNow;
        var today = clock.TodayInSaoPaulo();
        var found = await reservations.SearchAsync(search with { Cpf = cpf }, cancellationToken);
        return found.Select(r => AdminReservation.From(r, now, today)).ToList();
    }
}

// RF12: detalhe de uma reserva.
public class GetReservationForAdministrator(IReservationRepository reservations, IClock clock)
{
    public async Task<AdminReservation> ExecuteAsync(string code, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByCodeAsync(code.Trim().ToUpperInvariant(), cancellationToken)
            ?? throw new NotFoundException(ReservationLookup.NotFoundMessage);
        return AdminReservation.From(reservation, clock.UtcNow, clock.TodayInSaoPaulo());
    }
}

// RF14 / RN13: o administrador muda o quiosque e/ou a data de uma reserva ativa, para qualquer data.
public class RescheduleReservation(
    IReservationRepository reservations,
    IKioskRepository kiosks,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task ExecuteAsync(string code, Guid kioskId, DateOnly date, CancellationToken cancellationToken)
    {
        var kiosk = await kiosks.GetActiveAsync(kioskId, cancellationToken)
            ?? throw new NotFoundException("Quiosque não encontrado");
        var now = clock.UtcNow;

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var reservation = await reservations.GetByCodeForUpdateAsync(code.Trim().ToUpperInvariant(), ct)
                ?? throw new NotFoundException(ReservationLookup.NotFoundMessage);

            // ADR-001: libera a data se houver uma reserva pendente vencida nela.
            await reservations.ExpireOverduePendingAsync(kiosk.Id, date, now, ct);
            reservation.Reschedule(kiosk, date, now);

            // RN01: data ocupada vira DateUnavailableException (índice único parcial).
            await unitOfWork.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);
    }
}
