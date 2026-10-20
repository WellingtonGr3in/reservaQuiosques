using Quiosques.Domain.ValueObjects;

namespace Quiosques.Domain.Tests.ValueObjects;

// RN04: o CPF informado deve ser válido (dígitos verificadores).
public class CpfTests
{
    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData("52998224725", "52998224725")]
    [InlineData(" 529.982.247-25 ", "52998224725")]
    [InlineData("123.456.789-09", "12345678909")]
    public void RN04_ValidCpf_IsAcceptedAndNormalized(string input, string expected)
    {
        Assert.Equal(expected, Cpf.Parse(input).Value);
    }

    [Theory]
    [InlineData("111.111.111-11")] // sequência repetida (cenário da HU02)
    [InlineData("000.000.000-00")]
    [InlineData("529.982.247-24")] // dígito verificador errado
    [InlineData("529.982.247-35")]
    [InlineData("5299822472")]     // 10 dígitos
    [InlineData("529982247250")]   // 12 dígitos
    [InlineData("529A982247-25")]
    [InlineData("")]
    [InlineData(null)]
    public void RN04_InvalidCpf_IsRejected(string? input)
    {
        var exception = Assert.Throws<DomainException>(() => Cpf.Parse(input));
        Assert.Equal("CPF inválido", exception.Message);
    }

    [Fact]
    public void RNF07_MaskedCpf_HidesFirstAndLastDigits()
    {
        Assert.Equal("***.982.247-**", Cpf.Parse("529.982.247-25").Masked);
    }
}
