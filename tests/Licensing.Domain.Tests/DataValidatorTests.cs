using Licensing.Domain.Applications;
using Licensing.Domain.Rules;

namespace Licensing.Domain.Tests;

public class DataValidatorTests
{
    private readonly DataValidator _validator = new();

    [Fact]
    public void Valid_application_passes()
    {
        var result = _validator.Validate(TestData.NewApplication(), TestData.Today);

        Assert.True(result.Passed);
        Assert.Null(result.Details);
    }

    [Fact]
    public void Missing_fields_are_all_reported()
    {
        var details = new PersonalDetails(null, " ", null, null, null, null, null);
        var app = TestData.NewApplication(details, category: null);

        var result = _validator.Validate(app, TestData.Today);

        Assert.False(result.Passed);
        Assert.Contains("First name is required", result.Reasons);
        Assert.Contains("Last name is required", result.Reasons);
        Assert.Contains("National ID is required", result.Reasons);
        Assert.Contains("Email is required", result.Reasons);
        Assert.Contains("Date of birth is required", result.Reasons);
        Assert.Contains("Licence category is required", result.Reasons);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("john@")]
    [InlineData("john@example")]
    [InlineData("john doe@example.com")]
    public void Invalid_email_fails(string email)
    {
        var app = TestData.NewApplication(TestData.ValidDetails() with { Email = email });

        var result = _validator.Validate(app, TestData.Today);

        Assert.Contains("Email is invalid", result.Reasons);
    }

    [Fact]
    public void Applicant_turning_18_today_passes()
    {
        var dob = TestData.Today.AddYears(-18);
        var app = TestData.NewApplication(TestData.ValidDetails(dob));

        Assert.True(_validator.Validate(app, TestData.Today).Passed);
    }

    [Fact]
    public void Applicant_turning_18_tomorrow_fails()
    {
        var dob = TestData.Today.AddYears(-18).AddDays(1);
        var app = TestData.NewApplication(TestData.ValidDetails(dob));

        var result = _validator.Validate(app, TestData.Today);

        Assert.Contains("Applicant is under 18", result.Reasons);
    }

    [Fact]
    public void Date_of_birth_in_the_future_fails()
    {
        var app = TestData.NewApplication(TestData.ValidDetails(TestData.Today.AddDays(1)));

        var result = _validator.Validate(app, TestData.Today);

        Assert.Equal("Date of birth is in the future", Assert.Single(result.Reasons));
    }

    [Fact]
    public void Date_of_birth_today_is_under_18_not_in_the_future()
    {
        var app = TestData.NewApplication(TestData.ValidDetails(TestData.Today));

        var result = _validator.Validate(app, TestData.Today);

        Assert.Equal("Applicant is under 18", Assert.Single(result.Reasons));
    }
}