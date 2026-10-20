using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quiosques.Domain.Entities;

namespace Quiosques.Infrastructure.Persistence;

// Valores de status gravados no banco, conforme docs/05-modelagem-dados.md.
public static class StatusMapping
{
    public const string PendingPayment = "PENDENTE_PAGAMENTO";
    public const string Confirmed = "CONFIRMADA";
    public const string Expired = "EXPIRADA";
    public const string Cancelled = "CANCELADA";

    public const string PaymentPending = "PENDENTE";
    public const string PaymentApproved = "APROVADO";
    public const string PaymentRefunded = "REEMBOLSADO";

    public const string MethodPix = "PIX";
    public const string MethodAdministration = "ADMINISTRACAO";

    public static readonly ValueConverter<PaymentMethod, string> PaymentMethodConverter = new(
        method => method == PaymentMethod.Administration ? MethodAdministration : MethodPix,
        value => value == MethodAdministration ? PaymentMethod.Administration : PaymentMethod.Pix);

    public static readonly ValueConverter<ReservationStatus, string> ReservationStatusConverter = new(
        status => ToDatabase(status),
        value => ToReservationStatus(value));

    public static readonly ValueConverter<PaymentStatus, string> PaymentStatusConverter = new(
        status => ToDatabase(status),
        value => ToPaymentStatus(value));

    public static string ToDatabase(ReservationStatus status) => status switch
    {
        ReservationStatus.PendingPayment => PendingPayment,
        ReservationStatus.Confirmed => Confirmed,
        ReservationStatus.Expired => Expired,
        ReservationStatus.Cancelled => Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static ReservationStatus ToReservationStatus(string value) => value switch
    {
        PendingPayment => ReservationStatus.PendingPayment,
        Confirmed => ReservationStatus.Confirmed,
        Expired => ReservationStatus.Expired,
        Cancelled => ReservationStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    public static string ToDatabase(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => PaymentPending,
        PaymentStatus.Approved => PaymentApproved,
        PaymentStatus.Refunded => PaymentRefunded,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static PaymentStatus ToPaymentStatus(string value) => value switch
    {
        PaymentPending => PaymentStatus.Pending,
        PaymentApproved => PaymentStatus.Approved,
        PaymentRefunded => PaymentStatus.Refunded,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
