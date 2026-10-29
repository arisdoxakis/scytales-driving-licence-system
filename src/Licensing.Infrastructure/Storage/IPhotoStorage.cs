namespace Licensing.Infrastructure.Storage;

public interface IPhotoStorage
{
    Task<string> SaveAsync(Stream content, string contentType, CancellationToken cancellationToken);
}