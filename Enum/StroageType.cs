using System.ComponentModel;

namespace azure_app_service_example.Enum;

public enum StorageType
{
    [field:Description("Azure Blob Storage")]
    azure,
    [field:Description("Amazon S3")]
    amazon
    // [field:Description("닉네임")]
    // nickname
}