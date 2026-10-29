namespace Licensing.Domain.Rules;

public sealed class PhotoCheckOptions
{
    public const string SectionName = "PhotoCheck";

    public string[] AllowedContentTypes { get; init; } = ["image/jpeg", "image/png"];

    public long MinSizeBytes { get; init; } = 10 * 1024;

    public double MinQualityScore { get; init; } = 0.3;
}