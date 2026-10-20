using Quiosques.Domain.Entities;

namespace Quiosques.Application.Kiosks;

public record KioskDetails(
    Guid Id,
    string Name,
    string Description,
    int Capacity,
    decimal DailyRate,
    string PhotoUrl,
    string UsageRules,
    IReadOnlyList<string> Items,
    IReadOnlyList<string> Photos);

// RF02: detalhes do quiosque (descrição, itens, fotos e regras de uso).
public class GetKioskDetails(IKioskRepository kiosks)
{
    public async Task<KioskDetails?> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var kiosk = await kiosks.GetActiveWithDetailsAsync(id, cancellationToken);
        return kiosk is null ? null : ToDetails(kiosk);
    }

    // Exige o quiosque carregado com fotos e itens.
    public static KioskDetails ToDetails(Kiosk kiosk) => new(
        kiosk.Id,
        kiosk.Name,
        kiosk.Description,
        kiosk.Capacity,
        kiosk.DailyRate,
        kiosk.PhotoUrl,
        kiosk.UsageRules,
        kiosk.Items.Select(i => i.Description).ToList(),
        kiosk.Photos.OrderBy(p => p.Order).Select(p => p.Url).ToList());
}
