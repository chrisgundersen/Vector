using Vector.Domain.EmailIntake.Aggregates;

namespace Vector.Application.EmailIntake.DTOs;

public sealed record InboundEmailDto(
    Guid Id,
    Guid TenantId,
    string ExternalMessageId,
    string MailboxId,
    string FromAddress,
    string Subject,
    string BodyPreview,
    DateTime ReceivedAt,
    string Status,
    int AttachmentCount,
    IReadOnlyList<EmailAttachmentDto> Attachments);

public sealed record EmailAttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeInBytes,
    string BlobStorageUrl,
    string Status);

public static class InboundEmailMappingExtensions
{
    public static InboundEmailDto ToDto(this InboundEmail email)
    {
        return new InboundEmailDto(
            email.Id,
            email.TenantId,
            email.ExternalMessageId,
            email.MailboxId,
            email.FromAddress.Value,
            email.Subject,
            email.BodyPreview,
            email.ReceivedAt,
            email.Status.ToString(),
            email.Attachments.Count,
            email.Attachments.Select(a => a.ToDto()).ToList());
    }

    public static EmailAttachmentDto ToDto(this EmailAttachment attachment)
    {
        return new EmailAttachmentDto(
            attachment.Id,
            attachment.Metadata.FileName,
            attachment.Metadata.ContentType,
            attachment.Metadata.SizeInBytes,
            attachment.BlobStorageUrl,
            attachment.Status.ToString());
    }
}
