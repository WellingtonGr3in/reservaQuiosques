using Quiosques.Application.Common;

namespace Quiosques.Application.Admin;

// Onde as imagens enviadas pelo administrador são guardadas. Devolve o endereço público da imagem.
public interface IImageStorage
{
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken);
}

// RF16: o administrador anexa fotos do quiosque (capa e galeria) a partir do computador.
public class UploadKioskImage(IImageStorage storage)
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public const string InvalidImageMessage = "Envie uma imagem JPG, PNG ou WEBP de até 5 MB";

    public async Task<string> ExecuteAsync(Stream content, long length, CancellationToken cancellationToken)
    {
        if (length is <= 0 or > MaxBytes)
        {
            throw new ValidationException(InvalidImageMessage);
        }

        // O tipo é conferido pelos primeiros bytes do arquivo, não pelo nome ou pelo que o navegador informa.
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var extension = DetectExtension(buffer.GetBuffer().AsSpan(0, (int)buffer.Length))
            ?? throw new ValidationException(InvalidImageMessage);

        buffer.Position = 0;
        return await storage.SaveAsync(buffer, extension, cancellationToken);
    }

    private static string? DetectExtension(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF })) return ".jpg";
        if (bytes.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return ".png";
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8)) return ".webp";
        return null;
    }
}
