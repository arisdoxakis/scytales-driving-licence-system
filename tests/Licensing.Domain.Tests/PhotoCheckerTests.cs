using Licensing.Domain.Rules;

namespace Licensing.Domain.Tests;

public class PhotoCheckerTests
{
    private readonly PhotoChecker _checker = new(new PhotoCheckOptions());

    [Fact]
    public void Good_photo_passes() => Assert.True(_checker.Check(TestData.ValidPhoto()).Passed);

    [Fact]
    public void Missing_photo_fails()
    {
        var result = _checker.Check(null);

        Assert.Equal("No photo was provided", Assert.Single(result.Reasons));
    }

    [Fact]
    public void Unsupported_type_fails()
    {
        var result = _checker.Check(TestData.ValidPhoto() with { ContentType = "image/gif" });

        Assert.Contains(result.Reasons, r => r.StartsWith("Unsupported photo type"));
    }

    [Fact]
    public void Too_small_file_fails()
    {
        var result = _checker.Check(TestData.ValidPhoto() with { SizeBytes = 500 });

        Assert.Contains(result.Reasons, r => r.StartsWith("Photo is too small"));
    }

    [Fact]
    public void Quality_below_hard_floor_fails()
    {
        var result = _checker.Check(TestData.ValidPhoto(qualityScore: 0.29));

        Assert.Contains(result.Reasons, r => r.StartsWith("Photo quality score"));
    }

    [Fact]
    public void Quality_between_floor_and_auto_approval_threshold_still_passes_photo_check()
    {
        // 0.45 is above the 0.3 floor; the risk step decides about manual review.
        Assert.True(_checker.Check(TestData.ValidPhoto(qualityScore: 0.45)).Passed);
    }

    [Fact]
    public void Unscored_photo_is_a_programming_error()
    {
        Assert.Throws<InvalidOperationException>(() => _checker.Check(TestData.ValidPhoto(qualityScore: null)));
    }
}