using System.Net;
using System.Text.Json;
using MyProject.Infrastructure.Persistence.Exceptions;
using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Middlewares;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment env)
{
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
            await HandleExceptionAsync(context, e, HttpStatusCode.InternalServerError);
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

        var payload = JsonSerializer.Serialize(errorResponse);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(payload);
    }
}
