using System.Security.Cryptography;
using FluentAssertions;
using LaunchMe.Core.Sync;
using Xunit;

namespace LaunchMe.Core.Tests.Sync;

public class PhoneLinkCryptoTests
{
    [Fact]
    public void EncryptAndDecrypt_RoundTrip_MatchesOriginalText()
    {
        byte[] salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        byte[] key = PhoneLinkCrypto.DeriveKeyFromPin("654321", salt);

        string original = "ApprovalRequest: git push origin main";
        var (ciphertext, nonce, tag) = PhoneLinkCrypto.Encrypt(original, key);

        string decrypted = PhoneLinkCrypto.Decrypt(ciphertext, nonce, tag, key);

        decrypted.Should().Be(original);
    }

    [Fact]
    public void TamperedCiphertext_ThrowsCryptographicException()
    {
        byte[] salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        byte[] key = PhoneLinkCrypto.DeriveKeyFromPin("123456", salt);

        var (ciphertext, nonce, tag) = PhoneLinkCrypto.Encrypt("SensitiveData", key);
        ciphertext[0] ^= 0xFF; // Tamper with ciphertext

        var act = () => PhoneLinkCrypto.Decrypt(ciphertext, nonce, tag, key);

        act.Should().Throw<CryptographicException>();
    }
}
