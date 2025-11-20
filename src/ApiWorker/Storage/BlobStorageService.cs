using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ApiWorker.Storage;

public interface IBlobStorageService
{
    Task<string> UploadImageAsync(IFormFile file, string containerName, CancellationToken ct = default);
    Task<string> UploadAsync(Stream stream, string fileName, string containerName, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string blobPath, string containerName, CancellationToken ct = default);
}

public sealed class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(BlobServiceClient blobServiceClient, ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<string> UploadImageAsync(IFormFile file, string containerName, CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var blobClient = containerClient.GetBlobClient(fileName);

        var blobHttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType };
        
        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = blobHttpHeaders }, ct);

        return blobClient.Uri.ToString();
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string containerName, string contentType, CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var blobClient = containerClient.GetBlobClient(fileName);
        var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };

        if (stream.CanSeek)
            stream.Position = 0;

        await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = blobHttpHeaders }, ct);

        return blobClient.Uri.ToString();
    }

    public async Task<Stream> DownloadAsync(string blobPath, string containerName, CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream, ct);
        memoryStream.Position = 0;

        return memoryStream;
    }
}
