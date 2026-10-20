namespace Quiosques.Domain.Entities;

// Tabela "quiosque_foto": galeria do detalhe do quiosque (RF02).
public class KioskPhoto
{
    public Guid Id { get; private set; }
    public Guid KioskId { get; private set; }
    public string Url { get; private set; } = null!;
    public int Order { get; private set; }

    private KioskPhoto() { }

    public KioskPhoto(Guid kioskId, string url, int order)
    {
        Id = Guid.CreateVersion7();
        KioskId = kioskId;
        Url = url;
        Order = order;
    }
}
