using System.Security.Cryptography;
using System.Text;

namespace MyProject.Infrastructure.Cryptography;

/// <summary>
/// Provides common hashing utilities for the application.
/// </summary>
internal static class HashHelper
{
    /// <summary>
    /// Computes a SHA-256 hash of the input string and returns it as a lowercase hex string.
    /// </summary>
    /// <param name="input">The string to hash.</param>
    /// <returns>A 64-character lowercase hexadecimal string.</returns>
    public static string Sha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
