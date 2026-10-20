using Quiosques.Domain.Entities;

namespace Quiosques.Application.Kiosks;

public interface IKioskRepository
{
    Task<IReadOnlyList<Kiosk>> ListActiveAsync(CancellationToken cancellationToken);

    Task<Kiosk?> GetActiveAsync(Guid id, CancellationToken cancellationToken);

    // Inclui fotos e itens (RF02).
    Task<Kiosk?> GetActiveWithDetailsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> IsActiveAsync(Guid id, CancellationToken cancellationToken);

    // Administrador (RF16): todos os quiosques, inclusive desativados, com fotos e itens.
    Task<IReadOnlyList<Kiosk>> ListAllWithDetailsAsync(CancellationToken cancellationToken);

    Task<Kiosk?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken);

    // Rastreado, para editar ou excluir.
    Task<Kiosk?> GetWithDetailsForUpdateAsync(Guid id, CancellationToken cancellationToken);

    void Add(Kiosk kiosk);

    void Remove(Kiosk kiosk);
}
