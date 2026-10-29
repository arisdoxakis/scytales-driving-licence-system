namespace Licensing.Infrastructure.Storage;

internal sealed class LocalFilePhotoStorage(PhotoStorageOptions options) : IPhotoStorage
{
    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
    };

    public async Task<string> SaveAsync(Stream content, string contentType, CancellationToken cancellationToken)
    {
        var relativePath = $"{Guid.NewGuid():N}{Extensions.GetValueOrDefault(contentType, ".bin")}";
        var fullPath = Resolve(relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);

        return relativePath;
    }

    public Task<byte[]> ReadAllBytesAsync(string relativePath, CancellationToken cancellationToken) => File.ReadAllBytesAsync(Resolve(relativePath), cancellationToken);

    private string Resolve(string relativePath)
    {
        var root = Path.GetFullPath(options.RootPath);
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));

        if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Photo path escapes the storage root.");

        return fullPath;
    }
}