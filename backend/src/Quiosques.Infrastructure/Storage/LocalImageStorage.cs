using Microsoft.Extensions.Options;
using Quiosques.Application.Admin;

namespace Quiosques.Infrastructure.Storage;

public class ImageStorageSettings
{
    public const string Section = "Storage";

    // Pasta onde as imagens enviadas são gravadas. No Docker, é um volume (docker-compose.yml).
    public string ImagesPath { get; set; } = "uploads";

    // Prefixo público das imagens; a Api serve a pasta nesse endereço.
    public const string PublicPath = "/uploads";
}

// Grava as imagens numa pasta local, com nome aleatório (o nome original do arquivo não é usado).
public class LocalImageStorage(IOptions<ImageStorageSettings> options) : IImageStorage
{
    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken)
    {
        var folder = Path.GetFullPath(options.Value.ImagesPath);
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using (var file = File.Create(Path.Combine(folder, fileName)))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        return $"{ImageStorageSettings.PublicPath}/{fileName}";
    }
}
