namespace Quiosques.Api.Endpoints;

// Formato padrão de erro da API. A mensagem é exibida ao usuário, por isso fica em português.
public record ErrorResponse(string Message);
