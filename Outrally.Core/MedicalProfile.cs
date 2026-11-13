namespace Outrally.Core;

public class MedicalProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    /// <summary>AES-256-GCM blob: nonce(12) + tag(16) + ciphertext</summary>
    public byte[] EncryptedData { get; set; } = Array.Empty<byte>();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
