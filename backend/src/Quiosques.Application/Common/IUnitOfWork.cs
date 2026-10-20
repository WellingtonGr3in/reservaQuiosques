namespace Quiosques.Application.Common;

public interface IUnitOfWork
{
    // Executa a operação numa transação do banco. Se ela falhar, nada é gravado.
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);

    // Lança DateUnavailableException ou DuplicateReservationCodeException quando o banco rejeita a gravação.
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
