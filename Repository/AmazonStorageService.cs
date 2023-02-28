using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using azure_app_service_example.DTO;

namespace azure_app_service_example.Service;

public class AmazonStorageService : IStorageService
{
    private readonly AmazonS3Client _amazonS3Client;

    private readonly string _bucketName = "wonjun-s3";

    public AmazonStorageService(IConfiguration configuration)
    {
        AmazonS3Config config = new AmazonS3Config();

        config.RegionEndpoint = RegionEndpoint.GetBySystemName(configuration["AWS:S3:Region"]);

        _amazonS3Client = new AmazonS3Client(configuration["AWS:S3:AccessKey"], configuration["AWS:S3:SecretAccessKey"], config);
    }

    public string storageType {
        get { return "amazon"; }
    }

    public async Task UploadFile(IFormFile file)
    {
        // MultipartUpload Part Reponse
        List<UploadPartResponse> uploadResponses = new List<UploadPartResponse>();

        // MultipartUpload 설정 초기화
        InitiateMultipartUploadResponse initResponse = await _amazonS3Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest {
            BucketName = _bucketName,
            Key = file.FileName
        });

        // Part의 최소 크기는 5MB
        long contentLength = file.Length;
        long partSize = 5 * (long)Math.Pow(2, 20); // 최소크기 5MB

        try
        {
            long filePosition = 0;

            // 파일을 chunk로 쪼개서 요청 생성
            for (int i = 1; filePosition < contentLength; i++)
            {
                UploadPartRequest uploadRequest = new UploadPartRequest {
                    BucketName = _bucketName,
                    Key = file.FileName,
                    UploadId = initResponse.UploadId,
                    PartNumber = i,
                    PartSize = partSize,
                    FilePosition = filePosition,
                    InputStream = file.OpenReadStream()
                };

                uploadResponses.Add(await _amazonS3Client.UploadPartAsync(uploadRequest));

                filePosition += partSize;
            }

            // 완료 요청 설정
            CompleteMultipartUploadRequest completeRequest = new CompleteMultipartUploadRequest {
                BucketName = _bucketName,
                Key = file.FileName,
                UploadId = initResponse.UploadId
            };

            // 각 part의 ETAG 
            completeRequest.AddPartETags(uploadResponses);

            // 완료 요청을 보내 Upload 마무리
            // 완료 요청을 보내지 않으면 파일이 Upload 되지 않음
            CompleteMultipartUploadResponse completeUploadResponse = await _amazonS3Client.CompleteMultipartUploadAsync(completeRequest);
        }

        catch (Exception ex)
        {
            // 오류 발생 시 Upload 중단
            // 해당 UploadId 폐기
            AbortMultipartUploadRequest abortMPURequest = new AbortMultipartUploadRequest
            {
                BucketName = _bucketName,
                Key = file.FileName,
                UploadId = initResponse.UploadId
            };

            await _amazonS3Client.AbortMultipartUploadAsync(abortMPURequest);
        }

        // 일반 Upload
        // var obj = new PutObjectRequest {
        //     BucketName = _bucketName,
        //     //Key = "/Temp/" + file.FileName,
        //     Key = file.FileName,
        //     InputStream = file.OpenReadStream()
        // };
        
        // PutObjectResponse putObjectResponse = await _amazonS3Client.PutObjectAsync(obj);
    }

    public async Task<FileDownloadDTO?> DownloadFile(string fileName)
    {
        FileDownloadDTO result = new();

        try
        {
            // 객체 다운로드 요청 생성
            GetObjectRequest obj = new GetObjectRequest() {
                BucketName = _bucketName,
                Key = fileName
            };

            // 다운로드
            using (GetObjectResponse objectResponse = await _amazonS3Client.GetObjectAsync(obj))
            {
                return new FileDownloadDTO() { content = objectResponse.ResponseStream, contentType = "application/octet-stream", fileName = fileName };
            }
        }

        // 파일 없을시 에러
        catch
        {
            return null;
        }
    }

    public async Task DeleteFile(string fileName)
    {
        try
        {
            // 객체 삭제
            await _amazonS3Client.DeleteObjectAsync(_bucketName, fileName);
        }

        catch 
        {
        }
    }
}