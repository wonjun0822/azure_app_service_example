using Azure.Storage.Blobs.Models;

using azure_app_service_example.DTO;

namespace azure_app_service_example.Service;

public interface IStorageService
{
    public string storageType {
        get;
    }

    Task<BlobContentInfo> UploadFile(IFormFile file);

    Task<FileDownloadDTO> DownloadFile(string blobName);

    Task DeleteFile(string blobName);

    // /// <summary>
    // /// This method returns a list of all files located in the container
    // /// </summary>
    // /// <returns>Blobs in a list</returns>
    // Task<List<BlobDto>> ListAsync();
}
