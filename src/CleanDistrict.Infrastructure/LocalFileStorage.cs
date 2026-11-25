using CleanDistrict.Application;

namespace CleanDistrict.Infrastructure;

public class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(string root)
    {
        _root = root;
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default)
    {
        var safeExtension = Path.GetExtension(extension).ToLowerInvariant();
        var storedName = $"{Guid.NewGuid():N}{safeExtension}";
        await using var output = new FileStream(Path.Combine(_root, storedName), FileMode.CreateNew, FileAccess.Write);
        await content.CopyToAsync(output, cancellationToken);
        return storedName;
    }

    public Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var safeName = Path.GetFileName(storedFileName);
        var path = Path.Combine(_root, safeName);
        Stream? stream = File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
        return Task.FromResult(stream);
    }
}
