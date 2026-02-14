using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Vector.Api.Middleware;

/// <summary>
/// Middleware that handles unhandled exceptions and returns consistent error responses.
/// </summary>
public class GlobalExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlerMiddleware> logger,
    IHostEnvironment environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Request.Headers[CorrelationIdMiddleware.CorrelationIdHeader].FirstOrDefault()
            ?? "unknown";

        logger.LogError(exception,
            "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
            correlationId,
            context.Request.Path,
            context.Request.Method);

        context.Response.ContentType = "application/problem+json";

        var (statusCode, title) = GetStatusCodeAndTitle(exception);
        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Instance = context.Request.Path,
            Extensions =
            {
                ["correlationId"] = correlationId,
                ["traceId"] = context.TraceIdentifier
            }
        };

        // Only include exception details in development
        if (environment.IsDevelopment())
        {
            problemDetails.Detail = exception.Message;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
        }
        else
        {
            problemDetails.Detail = "An unexpected error occurred. Please try again later.";
        }

        var json = JsonSerializer.Serialize(problemDetails, JsonOptions);
        await context.Response.WriteAsync(json);
    }

    private static (int StatusCode, string Title) GetStatusCodeAndTitle(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => (StatusCodes.Status400BadRequest, "Bad Request"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Conflict"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            TimeoutException => (StatusCodes.Status504GatewayTimeout, "Gateway Timeout"),
            OperationCanceledException => (StatusCodes.Status499ClientClosedRequest, "Client Closed Request"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };
    }
}

/// <summary>
/// Extension methods for adding the global exception handler middleware.
/// </summary>
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
