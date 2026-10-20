using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quiosques.Domain.Entities;
using Quiosques.Infrastructure.Persistence;
using Quiosques.Infrastructure.Persistence.Configurations;

namespace Quiosques.Api.Tests.Infrastructure;

// Etapa 1: confere que a migration cria o banco conforme docs/05-modelagem-dados.md
// e que as restrições do banco garantem as regras que dependem dele.
[Collection(PostgresCollection.Name)]
public class DatabaseSchemaTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly ReservationDate = new(2026, 11, 20);

    [Fact]
    public async Task Migration_CreatesAllTablesFromDer()
    {
        await using var context = fixture.CreateContext();

        var tables = await context.Database
            .SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'public'")
            .ToListAsync();

        Assert.Superset(
            new HashSet<string> { "cliente", "conta", "quiosque", "quiosque_foto", "quiosque_item", "reserva", "pagamento" },
            tables.ToHashSet());
    }

    [Fact]
    public async Task RN01_ActiveReservationIndex_MatchesAdr001()
    {
        await using var context = fixture.CreateContext();

        var definition = await context.Database
            .SqlQueryRaw<string>(
                "SELECT indexdef AS \"Value\" FROM pg_indexes WHERE indexname = {0}",
                ReservationConfiguration.ActiveReservationIndex)
            .SingleAsync();

        Assert.Equal(
            "CREATE UNIQUE INDEX ux_reserva_quiosque_data_ativa ON public.reserva USING btree (quiosque_id, data_reserva) " +
            "WHERE ((status)::text = ANY ((ARRAY['PENDENTE_PAGAMENTO'::character varying, 'CONFIRMADA'::character varying])::text[]))",
            definition);
    }

    [Theory]
    [InlineData(ReservationStatus.PendingPayment, ReservationStatus.PendingPayment)]
    [InlineData(ReservationStatus.PendingPayment, ReservationStatus.Confirmed)]
    [InlineData(ReservationStatus.Confirmed, ReservationStatus.Confirmed)]
    public async Task RN01_SecondActiveReservationForSameKioskAndDate_IsRejected(ReservationStatus existing, ReservationStatus attempted)
    {
        var (customer, kiosk) = await SeedCustomerAndKioskAsync();
        await AddReservationAsync(customer, kiosk, existing);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => AddReservationAsync(customer, kiosk, attempted));

        AssertUniqueViolation(exception, ReservationConfiguration.ActiveReservationIndex);
    }

    [Theory]
    [InlineData(ReservationStatus.Expired)]
    [InlineData(ReservationStatus.Cancelled)]
    public async Task RN01_InactiveReservation_DoesNotBlockTheDate(ReservationStatus inactive)
    {
        var (customer, kiosk) = await SeedCustomerAndKioskAsync();
        await AddReservationAsync(customer, kiosk, inactive);

        await AddReservationAsync(customer, kiosk, ReservationStatus.PendingPayment);

        await using var context = fixture.CreateContext();
        Assert.Equal(2, await context.Reservations.CountAsync(r => r.KioskId == kiosk.Id));
    }

    [Fact]
    public async Task RN01_SameDateOnDifferentKiosks_IsAllowed()
    {
        var (customer, kiosk) = await SeedCustomerAndKioskAsync();
        var otherKiosk = await AddAsync(NewKiosk());
        await AddReservationAsync(customer, kiosk, ReservationStatus.Confirmed);

        await AddReservationAsync(customer, otherKiosk, ReservationStatus.Confirmed);
    }

    [Fact]
    public async Task Rnf03_DuplicateGatewayTransactionId_IsRejected()
    {
        var (customer, kiosk) = await SeedCustomerAndKioskAsync();
        var reservation = await AddReservationAsync(customer, kiosk, ReservationStatus.PendingPayment);
        var transactionId = $"tx-{Guid.NewGuid()}";
        await AddAsync(new Payment(reservation.Id, transactionId, 150m, PaymentStatus.Approved, Now));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => AddAsync(new Payment(reservation.Id, transactionId, 150m, PaymentStatus.Approved, Now)));

        AssertUniqueViolation(exception, "ux_pagamento_transacao_gateway_id");
    }

    [Fact]
    public async Task RN09_SecondAccountForSameCustomer_IsRejected()
    {
        var (customer, _) = await SeedCustomerAndKioskAsync();
        await AddAsync(new Account(customer.Id, $"{Guid.NewGuid()}@exemplo.com", "hash", Now));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => AddAsync(new Account(customer.Id, $"{Guid.NewGuid()}@exemplo.com", "hash", Now)));

        AssertUniqueViolation(exception, "ux_conta_cliente_id");
    }

    [Fact]
    public async Task DuplicateCpf_IsRejected()
    {
        var (customer, _) = await SeedCustomerAndKioskAsync();

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => AddAsync(new Customer(customer.Cpf, "Outra Pessoa", "outra@exemplo.com", "47999990000", Now)));

        AssertUniqueViolation(exception, "ux_cliente_cpf");
    }

    [Fact]
    public async Task RN10_DuplicateReservationCode_IsRejected()
    {
        var (customer, kiosk) = await SeedCustomerAndKioskAsync();
        var otherKiosk = await AddAsync(NewKiosk());
        var reservation = await AddReservationAsync(customer, kiosk, ReservationStatus.Confirmed);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => AddAsync(new Reservation(
            reservation.Code, customer.Id, otherKiosk.Id, ReservationDate, 150m, ReservationStatus.Confirmed, Now, Now)));

        AssertUniqueViolation(exception, "ux_reserva_codigo");
    }

    [Theory]
    [InlineData("reserva", "ck_reserva_status")]
    [InlineData("pagamento", "ck_pagamento_status")]
    public async Task InvalidStatus_IsRejectedByCheckConstraint(string table, string constraint)
    {
        var (customer, kiosk) = await SeedCustomerAndKioskAsync();
        var reservation = await AddReservationAsync(customer, kiosk, ReservationStatus.PendingPayment);
        var payment = await AddAsync(new Payment(reservation.Id, $"tx-{Guid.NewGuid()}", 150m, PaymentStatus.Pending, null));
        FormattableString update = table == "reserva"
            ? (FormattableString)$"UPDATE reserva SET status = 'INVALIDO' WHERE id = {reservation.Id}"
            : (FormattableString)$"UPDATE pagamento SET status = 'INVALIDO' WHERE id = {payment.Id}";

        await using var context = fixture.CreateContext();
        var exception = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlAsync(update));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal(constraint, exception.ConstraintName);
    }

    private async Task<(Customer Customer, Kiosk Kiosk)> SeedCustomerAndKioskAsync()
    {
        var customer = await AddAsync(new Customer(TestData.NextCpf(), "João da Silva", "joao@exemplo.com", "47999998888", Now));
        var kiosk = await AddAsync(NewKiosk());
        return (customer, kiosk);
    }

    private Task<Reservation> AddReservationAsync(Customer customer, Kiosk kiosk, ReservationStatus status) =>
        AddAsync(new Reservation(TestData.NextCode(), customer.Id, kiosk.Id, ReservationDate, kiosk.DailyRate, status, Now.AddMinutes(15), Now));

    private async Task<T> AddAsync<T>(T entity) where T : class
    {
        await using var context = fixture.CreateContext();
        context.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    private static Kiosk NewKiosk() =>
        new("Quiosque A", "Churrasqueira coberta", 30, 150m, "https://exemplo.com/a.jpg", "Uso das 08:00 às 22:00", isActive: true);

    private static void AssertUniqueViolation(DbUpdateException exception, string constraint)
    {
        var postgres = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal(constraint, postgres.ConstraintName);
    }
}
