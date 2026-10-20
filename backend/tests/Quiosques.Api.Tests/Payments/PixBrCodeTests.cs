using Quiosques.Infrastructure.Payments;

namespace Quiosques.Api.Tests.Payments;

// RF05: o copia-e-cola segue o padrão BR Code (EMV) do Banco Central, para ser lido pelos apps dos bancos.
public class PixBrCodeTests
{
    private static readonly PixSettings Settings = new()
    {
        Key = "+5547999998888",
        MerchantName = "José da Silva Conceição Júnior",
        MerchantCity = "São José dos Pinhais",
    };

    [Fact]
    public void Crc16_UsaCcittFalse()
    {
        // Valor de verificação oficial do CRC-16/CCITT-FALSE.
        Assert.Equal(0x29B1, PixBrCode.Crc16("123456789"));
    }

    [Fact]
    public void Build_MontaOsCamposDoBrCode()
    {
        var code = PixBrCode.Build(Settings, 150m, "QSK-7F3K9Q");

        var fields = Parse(code);
        Assert.Equal("01", fields["00"]);
        var account = Parse(fields["26"]);
        Assert.Equal("br.gov.bcb.pix", account["00"]);
        Assert.Equal("+5547999998888", account["01"]);
        Assert.Equal("0000", fields["52"]);
        Assert.Equal("986", fields["53"]);
        Assert.Equal("150.00", fields["54"]);
        Assert.Equal("BR", fields["58"]);
        Assert.Equal("JOSE DA SILVA CONCEICAO J", fields["59"]); // sem acentos, até 25 caracteres
        Assert.Equal("SAO JOSE DOS PI", fields["60"]);           // até 15 caracteres
        Assert.Equal("QSK7F3K9Q", Parse(fields["62"])["05"]);
    }

    [Fact]
    public void Build_TerminaComCrcValido()
    {
        var code = PixBrCode.Build(Settings, 120.5m, "QSK-ABCDEF");

        Assert.EndsWith(PixBrCode.Crc16(code[..^4]).ToString("X4"), code);
        Assert.Contains("5406120.50", code);
    }

    // Lê os campos ID (2) + tamanho (2) + valor.
    private static Dictionary<string, string> Parse(string payload)
    {
        var fields = new Dictionary<string, string>();
        for (var i = 0; i < payload.Length;)
        {
            var id = payload.Substring(i, 2);
            var length = int.Parse(payload.Substring(i + 2, 2));
            fields[id] = payload.Substring(i + 4, length);
            i += 4 + length;
        }

        return fields;
    }
}
