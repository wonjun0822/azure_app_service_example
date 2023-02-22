using azure_app_service_example.Service;
using Microsoft.AspNetCore.Mvc;

namespace azure_app_service_example.Controllers;

/// <summary>
/// 파일
/// </summary>
[ApiController]
[Route("api")]
public class StorageController : ControllerBase
{
    private readonly IStorageService _storageService;

    public StorageController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpPost("files")]
    public async Task<ActionResult> UploadFile(IFormFile file)
    {
        try
        {
            await _storageService.UploadFile(file);

            return CreatedAtAction(nameof(DownloadFIle), new { fileName = file.FileName }, file);
        }

        catch
        {
            return Problem("파일 다운로드 중 오류가 발생했습니다.");
        }
    }

    [HttpGet("files")]
    public async Task<ActionResult> DownloadFIle(string fileName)
    {
        try
        {
            var result = await _storageService.DownloadFile(fileName);

            return File(result.content, result.contentType, result.fileName);
        }

        catch
        {
            return Problem("파일 다운로드 중 오류가 발생했습니다.");
        }
    }

    [HttpDelete("files")]
    public async Task<ActionResult> DeleteFile(string fileName)
    {
        try
        {
            await _storageService.DeleteFile(fileName);

            return NoContent();
        }

        catch
        {
            return Problem("파일 다운로드 중 오류가 발생했습니다.");
        }
    }
}