namespace Workbench.Modules.Storage.Services.Implementations;

public class CloudStorageService : IStorageService
{
    private const string StorageFolder = "Cloud";
    private readonly string _storagePath;

    public CloudStorageService(IWebHostEnvironment environment)
    {
        _storagePath = Path.Combine(environment.ContentRootPath, StorageFolder);
        Directory.CreateDirectory(_storagePath);
    }

    public async Task Store(IFormFile file, string key)
    {
        var filePath = GetFilePath(key);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);
    }

    public Task<Stream?> Load(string key)
    {
        var filePath = GetFilePath(key);

        if (!File.Exists(filePath))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteFile(string key)
    {
        var filePath = GetFilePath(key);

        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }

    private string GetFilePath(string key) => Path.Combine(_storagePath, key);
}
