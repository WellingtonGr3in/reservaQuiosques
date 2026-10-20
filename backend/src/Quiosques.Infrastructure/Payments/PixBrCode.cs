using System.Globalization;
using System.Text;

namespace Quiosques.Infrastructure.Payments;

// Recebedor do Pix (configuração "Pix" no appsettings / docker-compose).
public class PixSettings
{
    public const string Section = "Pix";

    // Chave Pix: CPF, e-mail, telefone (+55...) ou chave aleatória. Vazia = código simulado.
    public string Key { get; set; } = "";
    public string MerchantName { get; set; } = "";
    public string MerchantCity { get; set; } = "";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Key);
}

// Código "Pix copia-e-cola" (BR Code) de um Pix estático, no padrão EMV do Banco Central.
// O mesmo texto é o conteúdo do QR Code. Pix estático não avisa quando foi pago: a confirmação
// continua vindo do gateway (aqui, simulado).
public static class PixBrCode
{
    public static string Build(PixSettings settings, decimal amount, string transactionReference)
    {
        var merchantAccount = Field("00", "br.gov.bcb.pix") + Field("01", settings.Key.Trim());

        var payload = new StringBuilder()
            .Append(Field("00", "01"))                                   // versão do payload
            .Append(Field("26", merchantAccount))                        // dados da chave Pix
            .Append(Field("52", "0000"))                                 // categoria do recebedor
            .Append(Field("53", "986"))                                  // moeda: real
            .Append(Field("54", amount.ToString("0.00", CultureInfo.InvariantCulture)))
            .Append(Field("58", "BR"))
            .Append(Field("59", Clean(settings.MerchantName, 25)))
            .Append(Field("60", Clean(settings.MerchantCity, 15)))
            .Append(Field("62", Field("05", TransactionId(transactionReference))))
            .Append("6304")                                              // CRC16, calculado sobre tudo antes dele
            .ToString();

        return payload + Crc16(payload).ToString("X4");
    }

    // Campo no formato ID (2 dígitos) + tamanho (2 dígitos) + valor.
    private static string Field(string id, string value) => $"{id}{value.Length:D2}{value}";

    // Identificador da transação: só letras e números, até 25 caracteres (ex.: QSK7F3K9Q).
    private static string TransactionId(string reference)
    {
        var id = new string(reference.Where(char.IsAsciiLetterOrDigit).ToArray());
        return id.Length == 0 ? "***" : id[..Math.Min(id.Length, 25)];
    }

    // Nome e cidade: maiúsculas, sem acentos, no tamanho máximo do padrão.
    private static string Clean(string value, int maxLength)
    {
        var withoutAccents = new string(value.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());
        var cleaned = withoutAccents.Trim().ToUpperInvariant();
        return cleaned[..Math.Min(cleaned.Length, maxLength)];
    }

    // CRC-16/CCITT-FALSE (polinômio 0x1021, valor inicial 0xFFFF), exigido pelo BR Code.
    public static ushort Crc16(string text)
    {
        ushort crc = 0xFFFF;
        foreach (var b in Encoding.UTF8.GetBytes(text))
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
            }
        }

        return crc;
    }
}
