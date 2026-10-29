using Licensing.Domain.Applications;

namespace Licensing.Domain.Rules;

public sealed class PhotoChecker(PhotoCheckOptions options)
{
    public RuleResult Check(Photo? photo)
    {
        if (photo is null)
            return new RuleResult(["No photo was provided"]);

        var reasons = new List<string>();

        if (!options.AllowedContentTypes.Contains(photo.ContentType, StringComparer.OrdinalIgnoreCase))
            reasons.Add($"Unsupported photo type '{photo.ContentType}'");

        if (photo.SizeBytes < options.MinSizeBytes)
            reasons.Add($"Photo is too small ({photo.SizeBytes} bytes, minimum {options.MinSizeBytes})");

        if (photo.QualityScore is null)
            throw new InvalidOperationException("Photo must be scored before it is checked.");

        if (photo.QualityScore < options.MinQualityScore)
            reasons.Add($"Photo quality score {photo.QualityScore:0.00} is below minimum {options.MinQualityScore:0.00}");

        return new RuleResult(reasons);
    }
}