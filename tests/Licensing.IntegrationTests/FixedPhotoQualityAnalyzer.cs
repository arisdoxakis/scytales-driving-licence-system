using Licensing.Domain.Applications;
using Licensing.Domain.Workflow;

namespace Licensing.IntegrationTests;

internal sealed class FixedPhotoQualityAnalyzer(double score) : IPhotoQualityAnalyzer
{
    public Task<double> AnalyzeAsync(Photo photo, CancellationToken cancellationToken) => Task.FromResult(score);
}