using Quiosques.Application.Common;
using Quiosques.Application.Kiosks;
using Quiosques.Application.Reservations;
using Quiosques.Domain.Entities;

namespace Quiosques.Application.Admin;

// Quiosque visto pelo administrador: os dados do detalhe (RF02), se está ativo e se já teve reservas (RN15).
public record AdminKiosk(KioskDetails Details, bool IsActive, bool HasReservations);

public record SaveKioskCommand(
    string? Name,
    string? Description,
    int Capacity,
    decimal DailyRate,
    string? PhotoUrl,
    string? UsageRules,
    bool IsActive,
    IReadOnlyList<string>? Items,
    IReadOnlyList<string>? Photos);

// RF16: lista todos os quiosques, inclusive os desativados.
public class ListKiosksForAdministrator(IKioskRepository kiosks, IReservationRepository reservations)
{
    public async Task<IReadOnlyList<AdminKiosk>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var all = await kiosks.ListAllWithDetailsAsync(cancellationToken);
        var result = new List<AdminKiosk>();
        foreach (var kiosk in all)
        {
            result.Add(new AdminKiosk(GetKioskDetails.ToDetails(kiosk), kiosk.IsActive, await reservations.AnyForKioskAsync(kiosk.Id, cancellationToken)));
        }

        return result;
    }
}

// RF16: detalhe de um quiosque, inclusive desativado.
public class GetKioskForAdministrator(IKioskRepository kiosks, IReservationRepository reservations)
{
    public async Task<AdminKiosk> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var kiosk = await kiosks.GetWithDetailsAsync(id, cancellationToken)
            ?? throw new NotFoundException(SaveKiosk.NotFoundMessage);
        return new AdminKiosk(GetKioskDetails.ToDetails(kiosk), kiosk.IsActive, await reservations.AnyForKioskAsync(id, cancellationToken));
    }
}

// RF16: cadastra (id nulo) ou edita um quiosque. Reservas já feitas mantêm o valor do momento da reserva.
public class SaveKiosk(IKioskRepository kiosks, IUnitOfWork unitOfWork)
{
    public const string NotFoundMessage = "Quiosque não encontrado";

    public async Task<Guid> ExecuteAsync(Guid? id, SaveKioskCommand command, CancellationToken cancellationToken)
    {
        var data = Validate(command);

        Kiosk kiosk;
        if (id is null)
        {
            kiosk = new Kiosk(data.Name, data.Description, data.Capacity, data.DailyRate, data.PhotoUrl, data.UsageRules, data.IsActive);
            kiosks.Add(kiosk);
        }
        else
        {
            kiosk = await kiosks.GetWithDetailsForUpdateAsync(id.Value, cancellationToken)
                ?? throw new NotFoundException(NotFoundMessage);
            kiosk.Update(data.Name, data.Description, data.Capacity, data.DailyRate, data.PhotoUrl, data.UsageRules, data.IsActive);
        }

        kiosk.ReplaceItems(data.Items);
        kiosk.ReplacePhotos(data.Photos);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return kiosk.Id;
    }

    private static (string Name, string Description, int Capacity, decimal DailyRate, string PhotoUrl, string UsageRules, bool IsActive,
        List<string> Items, List<string> Photos) Validate(SaveKioskCommand command)
    {
        var name = Required(command.Name, 100, "Nome");
        var description = Required(command.Description, 4000, "Descrição");
        var photoUrl = Required(command.PhotoUrl, 500, "Foto de capa");
        var usageRules = Required(command.UsageRules, 4000, "Regras de uso");

        if (command.Capacity is < 1 or > 1000)
        {
            throw new ValidationException("A capacidade deve ser entre 1 e 1000 pessoas");
        }

        if (command.DailyRate is <= 0 or > 99_999_999.99m || decimal.Round(command.DailyRate, 2) != command.DailyRate)
        {
            throw new ValidationException("Valor da diária inválido");
        }

        var items = Clean(command.Items, 150, "Cada item deve ter até 150 caracteres");
        var photos = Clean(command.Photos, 500, "Cada foto deve ter até 500 caracteres");

        return (name, description, command.Capacity, command.DailyRate, photoUrl, usageRules, command.IsActive, items, photos);
    }

    private static string Required(string? value, int maxLength, string field)
    {
        var trimmed = value?.Trim() ?? "";
        if (trimmed.Length == 0 || trimmed.Length > maxLength)
        {
            throw new ValidationException($"{field} é obrigatório e deve ter até {maxLength} caracteres");
        }

        return trimmed;
    }

    private static List<string> Clean(IReadOnlyList<string>? values, int maxLength, string message)
    {
        var cleaned = (values ?? []).Select(v => v.Trim()).Where(v => v.Length > 0).ToList();
        if (cleaned.Any(v => v.Length > maxLength))
        {
            throw new ValidationException(message);
        }

        return cleaned;
    }
}

// RF16 / RN15: exclui um quiosque que nunca teve reservas; os demais devem ser desativados.
public class DeleteKiosk(IKioskRepository kiosks, IReservationRepository reservations, IUnitOfWork unitOfWork)
{
    public const string HasReservationsMessage = "Este quiosque tem reservas e não pode ser excluído. Desative-o.";

    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var kiosk = await kiosks.GetWithDetailsForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException(SaveKiosk.NotFoundMessage);

        if (await reservations.AnyForKioskAsync(id, cancellationToken))
        {
            throw new ConflictException(HasReservationsMessage);
        }

        kiosks.Remove(kiosk);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
