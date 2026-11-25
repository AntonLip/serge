namespace CleanDistrict.Application;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default);
}
