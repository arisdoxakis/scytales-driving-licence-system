using Licensing.Domain.Applications;

namespace Licensing.Domain.Tests;

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 6, 9, 10, 30, 0, TimeSpan.Zero);
    public static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    public static PersonalDetails ValidDetails(DateOnly? dateOfBirth = null, string? nationalId = "AB123456") => new(
        FirstName: "Aris",
        LastName: "Doxakis",
        DateOfBirth: dateOfBirth ?? new DateOnly(1981, 6, 18),
        NationalId: nationalId,
        Email: "aris.doxakis@example.com",
        PhoneNumber: "+30690000000",
        Address: "Chania, Crete, Greece");

    public static Photo ValidPhoto(double? qualityScore = 0.9) =>
        new("photos/face.jpg", "image/jpeg", SizeBytes: 50_000, qualityScore);

    public static Application NewApplication(
        PersonalDetails? details = null,
        LicenceCategory? category = LicenceCategory.B,
        Photo? photo = null,
        bool withoutPhoto = false) =>
        Application.Submit(details ?? ValidDetails(), category, withoutPhoto ? null : photo ?? ValidPhoto(), Now);

    /// <summary>Drives a fresh application into the given status using only valid transitions.</summary>
    public static Application InStatus(ApplicationStatus status)
    {
        var app = NewApplication();
        WorkflowTrigger[] path = status switch
        {
            ApplicationStatus.Submitted => [],
            ApplicationStatus.ValidatingData => [WorkflowTrigger.ValidationStarted],
            ApplicationStatus.CheckingPhoto => [WorkflowTrigger.ValidationStarted, WorkflowTrigger.ValidationPassed],
            ApplicationStatus.RiskAssessment => [WorkflowTrigger.ValidationStarted, WorkflowTrigger.ValidationPassed, WorkflowTrigger.PhotoCheckPassed],
            ApplicationStatus.PendingManualReview => [WorkflowTrigger.ValidationStarted, WorkflowTrigger.ValidationPassed, WorkflowTrigger.PhotoCheckPassed, WorkflowTrigger.ManualReviewRequired],
            ApplicationStatus.Approved => [WorkflowTrigger.ValidationStarted, WorkflowTrigger.ValidationPassed, WorkflowTrigger.PhotoCheckPassed, WorkflowTrigger.AutoApproved],
            ApplicationStatus.Rejected => [WorkflowTrigger.ValidationStarted, WorkflowTrigger.ValidationPassed, WorkflowTrigger.PhotoCheckPassed, WorkflowTrigger.ManualReviewRequired, WorkflowTrigger.ManuallyRejected],
            ApplicationStatus.Failed => [WorkflowTrigger.ValidationStarted, WorkflowTrigger.ValidationFailed],
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };

        foreach (var trigger in path)
            app.Fire(trigger, Now);

        return app;
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}