using System.Security.Cryptography;
using FluentAssertions;
using LaunchMe.Core.Sync;
using Xunit;

namespace LaunchMe.Core.Tests.Sync;

public class LeashCryptoTests
{
    [Fact]
    public void EncryptAndDecrypt_RoundTrip_MatchesOriginalText()
    {
        byte[] salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        byte[] key = LeashCrypto.DeriveKeyFromPin("654321", salt);

        string original = "PookieRequest: git push origin main";
        var (ciphertext, nonce, tag) = LeashCrypto.Encrypt(original, key);

        string decrypted = LeashCrypto.Decrypt(ciphertext, nonce, tag, key);

        decrypted.Should().Be(original);
    }

    [Fact]
    public void TamperedCiphertext_ThrowsCryptographicException()
    {
        byte[] salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        byte[] key = LeashCrypto.DeriveKeyFromPin("123456", salt);

        var (ciphertext, nonce, tag) = LeashCrypto.Encrypt("SensitiveData", key);
        ciphertext[0] ^= 0xFF;

        var act = () => LeashCrypto.Decrypt(ciphertext, nonce, tag, key);

        act.Should().Throw<CryptographicException>();
    }
}
