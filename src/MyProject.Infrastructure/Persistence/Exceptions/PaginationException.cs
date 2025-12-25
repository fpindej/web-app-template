namespace MyProject.Infrastructure.Persistence.Exceptions;

/// <summary>
/// Exception thrown when pagination parameters are invalid
/// </summary>
/// <param name="paramName">The name of the parameter that caused the exception</param>
/// <param name="message">The error message that explains the reason for the exception</param>
public class PaginationException(string paramName, string message) : ArgumentOutOfRangeException(paramName, message);
