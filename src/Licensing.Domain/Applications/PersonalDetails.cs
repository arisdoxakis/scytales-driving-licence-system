namespace Licensing.Domain.Applications;

public sealed record PersonalDetails(
    string? FirstName,
    string? LastName,
    DateOnly? DateOfBirth,
    string? NationalId,
    string? Email,
    string? PhoneNumber,
    string? Address);
