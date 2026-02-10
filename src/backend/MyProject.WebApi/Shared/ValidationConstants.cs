namespace MyProject.WebApi.Shared;

/// <summary>
/// Shared validation constants used across multiple request validators.
/// </summary>
public static class ValidationConstants
{
    /// <summary>
    /// Regex pattern for validating phone numbers. Allows an optional country code prefix
    /// followed by 6–14 digits (e.g. +420123456789).
    /// </summary>
    public const string PhoneNumberPattern = @"^(\+\d{1,3})? ?\d{6,14}$";
}
