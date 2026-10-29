using Licensing.Domain.Applications;

namespace Licensing.Domain.Workflow;

public interface IPhotoQualityAnalyzer
{
    Task<double> AnalyzeAsync(Photo photo, CancellationToken cancellationToken);
}