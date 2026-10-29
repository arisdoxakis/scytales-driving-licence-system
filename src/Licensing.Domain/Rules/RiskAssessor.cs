using Licensing.Domain.Applications;

namespace Licensing.Domain.Rules;

public sealed class RiskAssessor
{
    private readonly RiskOptions _options;
    private readonly HashSet<string> _watchlist;

    public RiskAssessor(RiskOptions options)
    {
        _options = options;
        _watchlist = options.Watchlist
            .Select(NormalizeNationalId)
            .ToHashSet(StringComparer.Ordinal);
    }

    public RuleResult Assess(Application application, DateOnly today)
    {
        var reasons = new List<string>();
        var d = application.PersonalDetails;

        if (d.DateOfBirth is { } dob && AgeCalculator.AgeOn(dob, today) < _options.MinAgeForAutoApproval)
            reasons.Add($"Applicant is below {_options.MinAgeForAutoApproval}");

        if (application.LicenceCategory is not { } category || !_options.AutoApprovalCategories.Contains(category))
            reasons.Add($"Licence category {application.LicenceCategory} requires manual review");

        if (d.NationalId is not null && _watchlist.Contains(NormalizeNationalId(d.NationalId)))
            reasons.Add("National ID is on the watchlist");

        if (application.Photo?.QualityScore is { } score && score < _options.MinPhotoQualityForAutoApproval)
            reasons.Add($"Photo quality score {score:0.00} is below auto-approval threshold {_options.MinPhotoQualityForAutoApproval:0.00}");

        return new RuleResult(reasons);
    }

    private static string NormalizeNationalId(string id) => id.Trim().ToUpperInvariant();
}