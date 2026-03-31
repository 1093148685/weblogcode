using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using System.Security.Cryptography;

namespace Weblog.Core.Api.Services;

public class MinIOService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private readonly string _endpoint;
    private readonly string _publicUrl;
    private readonly object _lock = new object();
    private bool _bucketChecked = false;

    public MinIOService(IConfiguration configuration)
    {
        var minioConfig = configuration.GetSection("MinIO");
        var endpoint = minioConfig["Endpoint"] ?? "127.0.0.1:9000";
        endpoint = endpoint.Replace("http://", "").Replace("https://", "");

        var accessKey = minioConfig["AccessKey"] ?? "your_access_key";
        var secretKey = minioConfig["SecretKey"] ?? "your_secret_key";
        _bucketName = minioConfig["BucketName"] ?? "weblog";

        var endpointUrl = minioConfig["Endpoint"] ?? "http://127.0.0.1:9000";
        if (!endpointUrl.StartsWith("http"))
        {
            endpointUrl = "http://" + endpointUrl;
        }
        _endpoint = endpointUrl;

        _publicUrl = minioConfig["PublicUrl"] ?? endpointUrl;

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .Build();

        Console.WriteLine($"MinIOService initialized with endpoint: {_endpoint}, bucket: {_bucketName}");
    }

    /// <summary>
    /// 上传文件，如果相同内容的文件已存在则返回已存在的 URL
    /// </summary>
    /// <param name="folder">文件夹路径</param>
    /// <param name="fileName">原始文件名</param>
    /// <param name="fileData">文件数据</param>
    /// <returns>文件的 URL</returns>
    public async Task<string> UploadFileAsync(string folder, string fileName, byte[] fileData)
    {
        // 使用文件内容的 MD5 hash 作为文件名，实现去重
        var extension = Path.GetExtension(fileName).ToLower();
        var contentHash = GetContentHash(fileData);
        var objectName = $"{contentHash}{extension}";

        // Ensure bucket exists (thread-safe)
        await EnsureBucketExistsAsync();

        // 检查文件是否已存在
        var existingUrl = await GetExistingFileUrlAsync(objectName);
        if (!string.IsNullOrEmpty(existingUrl))
        {
            Console.WriteLine($"File already exists, returning existing URL: {existingUrl}");
            return existingUrl;
        }

        // 上传新文件
        await UploadAsync(objectName, fileData, extension);

        var url = $"{_publicUrl}/{_bucketName}/{objectName}";
        Console.WriteLine($"File uploaded successfully: {url}");
        return url;
    }

    /// <summary>
    /// 检查文件是否已存在于 MinIO
    /// </summary>
    private async Task<string?> GetExistingFileUrlAsync(string objectName)
    {
        try
        {
            var statArgs = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName);

            await _minioClient.StatObjectAsync(statArgs);

            // 文件存在，返回 URL
            return $"{_publicUrl}/{_bucketName}/{objectName}";
        }
        catch (ObjectNotFoundException)
        {
            // 文件不存在
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking existing file: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 确保桶存在
    /// </summary>
    private async Task EnsureBucketExistsAsync()
    {
        if (_bucketChecked) return;

        lock (_lock)
        {
            if (_bucketChecked) return;

            try
            {
                var bucketExist = _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName)).GetAwaiter().GetResult();
                if (!bucketExist)
                {
                    _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName)).GetAwaiter().GetResult();
                    Console.WriteLine($"Bucket created: {_bucketName}");
                }
                _bucketChecked = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MinIO bucket check error: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// 执行实际上传
    /// </summary>
    private async Task UploadAsync(string objectName, byte[] fileData, string extension)
    {
        int maxRetries = 3;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                using var stream = new MemoryStream(fileData);
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName)
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length)
                    .WithContentType(GetContentType(extension));

                await _minioClient.PutObjectAsync(putObjectArgs);
                return;
            }
            catch (MinioException ex)
            {
                Console.WriteLine($"MinIO upload attempt {i + 1} failed: {ex.Message}");
                if (i < maxRetries - 1)
                {
                    await Task.Delay(500 * (i + 1));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MinIO upload attempt {i + 1} failed (non-MinioException): {ex.Message}");
                if (i < maxRetries - 1)
                {
                    await Task.Delay(500 * (i + 1));
                }
            }
        }

        throw new Exception("MinIO upload failed after multiple attempts");
    }

    /// <summary>
    /// 计算文件内容的 MD5 Hash
    /// </summary>
    private string GetContentHash(byte[] data)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    private string GetContentType(string extension)
    {
        return extension.ToLower() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            ".mp4" => "video/mp4",
            ".mp3" => "audio/mpeg",
            _ => "application/octet-stream"
        };
    }
}
