using System.Net;
using System.Text.Json;
using MyProject.Infrastructure.Persistence.Exceptions;
using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Middlewares;

/// <summary>
/// Catches unhandled exceptions and maps them to standardized <see cref="ErrorResponse"/> JSON responses.
/// </summary>
/// <remarks>Pattern documented in src/backend/AGENTS.md — update both when changing.</remarks>
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment env)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Invokes the next middleware and catches exceptions, mapping them to HTTP status codes:
    /// <see cref="KeyNotFoundException"/> to 404,
    /// <see cref="PaginationException"/> to 400,
    /// and all others to 500.
    /// </summary>
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next.Invoke(context);
        }
        catch (KeyNotFoundException keyNotFoundEx)
        {
            logger.LogWarning(keyNotFoundEx, "A KeyNotFoundException occurred.");
            await HandleExceptionAsync(context, keyNotFoundEx, HttpStatusCode.NotFound);
        }
        catch (PaginationException paginationEx)
        {
            logger.LogWarning(paginationEx, "A PaginationException occurred.");
            await HandleExceptionAsync(context, paginationEx, HttpStatusCode.BadRequest);
        }
        catch (Exception e)
        {
            logger.LogError(e, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, e, HttpStatusCode.InternalServerError,
                customMessage: "An internal error occurred.");
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        HttpStatusCode statusCode,
        string? customMessage = null)
    {
        var errorResponse = new ErrorResponse
        {
            Message = customMessage ?? exception.Message,
            Details = env.IsDevelopment() ? exception.StackTrace : null
        };

        var payload = JsonSerializer.Serialize(errorResponse, JsonOptions);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(payload);
    }
}
