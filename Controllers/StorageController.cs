using azure_app_service_example.Service;

using Microsoft.AspNetCore.Mvc;

using azure_app_service_example.Enum;

namespace azure_app_service_example.Controllers;

/// <summary>
/// 파일
/// </summary>
[ApiController]
[Route("api")]
public class StorageController : ControllerBase
{
    private readonly IEnumerable<IStorageService> _storageService;

    // storageService DI(종속성 주입)
    public StorageController(IEnumerable<IStorageService> storageService)
    {
        _storageService = storageService;
    }

    // 파일 제한 500MB
    [RequestFormLimits(MultipartBodyLengthLimit = 524_288_000)]
    [RequestSizeLimit(524_288_000)]
    [HttpPost("files")]
    public async Task<ActionResult> UploadFile(StorageType stroageType, IFormFile file)
    {
        try
        {
            // 요청에 따른 storageService 호출
            // azure, amazon
            await _storageService.FirstOrDefault(x => x.storageType == stroageType.ToString())?.UploadFile(file)!;

            //return Ok();
            return CreatedAtAction(nameof(DownloadFile), new { fileName = file.FileName }, file);
        }

        catch
        {
            return Problem("파일 업로드 중 오류가 발생했습니다.");
        }
    }

    [HttpGet("files")]
    public async Task<ActionResult> DownloadFile(StorageType stroageType, string fileName)
    {
        try
        {
            var result = await _storageService.FirstOrDefault(x => x.storageType == stroageType.ToString())?.DownloadFile(fileName)!;

            return File(result.content, result.contentType, result.fileName);
        }

        catch
        {
            return Problem("파일 다운로드 중 오류가 발생했습니다.");
        }
    }

    [HttpDelete("files")]
    public async Task<ActionResult> DeleteFile(StorageType stroageType, string fileName)
    {
        try
        {
            await _storageService.FirstOrDefault(x => x.storageType == stroageType.ToString())?.DeleteFile(fileName)!;

            return NoContent();
        }

        catch
        {
            return Problem("파일 다운로드 중 오류가 발생했습니다.");
        }
    }
}