using Quiosques.Application.Common;
using Quiosques.Domain.Entities;
using Quiosques.Domain.ValueObjects;

namespace Quiosques.Application.Reservations;

// Sem login, uma reserva específica (pagamento, comprovante e cancelamento) é encontrada pelo código + CPF do titular.
// O código vem da própria tela: da reserva recém-criada ou da lista da consulta por CPF (RN10).
public static class ReservationLookup
{
    public const string NotFoundMessage = "Reserva não encontrada";

    // CPF inválido ou que não bate com o código dá a mesma resposta, para não revelar se o código existe.
    public static async Task<Reservation> FindAsync(
        IReservationRepository reservations, string? code, string? cpf, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || !Cpf.TryParse(cpf, out var parsed))
        {
            throw new NotFoundException(NotFoundMessage);
        }

        return await reservations.GetByCodeAndCpfAsync(code.Trim().ToUpperInvariant(), parsed.Value, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);
    }
}
