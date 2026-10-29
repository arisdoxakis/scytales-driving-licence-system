using Licensing.Domain.Applications;
using Licensing.Domain.Workflow;

namespace Licensing.Infrastructure.Storage;

internal sealed class PhotoQualityAnalyzer : IPhotoQualityAnalyzer
{
    public Task<double> AnalyzeAsync(Photo photo, CancellationToken cancellationToken) => Task.FromResult(Math.Round(Random.Shared.NextDouble(), 2));
}