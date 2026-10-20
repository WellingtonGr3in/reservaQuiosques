namespace Quiosques.Domain.Entities;

// Tabela "quiosque_item": itens inclusos no quiosque (RF02).
public class KioskItem
{
    public Guid Id { get; private set; }
    public Guid KioskId { get; private set; }
    public string Description { get; private set; } = null!;

    private KioskItem() { }

    public KioskItem(Guid kioskId, string description)
    {
        Id = Guid.CreateVersion7();
        KioskId = kioskId;
        Description = description;
    }
}
