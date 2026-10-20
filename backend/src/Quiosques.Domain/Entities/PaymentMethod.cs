namespace Quiosques.Domain.Entities;

// Gravado no banco como PIX e ADMINISTRACAO.
public enum PaymentMethod
{
    Pix,

    // RN14: registrado pelo administrador como pago na administração.
    Administration,
}
