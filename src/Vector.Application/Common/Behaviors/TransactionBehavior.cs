using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Domain.Common;

namespace Vector.Application.Common.Behaviors;

/// <summary>
/// Marker interface for commands that require transactional behavior.
/// </summary>
public interface ITransactionalCommand
{
}

/// <summary>
/// MediatR pipeline behavior for transactional commands.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ILogger<TransactionBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITransactionalCommand
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogDebug("Beginning transaction for {RequestName}", requestName);

        try
        {
            var response = await next();

            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogDebug("Committed transaction for {RequestName}", requestName);

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Transaction failed for {RequestName}", requestName);
            throw;
        }
    }
}
