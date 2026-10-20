using System.Linq.Expressions;
using Quiosques.Domain.Rules;

namespace Quiosques.Domain.Entities;

// Tabela "reserva". Uma reserva é uma diária (RN02), por isso só guarda a data.
public class Reservation
{
    // RN05: a data fica bloqueada por 15 minutos enquanto aguarda o pagamento.
    public static readonly TimeSpan PaymentDeadline = TimeSpan.FromMinutes(15);

    public const string OutsideBookingWindowMessage = "A reserva deve ser feita com 2 a 90 dias de antecedência";
    public const string NotAwaitingPaymentMessage = "Esta reserva não está aguardando pagamento";
    public const string CannotBeCancelledMessage = "Esta reserva não pode ser cancelada";
    public const string CannotBeChangedMessage = "Só reservas ativas podem ser alteradas";

    // RN01: reserva ativa é a confirmada ou a aguardando pagamento.
    // RN05: a aguardando pagamento só conta enquanto o prazo de 15 minutos não terminou,
    // mesmo que o job de expiração ainda não tenha rodado (ADR-001).
    public static Expression<Func<Reservation, bool>> IsActiveAt(DateTimeOffset now) =>
        r => r.Status == ReservationStatus.Confirmed
            || (r.Status == ReservationStatus.PendingPayment && r.ExpiresAt > now);

    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public Guid KioskId { get; private set; }
    public DateOnly ReservationDate { get; private set; }
    public decimal Amount { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Customer Customer { get; private set; } = null!;
    public Kiosk Kiosk { get; private set; } = null!;
    public ICollection<Payment> Payments { get; private set; } = [];

    private Reservation() { }

    // HU02: a reserva nasce aguardando pagamento, com o valor da diária do momento (DER).
    public static Reservation Create(string code, Customer customer, Kiosk kiosk, DateOnly date, DateOnly today, DateTimeOffset now)
    {
        // RN03
        if (!BookingWindow.Contains(date, today))
        {
            throw new DomainException(OutsideBookingWindowMessage);
        }

        return new Reservation(
            code, customer.Id, kiosk.Id, date, kiosk.DailyRate, ReservationStatus.PendingPayment, now + PaymentDeadline, now);
    }

    // RF13 / RN13: o administrador reserva para qualquer data, sem a antecedência do RN03.
    // RN14: "paga na administração" já nasce confirmada; senão aguarda o Pix como qualquer reserva (RN05).
    public static Reservation CreateByAdministrator(string code, Customer customer, Kiosk kiosk, DateOnly date, DateTimeOffset now, bool paid) =>
        new(code, customer.Id, kiosk.Id, date, kiosk.DailyRate,
            paid ? ReservationStatus.Confirmed : ReservationStatus.PendingPayment, now + PaymentDeadline, now);

    // RF14: o administrador muda o quiosque e/ou a data de uma reserva ativa. O valor continua o do momento da reserva.
    // A data nova continua sujeita ao RN01 (garantido pelo índice único parcial no banco).
    public void Reschedule(Kiosk kiosk, DateOnly date, DateTimeOffset now)
    {
        if (!IsActive(now))
        {
            throw new DomainException(CannotBeChangedMessage);
        }

        KioskId = kiosk.Id;
        ReservationDate = date;
    }

    // RN13: o administrador cancela a qualquer momento, desde que a reserva esteja ativa.
    public void EnsureCanBeCancelledByAdministrator(DateTimeOffset now)
    {
        if (!IsActive(now))
        {
            throw new DomainException(CannotBeCancelledMessage);
        }
    }

    public DateTimeOffset CancellationDeadline => CancellationPolicy.DeadlineFor(ReservationDate);

    // RN07: só reservas ativas podem ser canceladas, e só até 48h antes do dia reservado.
    public bool CanBeCancelledAt(DateTimeOffset now) => IsActive(now) && now <= CancellationDeadline;

    public void EnsureCanBeCancelled(DateTimeOffset now)
    {
        if (!IsActive(now))
        {
            throw new DomainException(CannotBeCancelledMessage);
        }

        if (now > CancellationDeadline)
        {
            throw new DomainException(CancellationPolicy.DeadlinePassedMessage);
        }
    }

    // Mesma regra de IsActiveAt, para uma reserva já carregada em memória.
    private bool IsActive(DateTimeOffset now) =>
        Status == ReservationStatus.Confirmed || (Status == ReservationStatus.PendingPayment && ExpiresAt > now);

    // RN05: só se gera cobrança enquanto a reserva aguarda pagamento e o prazo não terminou.
    public void EnsureAwaitingPayment(DateTimeOffset now)
    {
        if (Status != ReservationStatus.PendingPayment || ExpiresAt <= now)
        {
            throw new DomainException(NotAwaitingPaymentMessage);
        }
    }

    public Reservation(
        string code,
        Guid customerId,
        Guid kioskId,
        DateOnly reservationDate,
        decimal amount,
        ReservationStatus status,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt)
    {
        Id = Guid.CreateVersion7();
        Code = code;
        CustomerId = customerId;
        KioskId = kioskId;
        ReservationDate = reservationDate;
        Amount = amount;
        Status = status;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
    }
}
