using Quiosques.Application.Admin;
using Quiosques.Domain.Entities;

namespace Quiosques.Application.Reservations;

public interface IReservationRepository
{
    // Datas entre "from" e "to" (inclusive) que têm reserva ativa no quiosque no instante "now" (RN01, RN05).
    Task<IReadOnlySet<DateOnly>> GetActiveReservationDatesAsync(
        Guid kioskId,
        DateOnly from,
        DateOnly to,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    // RN08: reservas ativas do cliente para hoje em diante.
    Task<int> CountActiveByCustomerAsync(Guid customerId, DateOnly today, DateTimeOffset now, CancellationToken cancellationToken);

    // ADR-001: marca como expiradas as reservas pendentes vencidas do quiosque na data,
    // para que não continuem bloqueando o índice até o job de expiração rodar.
    Task ExpireOverduePendingAsync(Guid kioskId, DateOnly date, DateTimeOffset now, CancellationToken cancellationToken);

    void Add(Reservation reservation);

    // Sem login, uma reserva específica é encontrada pelo código + CPF do titular (RN10). Inclui quiosque e cliente.
    Task<Reservation?> GetByCodeAndCpfAsync(string code, string cpf, CancellationToken cancellationToken);

    // Inclui quiosque e cliente.
    Task<Reservation?> GetByCodeAndCustomerAsync(string code, Guid customerId, CancellationToken cancellationToken);

    // RF10: histórico do cliente, mais recentes primeiro. Inclui quiosque e cliente.
    Task<IReadOnlyList<Reservation>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken);

    // Administrador: busca pelo código. Inclui quiosque, cliente e pagamentos.
    Task<Reservation?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    // Administrador: reserva rastreada para alteração (RF14).
    Task<Reservation?> GetByCodeForUpdateAsync(string code, CancellationToken cancellationToken);

    // RF12: reservas filtradas, das datas mais recentes para as mais antigas. Inclui quiosque, cliente e pagamentos.
    Task<IReadOnlyList<Reservation>> SearchAsync(ReservationSearch search, CancellationToken cancellationToken);

    // RN15: se o quiosque já teve alguma reserva.
    Task<bool> AnyForKioskAsync(Guid kioskId, CancellationToken cancellationToken);

    // Inclui quiosque e cliente.
    Task<Reservation?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken);

    // RN06: muda de PENDENTE_PAGAMENTO para CONFIRMADA. UPDATE condicional por status,
    // para não conflitar com a expiração (CLAUDE.md). Devolve false se a reserva não estava pendente.
    Task<bool> TryConfirmAsync(Guid id, CancellationToken cancellationToken);

    // RN07: muda para CANCELADA só se a reserva ainda estiver no status esperado (UPDATE condicional),
    // para não conflitar com a confirmação do pagamento ou a expiração.
    Task<bool> TryCancelAsync(Guid id, ReservationStatus expectedStatus, CancellationToken cancellationToken);

    // RN05: expira todas as reservas pendentes com prazo vencido. Devolve quantas foram expiradas.
    Task<int> ExpireAllOverduePendingAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
