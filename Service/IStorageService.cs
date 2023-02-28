using azure_app_service_example.DTO;

namespace azure_app_service_example.Service;

public interface IStorageService
{
    // storageService 하위 모듈 구분을 위한 변수
    public string storageType {
        get;
    }

    // Upload
    Task UploadFile(IFormFile file);

    // Download
    Task<FileDownloadDTO> DownloadFile(string fileName);
    //Task DownloadFile(string fileName);

    // Delete
    Task DeleteFile(string fileName);
}
