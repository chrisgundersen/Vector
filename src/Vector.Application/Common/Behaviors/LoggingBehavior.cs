using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Application.Common.Interfaces;

namespace Vector.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior for logging request execution.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUserService currentUserService) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = currentUserService.UserId ?? "Anonymous";
        var tenantId = currentUserService.TenantId?.ToString() ?? "N/A";

        logger.LogInformation(
            "Handling {RequestName} for User: {UserId}, Tenant: {TenantId}",
            requestName, userId, tenantId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();

            logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds}ms",
                requestName, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            logger.LogError(ex,
                "Error handling {RequestName} after {ElapsedMilliseconds}ms: {ErrorMessage}",
                requestName, stopwatch.ElapsedMilliseconds, ex.Message);

            throw;
        }
    }
}
