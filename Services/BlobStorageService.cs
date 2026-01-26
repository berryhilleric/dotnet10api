using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Identity;

namespace Api.Services
{
  public interface IBlobStorageService
  {
    Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType);
    Task DeleteImageAsync(string imageUrl);
    string GetImageUrl(string blobName);
  }

  public class BlobStorageService : IBlobStorageService
  {
    private readonly BlobContainerClient _containerClient;
    private readonly string _containerName;

    public BlobStorageService(IConfiguration configuration)
    {
      var storageAccountName = configuration["BlobStorage:AccountName"]
        ?? throw new InvalidOperationException("BlobStorage:AccountName not configured");
      _containerName = configuration["BlobStorage:ContainerName"]
        ?? throw new InvalidOperationException("BlobStorage:ContainerName not configured");
      var connectionString = configuration["BlobStorage:ConnectionString"];

      BlobServiceClient blobServiceClient;

      if (!string.IsNullOrEmpty(connectionString))
      {
        // Use connection string for local development
        blobServiceClient = new BlobServiceClient(connectionString);
      }
      else
      {
        // Use Managed Identity for production
        var blobServiceUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");
        blobServiceClient = new BlobServiceClient(blobServiceUri, new DefaultAzureCredential());
      }

      _containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
    }

    public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType)
    {
      // Generate a unique blob name to avoid conflicts
      var blobName = $"{Guid.NewGuid()}-{fileName}";
      var blobClient = _containerClient.GetBlobClient(blobName);

      // Set content type for proper image rendering in browsers
      var blobHttpHeaders = new BlobHttpHeaders
      {
        ContentType = contentType
      };

      // Upload the image
      await blobClient.UploadAsync(imageStream, new BlobUploadOptions
      {
        HttpHeaders = blobHttpHeaders
      });

      // Return the full URL to the uploaded image
      return blobClient.Uri.ToString();
    }

    public async Task DeleteImageAsync(string imageUrl)
    {
      // Extract blob name from URL
      var uri = new Uri(imageUrl);
      var blobName = uri.Segments[^1]; // Get last segment (blob name)

      var blobClient = _containerClient.GetBlobClient(blobName);
      await blobClient.DeleteIfExistsAsync();
    }

    public string GetImageUrl(string blobName)
    {
      var blobClient = _containerClient.GetBlobClient(blobName);
      return blobClient.Uri.ToString();
    }
  }
}
