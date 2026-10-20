namespace Quiosques.Application.Kiosks;

public record KioskSummary(Guid Id, string Name, string PhotoUrl, int Capacity, decimal DailyRate, IReadOnlyList<string> Items);

// RF01: lista os quiosques ativos com nome, foto, capacidade e valor da diária.
// Os itens também vão na resposta porque o card do protótipo ("Lista de Quiosques") mostra alguns deles.
public class ListKiosks(IKioskRepository kiosks)
{
    public async Task<IReadOnlyList<KioskSummary>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var active = await kiosks.ListActiveAsync(cancellationToken);
        return active
            .Select(k => new KioskSummary(
                k.Id, k.Name, k.PhotoUrl, k.Capacity, k.DailyRate,
                k.Items.Select(i => i.Description).ToList()))
            .ToList();
    }
}
