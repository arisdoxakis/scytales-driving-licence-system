using System.Text.RegularExpressions;
using Licensing.Domain.Applications;

namespace Licensing.Domain.Rules;

public sealed partial class DataValidator
{
    public const int MinimumAge = 18;

    public RuleResult Validate(Application application, DateOnly today)
    {
        var reasons = new List<string>();
        var d = application.PersonalDetails;

        Require(d.FirstName, "First name", reasons);
        Require(d.LastName, "Last name", reasons);
        Require(d.NationalId, "National ID", reasons);
        Require(d.Email, "Email", reasons);
        Require(d.PhoneNumber, "Phone number", reasons);
        Require(d.Address, "Address", reasons);

        if (application.LicenceCategory is null)
            reasons.Add("Licence category is required");

        if (!string.IsNullOrWhiteSpace(d.Email) && !EmailPattern().IsMatch(d.Email.Trim()))
            reasons.Add("Email is invalid");

        if (d.DateOfBirth is null)
            reasons.Add("Date of birth is required");
        else if (d.DateOfBirth > today)
            reasons.Add("Date of birth is in the future");
        else if (AgeCalculator.AgeOn(d.DateOfBirth.Value, today) < MinimumAge)
            reasons.Add($"Applicant is under {MinimumAge}");

        return new RuleResult(reasons);
    }

    private static void Require(string? value, string field, List<string> reasons)
    {
        if (string.IsNullOrWhiteSpace(value))
            reasons.Add($"{field} is required");
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}