using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Licensing.Api.Endpoints;
using Licensing.Domain.Applications;
using Licensing.Domain.Rules;

namespace Licensing.IntegrationTests;

internal static class ApiClientExtensions
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static readonly byte[] HighQualityPhoto = new byte[20_000]; // any content; the score is fixed in tests

    public static Dictionary<string, string> ValidFields(DateOnly? dateOfBirth = null) => new()
    {
        ["FirstName"] = "John",
        ["LastName"] = "Doe",
        ["DateOfBirth"] = (dateOfBirth ?? new DateOnly(1990, 5, 20)).ToString("yyyy-MM-dd"),
        ["NationalId"] = $"AB{Random.Shared.Next(100000, 999999)}",
        ["Email"] = "john.doe@example.com",
        ["PhoneNumber"] = "+306900000000",
        ["Address"] = "Example Street 10",
        ["LicenceCategory"] = "B",
    };

    public static Task<HttpResponseMessage> PostApplicationAsync(
        this HttpClient client, Dictionary<string, string> fields, byte[]? photo)
    {
        var form = new MultipartFormDataContent();
        foreach (var (name, value) in fields)
            form.Add(new StringContent(value), name);

        if (photo is not null)
        {
            var file = new ByteArrayContent(photo);
            file.Headers.ContentType = new("image/jpeg");
            form.Add(file, "Photo", "face.jpg");
        }

        return client.PostAsync("/api/applications", form);
    }

    public static async Task<SubmitApplicationResponse> SubmitAsync(
        this HttpClient client, Dictionary<string, string> fields, byte[]? photo)
    {
        var response = await client.PostApplicationAsync(fields, photo);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SubmitApplicationResponse>(Json))!;
    }

    public static async Task<ApplicationStatusResponse> WaitForStatusAsync(
        this HttpClient client, Guid applicationId, ApplicationStatus expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        ApplicationStatusResponse? last = null;

        while (DateTime.UtcNow < deadline)
        {
            last = await client.GetFromJsonAsync<ApplicationStatusResponse>($"/api/applications/{applicationId}", Json);
            if (last!.Status == expected)
                return last;
            await Task.Delay(200);
        }

        throw new TimeoutException($"Expected {expected}, but the last status was {last?.Status}.");
    }

    public static async Task<List<HistoryEntryResponse>> GetHistoryAsync(this HttpClient client, Guid applicationId)
        => (await client.GetFromJsonAsync<List<HistoryEntryResponse>>($"/api/applications/{applicationId}/history", Json))!;

    public static Task<HttpResponseMessage> ApproveAsync(this HttpClient client, Guid applicationId, string reviewerId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/applications/{applicationId}/approve");
        request.Headers.Add("X-Reviewer-Id", reviewerId);
        return client.SendAsync(request);
    }
}