using Licensing.Domain.Applications;
using Licensing.Domain.Rules;

namespace Licensing.Domain.Tests;

public class RiskAssessorTests
{
    private readonly RiskAssessor _assessor = new(new RiskOptions { Watchlist = ["XY999999"] });

    [Fact]
    public void Low_risk_application_is_auto_approvable()
    {
        Assert.True(_assessor.Assess(TestData.NewApplication(), TestData.Today).Passed);
    }

    [Fact]
    public void Applicant_below_21_needs_manual_review()
    {
        var dob = TestData.Today.AddYears(-21).AddDays(1);
        var app = TestData.NewApplication(TestData.ValidDetails(dob));

        Assert.Contains("Applicant is below 21", _assessor.Assess(app, TestData.Today).Reasons);
    }

    [Fact]
    public void Applicant_turning_21_today_is_not_flagged_for_age()
    {
        var app = TestData.NewApplication(TestData.ValidDetails(TestData.Today.AddYears(-21)));

        Assert.True(_assessor.Assess(app, TestData.Today).Passed);
    }

    [Theory]
    [InlineData(LicenceCategory.A)]
    [InlineData(LicenceCategory.C)]
    [InlineData(LicenceCategory.BE)]
    public void Category_other_than_B_needs_manual_review(LicenceCategory category)
    {
        var app = TestData.NewApplication(category: category);

        Assert.Contains(_assessor.Assess(app, TestData.Today).Reasons, r => r.StartsWith("Licence category"));
    }

    [Theory]
    [InlineData("XY999999")]
    [InlineData(" xy999999 ")]
    public void Watchlisted_national_id_needs_manual_review(string nationalId)
    {
        var app = TestData.NewApplication(TestData.ValidDetails(nationalId: nationalId));

        Assert.Contains("National ID is on the watchlist", _assessor.Assess(app, TestData.Today).Reasons);
    }

    [Fact]
    public void Photo_quality_below_threshold_needs_manual_review()
    {
        var app = TestData.NewApplication(photo: TestData.ValidPhoto(qualityScore: 0.59));

        Assert.Contains(_assessor.Assess(app, TestData.Today).Reasons, r => r.StartsWith("Photo quality score"));
    }

    [Fact]
    public void All_reasons_are_collected()
    {
        var details = TestData.ValidDetails(TestData.Today.AddYears(-19), nationalId: "XY999999");
        var app = TestData.NewApplication(details, LicenceCategory.C, TestData.ValidPhoto(0.4));

        var result = _assessor.Assess(app, TestData.Today);

        Assert.Equal(4, result.Reasons.Count);
        Assert.Contains("; ", result.Details);
    }
}