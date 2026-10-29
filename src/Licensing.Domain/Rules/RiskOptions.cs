using Licensing.Domain.Applications;

namespace Licensing.Domain.Rules;

public sealed class RiskOptions
{
    public const string SectionName = "Risk";

    public int MinAgeForAutoApproval { get; init; } = 21;

    public LicenceCategory[] AutoApprovalCategories { get; init; } = [LicenceCategory.B];

    public string[] Watchlist { get; init; } = [];

    public double MinPhotoQualityForAutoApproval { get; init; } = 0.6;
}