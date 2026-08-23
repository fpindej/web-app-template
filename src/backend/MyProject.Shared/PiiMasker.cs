namespace MyProject.Shared;

/// <summary>
/// Masks personally identifiable information (PII) before it is exposed to
/// less-privileged callers or persisted outside the user record, such as
/// admin views without the <c>users.view_pii</c> permission, audit metadata,
/// and server-side logs.
/// </summary>
public static class PiiMasker
{
    private const string MaskedPlaceholder = "***";

    /// <summary>
    /// Masks an email address, preserving only the first character of the local part
    /// and the first character of the domain. Example: <c>john@gmail.com</c> becomes <c>j***@g***.com</c>.
    /// </summary>
    /// <param name="email">The email address to mask.</param>
    /// <returns>The masked email, or <c>***</c> if the format is unexpected.</returns>
    public static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return MaskedPlaceholder;
        }

        var atIndex = email.IndexOf('@');
        if (atIndex < 1)
        {
            return MaskedPlaceholder;
        }

        var local = email[0];
        var domain = email[(atIndex + 1)..];

        var dotIndex = domain.LastIndexOf('.');
        if (dotIndex < 1)
        {
            // No TLD separator - mask the entire domain
            return $"{local}***@{domain[0]}***";
        }

        var domainName = domain[0];
        var tld = domain[dotIndex..];

        return $"{local}***@{domainName}***{tld}";
    }

    /// <summary>
    /// Masks a phone number completely. Returns <c>***</c> if a value is present, or <c>null</c> if absent.
    /// </summary>
    /// <param name="phone">The phone number to mask.</param>
    /// <returns><c>***</c> if the phone is non-null; otherwise <c>null</c>.</returns>
    public static string? MaskPhone(string? phone)
    {
        return phone is null ? null : MaskedPlaceholder;
    }
}
