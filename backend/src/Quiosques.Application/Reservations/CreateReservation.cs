using System.Text.RegularExpressions;
using Quiosques.Application.Common;
using Quiosques.Application.Customers;
using Quiosques.Application.Kiosks;
using Quiosques.Application.Payments;
using Quiosques.Domain.Entities;
using Quiosques.Domain.Rules;
using Quiosques.Domain.ValueObjects;

namespace Quiosques.Application.Reservations;

public record CreateReservationCommand(Guid KioskId, DateOnly Date, string? Cpf, string? Name, string? Email, string? Phone);

public record CreatedReservation(string Code, Guid KioskId, DateOnly Date, decimal Amount, ReservationStatus Status, DateTimeOffset ExpiresAt);

// HU02 / RF04: reservar sem conta, informando CPF, nome, e-mail e telefone.
// HU06 / RF13: o administrador usa o mesmo fluxo, com as exceções do RN13 e do RN14.
public partial class CreateReservation(
    IKioskRepository kiosks,
    ICustomerRepository customers,
    IReservationRepository reservations,
    IPaymentRepository payments,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    private const int MaxCodeAttempts = 3;

    public Task<CreatedReservation> ExecuteAsync(CreateReservationCommand command, CancellationToken cancellationToken) =>
        BookAsync(command, byAdministrator: false, paid: false, cancellationToken);

    // HU05: cliente logado reserva sempre com o CPF da própria conta; as demais regras são as mesmas.
    public async Task<CreatedReservation> ExecuteForCustomerAsync(Guid customerId, CreateReservationCommand command, CancellationToken cancellationToken)
    {
        var customer = await customers.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException("Cliente não encontrado");
        return await BookAsync(command with { Cpf = customer.Cpf }, byAdministrator: false, paid: false, cancellationToken);
    }

    // RN13: sem a antecedência do RN03 e sem o limite do RN08. RN14: "paid" registra o pagamento feito na administração.
    public Task<CreatedReservation> ExecuteForAdministratorAsync(CreateReservationCommand command, bool paid, CancellationToken cancellationToken) =>
        BookAsync(command, byAdministrator: true, paid, cancellationToken);

    private async Task<CreatedReservation> BookAsync(CreateReservationCommand command, bool byAdministrator, bool paid, CancellationToken cancellationToken)
    {
        var cpf = Cpf.Parse(command.Cpf); // RN04
        var (name, email, phone) = ValidateContact(command);

        var kiosk = await kiosks.GetActiveAsync(command.KioskId, cancellationToken)
            ?? throw new NotFoundException("Quiosque não encontrado");

        var now = clock.UtcNow;
        var today = clock.TodayInSaoPaulo();

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    // RN12: o cadastro do CPF é criado ou atualizado com os dados mais recentes.
                    var customer = await customers.GetOrCreateForUpdateAsync(new Customer(cpf.Value, name, email, phone, now), ct);
                    customer.UpdateContact(name, email, phone);

                    Reservation reservation;
                    if (byAdministrator)
                    {
                        reservation = Reservation.CreateByAdministrator(ReservationCode.Generate(), customer, kiosk, command.Date, now, paid);
                    }
                    else
                    {
                        // RN08
                        CustomerReservationLimit.EnsureCanReserve(
                            await reservations.CountActiveByCustomerAsync(customer.Id, today, now, ct));

                        // RN03 e RN05
                        reservation = Reservation.Create(ReservationCode.Generate(), customer, kiosk, command.Date, today, now);
                    }

                    await reservations.ExpireOverduePendingAsync(kiosk.Id, command.Date, now, ct);
                    reservations.Add(reservation);
                    if (paid)
                    {
                        payments.Add(Payment.RegisteredByAdministration(reservation, now)); // RN14
                    }

                    // RN01: garantido pelo índice único parcial; a violação vira DateUnavailableException.
                    await unitOfWork.SaveChangesAsync(ct);

                    return new CreatedReservation(
                        reservation.Code, kiosk.Id, reservation.ReservationDate, reservation.Amount, reservation.Status, reservation.ExpiresAt);
                }, cancellationToken);
            }
            catch (DuplicateReservationCodeException) when (attempt < MaxCodeAttempts)
            {
                // Colisão de código: tenta de novo com outro código.
            }
        }
    }

    private static (string Name, string Email, string Phone) ValidateContact(CreateReservationCommand command)
    {
        var name = command.Name?.Trim() ?? "";
        if (name.Length is 0 or > 150)
        {
            throw new ValidationException("Nome inválido");
        }

        var email = command.Email?.Trim() ?? "";
        if (email.Length > 254 || !EmailPattern().IsMatch(email))
        {
            throw new ValidationException("E-mail inválido");
        }

        var phone = new string((command.Phone ?? "").Where(char.IsAsciiDigit).ToArray());
        if (phone.Length is not (10 or 11))
        {
            throw new ValidationException("Telefone inválido");
        }

        return (name, email, phone);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
