using System.Security.Cryptography;
using System.Text;

namespace Weblog.Core.Common.Helpers;

public static class AesUtil
{
    private static readonly string DefaultKey = "YourSecure32CharAESKeyHere!!";

    public static string Encrypt(string plainText, string? key = null)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        var aesKey = key ?? DefaultKey;
        if (aesKey.Length != 32)
            throw new ArgumentException("AES key must be 32 characters.");

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(aesKey);
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string cipherText, string? key = null)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        var aesKey = key ?? DefaultKey;
        if (aesKey.Length != 32)
            throw new ArgumentException("AES key must be 32 characters.");

        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(aesKey);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var iv = new byte[16];
        var cipher = new byte[fullCipher.Length - 16];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }

    public static string HashSha256(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder();
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    public static string DeriveKey(string userKey)
    {
        var hash = HashSha256(userKey);
        return hash.Substring(0, 32);
    }
}
