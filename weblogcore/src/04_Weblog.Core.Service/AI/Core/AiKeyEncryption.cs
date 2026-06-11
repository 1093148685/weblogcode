using System.Security.Cryptography;
using System.Text;

namespace Weblog.Core.Service.AI.Core;

public interface IAiKeyEncryptionService
{
    string Encrypt(string plainText, string? key = null);
    string Decrypt(string cipherText, string? key = null);
    EncryptionType CurrentEncryptionType { get; }
}

public class DpapiKeyEncryptionService : IAiKeyEncryptionService
{
    public EncryptionType CurrentEncryptionType => EncryptionType.DPAPI;

    public string Encrypt(string plainText, string? key = null)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        var bytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    public string Decrypt(string cipherText, string? key = null)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        try
        {
            var bytes = Convert.FromBase64String(cipherText);
            var decrypted = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            // If decryption fails, return as-is (could be plain text)
            return cipherText;
        }
    }
}

public class AesKeyEncryptionService : IAiKeyEncryptionService
{
    private readonly string _key;

    public AesKeyEncryptionService(string key)
    {
        _key = key;
    }

    public EncryptionType CurrentEncryptionType => EncryptionType.AES;

    public string Encrypt(string plainText, string? key = null)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key ?? _key));
        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText, string? key = null)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        try
        {
            var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key ?? _key));
            var fullBytes = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = keyBytes;

            var iv = new byte[16];
            var encrypted = new byte[fullBytes.Length - 16];

            Buffer.BlockCopy(fullBytes, 0, iv, 0, 16);
            Buffer.BlockCopy(fullBytes, 16, encrypted, 0, encrypted.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch
        {
            return string.Empty;
        }
    }
}

public class AiKeyEncryptionFactory
{
    public static IAiKeyEncryptionService Create(EncryptionType type, string? aesKey = null)
    {
        return type switch
        {
            EncryptionType.DPAPI => new DpapiKeyEncryptionService(),
            EncryptionType.AES when !string.IsNullOrEmpty(aesKey) => new AesKeyEncryptionService(aesKey),
            _ => new DpapiKeyEncryptionService()
        };
    }
}