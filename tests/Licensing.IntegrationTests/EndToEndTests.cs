using System.Net;
using Licensing.Domain.Applications;
using static Licensing.Domain.Applications.WorkflowTrigger;

namespace Licensing.IntegrationTests;

[Collection(LicensingCollection.Name)]
public sealed class EndToEndTests(LicensingFixture fixture)
{
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task Valid_application_is_approved_automatically()
    {
        var submitted = await _client.SubmitAsync(ApiClientExtensions.ValidFields(), ApiClientExtensions.HighQualityPhoto);
        Assert.Equal(ApplicationStatus.Submitted, submitted.Status);

        await _client.WaitForStatusAsync(submitted.ApplicationId, ApplicationStatus.Approved);

        var history = await _client.GetHistoryAsync(submitted.ApplicationId);
        Assert.Equal(
            new[] { ApplicationCreated, ValidationStarted, ValidationPassed, PhotoCheckPassed, AutoApproved },
            history.Select(h => h.Event));
    }

    [Fact]
    public async Task Young_applicant_goes_to_manual_review_and_can_be_approved()
    {
        var dateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-19);
        var submitted = await _client.SubmitAsync(ApiClientExtensions.ValidFields(dateOfBirth), ApiClientExtensions.HighQualityPhoto);

        await _client.WaitForStatusAsync(submitted.ApplicationId, ApplicationStatus.PendingManualReview);

        var response = await _client.ApproveAsync(submitted.ApplicationId, "reviewer-42");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var last = (await _client.GetHistoryAsync(submitted.ApplicationId)).Last();
        Assert.Equal(ManuallyApproved, last.Event);
        Assert.Equal("reviewer-42", last.Actor);
    }

    [Fact]
    public async Task Invalid_data_fails_and_cannot_be_approved()
    {
        var fields = ApiClientExtensions.ValidFields();
        fields["Email"] = "bad";
        var submitted = await _client.SubmitAsync(fields, ApiClientExtensions.HighQualityPhoto);

        await _client.WaitForStatusAsync(submitted.ApplicationId, ApplicationStatus.Failed);

        var response = await _client.ApproveAsync(submitted.ApplicationId, "reviewer-42");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Malformed_date_of_birth_is_rejected_by_the_api()
    {
        var fields = ApiClientExtensions.ValidFields();
        fields["DateOfBirth"] = "1990-13-45";

        var response = await _client.PostApplicationAsync(fields, ApiClientExtensions.HighQualityPhoto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_application_returns_404()
    {
        var response = await _client.GetAsync($"/api/applications/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}