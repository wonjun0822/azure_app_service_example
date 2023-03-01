using System.Security.Cryptography;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using azure_app_service_example.DTO;

namespace azure_app_service_example.Service;

public class AmazonStorageService : IStorageService
{
    private readonly AmazonS3Client _amazonS3Client;

    private readonly string _bucketName = "wonjun-s3";
    private readonly string _base64Key = string.Empty;

    public AmazonStorageService(IConfiguration configuration)
    {
        AmazonS3Config config = new AmazonS3Config();

        config.RegionEndpoint = RegionEndpoint.GetBySystemName(configuration["AWS:S3:Region"]);

        _amazonS3Client = new AmazonS3Client(configuration["AWS:S3:AccessKey"], configuration["AWS:S3:SecretAccessKey"], config);

        // 객체 암호화 키ㅈ
        _base64Key = configuration["AWS:S3:EncryptKey"];          
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
            Key = file.FileName,
            ServerSideEncryptionCustomerMethod = ServerSideEncryptionCustomerMethod.AES256,
            ServerSideEncryptionCustomerProvidedKey = _base64Key
        });

        Stream fileStream = file.OpenReadStream();

        // Part의 최소 크기는 5MB
        long contentLength = fileStream.Length;
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
                    InputStream = fileStream
                    // ServerSideEncryptionCustomerMethod = ServerSideEncryptionCustomerMethod.AES256,
                    // ServerSideEncryptionCustomerProvidedKey = _base64Key
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

        // // 일반 Upload
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
            GetObjectRequest request = new GetObjectRequest() {
                BucketName = _bucketName,
                Key = fileName
                // ServerSideEncryptionCustomerMethod = ServerSideEncryptionCustomerMethod.AES256,
                // ServerSideEncryptionCustomerProvidedKey = _base64Key
            };

            using (GetObjectResponse response = await _amazonS3Client.GetObjectAsync(request))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    await response.ResponseStream.CopyToAsync(ms);

                    return new FileDownloadDTO() { content = ms.ToArray(), contentType = "application/octet-stream", fileName = fileName };
                }
            }
        }

        // 버킷 또는 객체가 없을 경우
        catch (AmazonS3Exception)
        {
            return null;
        }

        catch (Exception)
        {
            return null;
        }
    }

    // public string DownloadUrl(string fileName)
    // {
    //     GetPreSignedUrlRequest preSignedUrlRequest = new GetPreSignedUrlRequest {
    //         BucketName = _bucketName,
    //         Key = fileName,
    //         Expires = DateTime.UtcNow.AddMinutes(10)
    //     };

    //     return _amazonS3Client.GetPreSignedURL(preSignedUrlRequest);
    // }

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