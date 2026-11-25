using System.Security.Cryptography;
using System.Text;

namespace LaunchMe.Core.Sync;

/// <summary>
/// End-to-end authenticated cryptographic encryption for cross-device Phone Link payloads.
/// </summary>
public static class PhoneLinkCrypto
{
    public static byte[] DeriveKeyFromPin(string pin, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(pin, salt, 10000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32); // 256-bit key
    }

    public static (byte[] Ciphertext, byte[] Nonce, byte[] Tag) Encrypt(string plaintext, byte[] key)
    {
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] nonce = new byte[12]; // 96-bit nonce for AES-GCM
        RandomNumberGenerator.Fill(nonce);

        byte[] ciphertext = new byte[plaintextBytes.Length];
        byte[] tag = new byte[16]; // 128-bit authentication tag

        using var aesGcm = new AesGcm(key, 16);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        return (ciphertext, nonce, tag);
    }

    public static string Decrypt(byte[] ciphertext, byte[] nonce, byte[] tag, byte[] key)
    {
        byte[] decrypted = new byte[ciphertext.Length];

        using var aesGcm = new AesGcm(key, 16);
        aesGcm.Decrypt(nonce, ciphertext, tag, decrypted);

        return Encoding.UTF8.GetString(decrypted);
    }
}
