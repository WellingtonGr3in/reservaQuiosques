namespace Quiosques.Domain.Entities;

// Tabela "quiosque".
public class Kiosk
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public int Capacity { get; private set; }
    public decimal DailyRate { get; private set; }
    public string PhotoUrl { get; private set; } = null!;
    public string UsageRules { get; private set; } = null!;
    public bool IsActive { get; private set; }

    public ICollection<KioskPhoto> Photos { get; private set; } = [];
    public ICollection<KioskItem> Items { get; private set; } = [];

    private Kiosk() { }

    public Kiosk(string name, string description, int capacity, decimal dailyRate, string photoUrl, string usageRules, bool isActive)
    {
        Id = Guid.CreateVersion7();
        Name = name;
        Description = description;
        Capacity = capacity;
        DailyRate = dailyRate;
        PhotoUrl = photoUrl;
        UsageRules = usageRules;
        IsActive = isActive;
    }

    // RF16: o administrador edita o quiosque. Reservas já feitas mantêm o valor do momento da reserva (DER).
    public void Update(string name, string description, int capacity, decimal dailyRate, string photoUrl, string usageRules, bool isActive)
    {
        Name = name;
        Description = description;
        Capacity = capacity;
        DailyRate = dailyRate;
        PhotoUrl = photoUrl;
        UsageRules = usageRules;
        IsActive = isActive;
    }

    // Substitui os itens inclusos e a galeria de fotos pela lista informada, na ordem recebida.
    public void ReplaceItems(IEnumerable<string> descriptions)
    {
        Items.Clear();
        foreach (var description in descriptions)
        {
            Items.Add(new KioskItem(Id, description));
        }
    }

    public void ReplacePhotos(IEnumerable<string> urls)
    {
        Photos.Clear();
        var order = 1;
        foreach (var url in urls)
        {
            Photos.Add(new KioskPhoto(Id, url, order++));
        }
    }
}
