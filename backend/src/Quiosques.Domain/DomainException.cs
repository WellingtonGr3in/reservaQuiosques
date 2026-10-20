namespace Quiosques.Domain;

// Violação de uma regra de negócio. A mensagem é exibida ao usuário.
public class DomainException(string message) : Exception(message);
