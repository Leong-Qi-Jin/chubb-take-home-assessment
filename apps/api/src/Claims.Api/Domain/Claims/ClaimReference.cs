using System.Security.Cryptography;

namespace Claims.Api.Domain.Claims;

public static class ClaimReference
{
    // Crockford Base32 omits I, L, O, and U, so references are hard to misread when typed or read aloud.
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    /// <summary>
    /// Creates an unguessable public reference such as <c>CLM-7Q4K-9XRM-2PDA</c>. Its 60 random bits make collisions negligible; the unique index rejects any that occur.
    /// </summary>
    public static string Generate()
    {
        var value = RandomNumberGenerator.GetString(Alphabet, 12);
        return $"CLM-{value[..4]}-{value[4..8]}-{value[8..]}";
    }
}
