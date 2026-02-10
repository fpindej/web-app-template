using System.Text.RegularExpressions;

namespace MyProject.Domain;

/// <summary>
/// Provides phone number normalization utilities for consistent comparison and storage.
/// Strips all non-digit characters except the leading '+' sign.
/// </summary>
public static partial class PhoneNumberHelper
{
    /// <summary>
    /// Normalizes a phone number by removing spaces, dashes, parentheses, and other non-digit characters,
    /// preserving only the leading '+' and digits. Returns <c>null</c> if the input is null or whitespace-only.
    /// </summary>
    /// <param name="phoneNumber">The raw phone number string to normalize.</param>
    /// <returns>The normalized phone number, or <c>null</c> if the input is empty or whitespace.</returns>
    public static string? Normalize(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        return NonDigitExceptLeadingPlus().Replace(phoneNumber, string.Empty);
    }

    [GeneratedRegex(@"(?<=.)\+|[^\d+]")]
    private static partial Regex NonDigitExceptLeadingPlus();
}
