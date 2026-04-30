using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Weblog.Core.Api.Services;

public class MinIOService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private readonly string _endpoint;
    private readonly string _publicUrl;
    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private bool _bucketChecked;

    public MinIOService(IConfiguration configuration)
    {
        var minioConfig = configuration.GetSection("MinIO");
        var endpointUrl = minioConfig["Endpoint"] ?? "http://127.0.0.1:9000";
        if (!endpointUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            endpointUrl = "http://" + endpointUrl;
        }

        var endpoint = endpointUrl.Replace("http://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("https://", "", StringComparison.OrdinalIgnoreCase);

        var accessKey = minioConfig["AccessKey"] ?? "your_access_key";
        var secretKey = minioConfig["SecretKey"] ?? "your_secret_key";

        _bucketName = minioConfig["BucketName"] ?? "weblog";
        _endpoint = endpointUrl;
        _publicUrl = minioConfig["PublicUrl"] ?? endpointUrl;

        var clientBuilder = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey);

        if (endpointUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            clientBuilder = clientBuilder.WithSSL();
        }

        _minioClient = clientBuilder.Build();

        Console.WriteLine($"MinIOService initialized with endpoint: {_endpoint}, bucket: {_bucketName}");
    }

    public async Task<string> UploadFileAsync(string folder, string fileName, byte[] fileData)
    {
        if (fileData.Length == 0)
        {
            throw new ArgumentException("File data is empty.", nameof(fileData));
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var safeExtension = Regex.Replace(extension, @"[^a-zA-Z0-9.]", string.Empty);
        var contentHash = GetContentHash(fileData);
        var safeFolder = string.IsNullOrWhiteSpace(folder)
            ? "uploads"
            : folder.Trim().Trim('/').Replace("\\", "/");
        var objectName = $"{safeFolder}/{contentHash}{safeExtension}";

        await EnsureBucketExistsAsync();

        var existingUrl = await GetExistingFileUrlAsync(objectName);
        if (!string.IsNullOrEmpty(existingUrl))
        {
            Console.WriteLine($"File already exists, returning existing URL: {existingUrl}");
            return existingUrl;
        }

        await UploadAsync(objectName, fileData, safeExtension);

        var url = $"{_publicUrl.TrimEnd('/')}/{_bucketName}/{objectName}";
        Console.WriteLine($"File uploaded successfully: {url}");
        return url;
    }

    private async Task<string?> GetExistingFileUrlAsync(string objectName)
    {
        try
        {
            var statArgs = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName);

            await _minioClient.StatObjectAsync(statArgs);
            return $"{_publicUrl.TrimEnd('/')}/{_bucketName}/{objectName}";
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking existing file: {ex.Message}");
            return null;
        }
    }

    private async Task EnsureBucketExistsAsync()
    {
        if (_bucketChecked)
        {
            return;
        }

        await _bucketLock.WaitAsync();
        try
        {
            if (_bucketChecked)
            {
                return;
            }

            var bucketExist = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));
            if (!bucketExist)
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
                Console.WriteLine($"Bucket created: {_bucketName}");
            }

            _bucketChecked = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MinIO bucket check error: {ex.Message}");
            throw;
        }
        finally
        {
            _bucketLock.Release();
        }
    }

    private async Task UploadAsync(string objectName, byte[] fileData, string extension)
    {
        const int maxRetries = 3;

        for (var i = 0; i < maxRetries; i++)
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

    private static string GetContentHash(byte[] data)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
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
