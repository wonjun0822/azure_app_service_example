namespace azure_app_service_example.DTO;

public class FileDownloadDTO
{
    public Stream content { get; set; }
    public string contentType { get; set; }
    public string fileName { get; set; }
}