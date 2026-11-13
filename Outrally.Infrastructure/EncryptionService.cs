using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Outrally.Infrastructure;

public interface IEncryptionService
{
    byte[] Encrypt(string plaintext, string userId);
    string Decrypt(byte[] encryptedData, string userId);
}

/// <summary>
/// AES-256-GCM encryption with per-user HKDF key derivation.
/// Blob format stored in DB: nonce(12 bytes) | tag(16 bytes) | ciphertext
/// Master key MUST be 32-byte base64. Never commit it — use user secrets / Azure config.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _masterKey;

    public EncryptionService(IConfiguration config)
    {
        var keyBase64 = config["Encryption:MasterKey"]
            ?? throw new InvalidOperationException(
                "Encryption:MasterKey is not configured. " +
                "Run: dotnet user-secrets set \"Encryption:MasterKey\" \"$(openssl rand -base64 32)\" --project Outrally.Api");

        _masterKey = Convert.FromBase64String(keyBase64);

        if (_masterKey.Length != 32)
            throw new InvalidOperationException("Encryption:MasterKey must be exactly 32 bytes (256-bit) base64-encoded.");
    }

    // HKDF: MasterKey + UserId → deterministic 256-bit user key (never stored)
    private byte[] DeriveUserKey(string userId)
    {
        var salt = Encoding.UTF8.GetBytes(userId);
        var info = "outrally-medical"u8.ToArray();
        return HKDF.DeriveKey(HashAlgorithmName.SHA256, _masterKey, outputLength: 32, salt: salt, info: info);
    }

    public byte[] Encrypt(string plaintext, string userId)
    {
        var userKey = DeriveUserKey(userId);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);

        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plainBytes.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(userKey, tagSizeInBytes: 16);
        aes.Encrypt(nonce, plainBytes, ciphertext, tag);

        // Blob: [nonce (12)] + [tag (16)] + [ciphertext]
        var blob = new byte[12 + 16 + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, blob, 0, 12);
        Buffer.BlockCopy(tag, 0, blob, 12, 16);
        Buffer.BlockCopy(ciphertext, 0, blob, 28, ciphertext.Length);
        return blob;
    }

    public string Decrypt(byte[] blob, string userId)
    {
        if (blob.Length < 28)
            throw new CryptographicException("Invalid encrypted blob: too short.");

        var userKey = DeriveUserKey(userId);
        var nonce = blob[..12];
        var tag = blob[12..28];
        var ciphertext = blob[28..];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(userKey, tagSizeInBytes: 16);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
