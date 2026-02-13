using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Application.Common.Interfaces;
using Vector.Domain.Common;
using Vector.Domain.EmailIntake;

namespace Vector.Application.EmailIntake.Commands;

/// <summary>
/// Handler for ExtractAttachmentCommand.
/// </summary>
public sealed class ExtractAttachmentCommandHandler(
    IInboundEmailRepository emailRepository,
    IBlobStorageService blobStorageService,
    ILogger<ExtractAttachmentCommandHandler> logger) : IRequestHandler<ExtractAttachmentCommand, Result<Guid>>
{
    private const string AttachmentsContainer = "email-attachments";

    public async Task<Result<Guid>> Handle(
        ExtractAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        var email = await emailRepository.GetByIdAsync(request.InboundEmailId, cancellationToken);

        if (email is null)
        {
            return Result.Failure<Guid>(new Error(
                "InboundEmail.NotFound",
                $"Email with ID {request.InboundEmailId} was not found."));
        }

        // Generate blob name
        var blobName = $"{email.TenantId}/{email.Id}/{Guid.NewGuid()}/{request.FileName}";

        // Upload to blob storage
        using var contentStream = new MemoryStream(request.Content);
        var blobUrl = await blobStorageService.UploadAsync(
            AttachmentsContainer,
            blobName,
            contentStream,
            request.ContentType,
            cancellationToken);

        // Add attachment to email aggregate
        var attachmentResult = email.AddAttachment(
            request.FileName,
            request.ContentType,
            request.Content.Length,
            request.Content,
            blobUrl);

        if (attachmentResult.IsFailure)
        {
            // Clean up blob if attachment creation failed
            await blobStorageService.DeleteAsync(AttachmentsContainer, blobName, cancellationToken);
            return Result.Failure<Guid>(attachmentResult.Error);
        }

        emailRepository.Update(email);

        logger.LogInformation(
            "Extracted attachment {FileName} ({Size} bytes) for email {EmailId}",
            request.FileName, request.Content.Length, request.InboundEmailId);

        return Result.Success(attachmentResult.Value.Id);
    }
}
