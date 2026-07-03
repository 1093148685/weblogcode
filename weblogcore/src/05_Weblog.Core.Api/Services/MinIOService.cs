using System.Buffers;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Weblog.Core.Api.Services;

public class MinIOService
{
    private static readonly TimeSpan StorageOperationTimeout = TimeSpan.FromSeconds(15);

    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private readonly string _endpoint;
    private readonly ILogger<MinIOService> _logger;
    private readonly string _publicUrl;
    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private bool _bucketChecked;

    public MinIOService(IConfiguration configuration, ILogger<MinIOService> logger)
    {
        _logger = logger;
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

        _logger.LogInformation("MinIOService initialized with endpoint: {Endpoint}, bucket: {Bucket}", _endpoint, _bucketName);
    }

    public async Task<string> UploadFileAsync(string folder, string fileName, byte[] fileData)
    {
        if (fileData.Length == 0)
        {
            throw new ArgumentException("File data is empty.", nameof(fileData));
        }

        using var stream = new MemoryStream(fileData, writable: false);
        return await UploadFileAsync(folder, fileName, stream);
    }

    public async Task<string> UploadFileAsync(string folder, string fileName, Stream fileStream)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        if (!fileStream.CanRead)
        {
            throw new ArgumentException("File stream is not readable.", nameof(fileStream));
        }

        if (!fileStream.CanSeek)
        {
            using var bufferedStream = new MemoryStream();
            await fileStream.CopyToAsync(bufferedStream);
            bufferedStream.Position = 0;
            return await UploadFileAsync(folder, fileName, bufferedStream);
        }

        if (fileStream.Length == 0)
        {
            throw new ArgumentException("File stream is empty.", nameof(fileStream));
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var safeExtension = Regex.Replace(extension, @"[^a-zA-Z0-9.]", string.Empty);
        var contentHash = await GetContentHashAsync(fileStream);
        fileStream.Position = 0;
        var safeFolder = string.IsNullOrWhiteSpace(folder)
            ? "uploads"
            : folder.Trim().Trim('/').Replace("\\", "/");
        var objectName = $"{safeFolder}/{contentHash}{safeExtension}";

        _logger.LogInformation(
            "MinIO upload started. Folder={Folder}, ObjectName={ObjectName}, Size={Size}",
            safeFolder,
            objectName,
            fileStream.Length);

        await EnsureBucketExistsAsync();

        var existingUrl = await GetExistingFileUrlAsync(objectName);
        if (!string.IsNullOrEmpty(existingUrl))
        {
            _logger.LogInformation("MinIO object already exists. ObjectName={ObjectName}", objectName);
            return existingUrl;
        }

        await UploadAsync(objectName, fileStream, safeExtension);

        var url = $"{_publicUrl.TrimEnd('/')}/{_bucketName}/{objectName}";
        _logger.LogInformation("MinIO upload completed. ObjectName={ObjectName}, Url={Url}", objectName, url);
        return url;
    }

    private async Task<string?> GetExistingFileUrlAsync(string objectName)
    {
        try
        {
            var statArgs = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName);

            await _minioClient.StatObjectAsync(statArgs).WaitAsync(StorageOperationTimeout);
            return $"{_publicUrl.TrimEnd('/')}/{_bucketName}/{objectName}";
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MinIO stat object failed, will try upload. ObjectName={ObjectName}", objectName);
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

            _logger.LogInformation("MinIO bucket check started. Bucket={Bucket}", _bucketName);
            var bucketExist = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName))
                .WaitAsync(StorageOperationTimeout);
            if (!bucketExist)
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName))
                    .WaitAsync(StorageOperationTimeout);
                _logger.LogInformation("MinIO bucket created. Bucket={Bucket}", _bucketName);
            }

            var policy = @"{
                ""Version"": ""2012-10-17"",
                ""Statement"": [{
                    ""Effect"": ""Allow"",
                    ""Principal"": {""AWS"": [""*""]},
                    ""Action"": [""s3:GetObject""],
                    ""Resource"": [""arn:aws:s3:::" + _bucketName + @"/*""]
                }]
            }";
            await _minioClient.SetPolicyAsync(
                new SetPolicyArgs().WithBucket(_bucketName).WithPolicy(policy))
                .WaitAsync(StorageOperationTimeout);
            _logger.LogInformation("MinIO bucket public-read policy set. Bucket={Bucket}", _bucketName);

            _bucketChecked = true;
            _logger.LogInformation("MinIO bucket check completed. Bucket={Bucket}", _bucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MinIO bucket check error. Bucket={Bucket}", _bucketName);
            throw;
        }
        finally
        {
            _bucketLock.Release();
        }
    }

    private async Task UploadAsync(string objectName, Stream fileStream, string extension)
    {
        const int maxRetries = 3;

        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                if (!fileStream.CanSeek)
                {
                    throw new InvalidOperationException("Upload stream must be seekable.");
                }

                fileStream.Position = 0;
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName)
                    .WithStreamData(fileStream)
                    .WithObjectSize(fileStream.Length)
                    .WithContentType(GetContentType(extension));

                _logger.LogInformation("MinIO put object attempt {Attempt}. ObjectName={ObjectName}, Size={Size}", i + 1, objectName, fileStream.Length);
                await _minioClient.PutObjectAsync(putObjectArgs).WaitAsync(StorageOperationTimeout);
                _logger.LogInformation("MinIO put object completed. ObjectName={ObjectName}", objectName);
                return;
            }
            catch (MinioException ex)
            {
                _logger.LogWarning(ex, "MinIO upload attempt {Attempt} failed. ObjectName={ObjectName}", i + 1, objectName);
                if (i < maxRetries - 1)
                {
                    await Task.Delay(500 * (i + 1));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MinIO upload attempt {Attempt} failed with non-Minio exception. ObjectName={ObjectName}", i + 1, objectName);
                if (i < maxRetries - 1)
                {
                    await Task.Delay(500 * (i + 1));
                }
            }
        }

        throw new Exception("MinIO upload failed after multiple attempts");
    }

    private static async Task<string> GetContentHashAsync(Stream stream)
    {
        if (!stream.CanSeek)
        {
            throw new InvalidOperationException("Hash stream must be seekable.");
        }

        stream.Position = 0;
        using var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        var buffer = ArrayPool<byte>.Shared.Rent(81920);

        try
        {
            while (true)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
                if (read <= 0)
                {
                    break;
                }

                md5.AppendData(buffer, 0, read);
            }

            return Convert.ToHexString(md5.GetHashAndReset()).ToLowerInvariant();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            stream.Position = 0;
        }
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
