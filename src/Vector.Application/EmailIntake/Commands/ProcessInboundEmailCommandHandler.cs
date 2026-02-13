using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Application.Common.Interfaces;
using Vector.Domain.Common;
using Vector.Domain.EmailIntake;
using Vector.Domain.EmailIntake.Aggregates;
using Vector.Domain.EmailIntake.ValueObjects;

namespace Vector.Application.EmailIntake.Commands;

/// <summary>
/// Handler for ProcessInboundEmailCommand.
/// </summary>
public sealed class ProcessInboundEmailCommandHandler(
    IInboundEmailRepository emailRepository,
    ICacheService cacheService,
    ILogger<ProcessInboundEmailCommandHandler> logger) : IRequestHandler<ProcessInboundEmailCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        ProcessInboundEmailCommand request,
        CancellationToken cancellationToken)
    {
        var contentHash = ContentHash.ComputeSha256(request.BodyContent);

        // Check for duplicate using cache first
        var cacheKey = $"email:hash:{request.TenantId}:{contentHash.Value}";
        var isDuplicate = await cacheService.ExistsAsync(cacheKey, cancellationToken);

        if (isDuplicate)
        {
            logger.LogInformation(
                "Duplicate email detected for tenant {TenantId} with hash {Hash}",
                request.TenantId, contentHash.Value);

            return Result.Failure<Guid>(new Error(
                "InboundEmail.Duplicate",
                "This email has already been processed."));
        }

        // Also check the database
        var existsInDb = await emailRepository.ExistsByContentHashAsync(
            request.TenantId,
            contentHash,
            cancellationToken);

        if (existsInDb)
        {
            // Cache it for future checks
            await cacheService.SetAsync(cacheKey, true, TimeSpan.FromHours(24), cancellationToken);

            return Result.Failure<Guid>(new Error(
                "InboundEmail.Duplicate",
                "This email has already been processed."));
        }

        // Create the inbound email
        var emailResult = InboundEmail.Create(
            request.TenantId,
            request.ExternalMessageId,
            request.MailboxId,
            request.FromAddress,
            request.Subject,
            request.BodyPreview,
            request.BodyContent,
            request.ReceivedAt);

        if (emailResult.IsFailure)
        {
            return Result.Failure<Guid>(emailResult.Error);
        }

        var email = emailResult.Value;

        await emailRepository.AddAsync(email, cancellationToken);

        // Cache the hash to prevent duplicates
        await cacheService.SetAsync(cacheKey, true, TimeSpan.FromHours(24), cancellationToken);

        logger.LogInformation(
            "Created inbound email {EmailId} for tenant {TenantId} from {FromAddress}",
            email.Id, request.TenantId, request.FromAddress);

        return Result.Success(email.Id);
    }
}
