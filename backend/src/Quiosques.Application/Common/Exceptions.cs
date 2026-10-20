namespace Quiosques.Application.Common;

// Exceções da aplicação. A Api traduz cada uma para um status HTTP; a mensagem é exibida ao usuário.

// Dados de entrada malformados (HTTP 400).
public class ValidationException(string message) : Exception(message);

// Recurso inexistente (HTTP 404).
public class NotFoundException(string message) : Exception(message);

// Credenciais inválidas (HTTP 401).
public class UnauthorizedException(string message) : Exception(message);

// Conflito com o estado atual (HTTP 409).
public class ConflictException(string message) : Exception(message);

// RN09: o índice ux_conta_cliente_id rejeitou a segunda conta do mesmo CPF.
public class AccountAlreadyExistsException() : ConflictException(Text)
{
    public const string Text = "Este CPF já possui conta. Faça login.";
}

// O índice ux_conta_email_login rejeitou um e-mail de login já usado por outra conta.
public class LoginEmailInUseException() : ConflictException(Text)
{
    public const string Text = "Este e-mail já está em uso por outra conta";
}

// RN01: o índice ux_reserva_quiosque_data_ativa rejeitou a reserva (ADR-001).
public class DateUnavailableException() : ConflictException(Text)
{
    public const string Text = "Data indisponível";
}

// O código gerado já existe; o caso de uso tenta de novo com outro código.
public class DuplicateReservationCodeException() : Exception("Código de reserva duplicado");
