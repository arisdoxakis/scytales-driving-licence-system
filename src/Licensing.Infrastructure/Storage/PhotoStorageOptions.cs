namespace Licensing.Infrastructure.Storage;

public sealed class PhotoStorageOptions
{
    public const string SectionName = "PhotoStorage";

    public string RootPath { get; init; } = Path.Combine(Path.GetTempPath(), "licensing-photos");
}