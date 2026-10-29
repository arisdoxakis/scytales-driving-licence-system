namespace Licensing.Domain.Applications;

public sealed record Photo(
    string StoragePath,
    string ContentType,
    long SizeBytes,
    double? QualityScore = null);
