using MediatR;
using Microsoft.AspNetCore.SignalR;
using Vector.Application.Common;
using Vector.Domain.Submission.Events;
using Vector.Web.Underwriting.Hubs;

namespace Vector.Web.Underwriting.EventHandlers;

/// <summary>
/// Handles <see cref="SubmissionCreatedEvent"/> by notifying underwriters via SignalR.
/// </summary>
public sealed class SubmissionCreatedNotificationHandler(
    IHubContext<SubmissionHub> hubContext) : INotificationHandler<DomainEventNotification<SubmissionCreatedEvent>>
{
    public async Task Handle(DomainEventNotification<SubmissionCreatedEvent> notification, CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent;

        await hubContext.NotifyNewSubmission(
            evt.SubmissionId,
            evt.SubmissionNumber,
            evt.InsuredName);
    }
}

/// <summary>
/// Handles <see cref="SubmissionStatusChangedEvent"/> by notifying clients via SignalR.
/// </summary>
public sealed class SubmissionStatusChangedNotificationHandler(
    IHubContext<SubmissionHub> hubContext) : INotificationHandler<DomainEventNotification<SubmissionStatusChangedEvent>>
{
    public async Task Handle(DomainEventNotification<SubmissionStatusChangedEvent> notification, CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent;

        await hubContext.NotifySubmissionUpdated(
            evt.SubmissionId,
            evt.NewStatus.ToString());
    }
}

/// <summary>
/// Handles <see cref="SubmissionAssignedEvent"/> by notifying clients via SignalR.
/// </summary>
public sealed class SubmissionAssignedNotificationHandler(
    IHubContext<SubmissionHub> hubContext) : INotificationHandler<DomainEventNotification<SubmissionAssignedEvent>>
{
    public async Task Handle(DomainEventNotification<SubmissionAssignedEvent> notification, CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent;

        await hubContext.NotifySubmissionAssigned(
            evt.SubmissionId,
            evt.UnderwriterId,
            evt.UnderwriterName);
    }
}

/// <summary>
/// Handles <see cref="ClearanceCompletedEvent"/> by notifying clients via SignalR.
/// </summary>
public sealed class ClearanceCompletedNotificationHandler(
    IHubContext<SubmissionHub> hubContext) : INotificationHandler<DomainEventNotification<ClearanceCompletedEvent>>
{
    public async Task Handle(DomainEventNotification<ClearanceCompletedEvent> notification, CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent;

        await hubContext.Clients.All.SendAsync(
            "ClearanceCompleted",
            new { evt.SubmissionId, Status = evt.Status.ToString(), evt.MatchCount },
            cancellationToken);
    }
}

/// <summary>
/// Handles <see cref="ClearanceOverriddenEvent"/> by notifying clients via SignalR.
/// </summary>
public sealed class ClearanceOverriddenNotificationHandler(
    IHubContext<SubmissionHub> hubContext) : INotificationHandler<DomainEventNotification<ClearanceOverriddenEvent>>
{
    public async Task Handle(DomainEventNotification<ClearanceOverriddenEvent> notification, CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent;

        await hubContext.NotifySubmissionUpdated(
            evt.SubmissionId,
            "Received");
    }
}
