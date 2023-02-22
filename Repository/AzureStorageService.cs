using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using azure_app_service_example.DTO;

namespace azure_app_service_example.Service;

public class AzureStorageService : IStorageService
{
    private readonly BlobServiceClient _blobServiceClient;

    private readonly string _containerName = "article";

    public AzureStorageService(IConfiguration configuration)
    {
        _blobServiceClient = new BlobServiceClient(new Uri(configuration["Azure:Storage"]), null);
    }

    public string storageType {
        get { return "azure"; }
    }

    public async Task<BlobContentInfo?> UploadFile(IFormFile file) 
    {
        try
        {
            BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(_containerName);

            BlobClient blob = container.GetBlobClient(file.FileName);

            await using (Stream? data = file?.OpenReadStream())
            {
                return await blob.UploadAsync(data, false);
            }
        }

        // 해당 Container에 동일한 Blob File 있을 시 에러
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobAlreadyExists)
        {
            return null;
        }

        // 그외 에러
        catch (RequestFailedException ex)
        {
            return null;
        }
    }

    public async Task<FileDownloadDTO?> DownloadFile(string blobName)
    {
        FileDownloadDTO result = new();

        try
        {
            BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(_containerName);

            BlobClient blob = container.GetBlobClient(blobName);

            using (BlobDownloadStreamingResult downloadResult = await blob.DownloadStreamingAsync())
            {
                return new FileDownloadDTO() { content = downloadResult.Content, contentType = downloadResult.Details.ContentType, fileName = blobName };
            }
        }

        // 파일 없을시 에러
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobNotFound)
        {
            return null;
        }
    }

    public async Task DeleteFile(string blobName)
    {
        try
        {
            BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(_containerName);

            BlobClient blob = container.GetBlobClient(blobName);

            await blob.DeleteAsync();
        }

        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobNotFound)
        {
        }
    }
}