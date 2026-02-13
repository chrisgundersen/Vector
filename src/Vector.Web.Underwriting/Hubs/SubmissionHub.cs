using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Vector.Web.Underwriting.Hubs;

/// <summary>
/// SignalR hub for real-time submission updates.
/// </summary>
[Authorize]
public class SubmissionHub : Hub
{
    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        // Add user to their tenant group for targeted notifications
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value
                    ?? Context.User?.FindFirst("tid")?.Value;

        if (!string.IsNullOrEmpty(tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{tenantId}");
        }

        // Add underwriters to underwriter group
        if (Context.User?.IsInRole("Underwriter") == true || Context.User?.IsInRole("Admin") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "underwriters");
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a specific submission's update channel.
    /// </summary>
    public async Task JoinSubmissionGroup(Guid submissionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"submission-{submissionId}");
    }

    /// <summary>
    /// Leave a specific submission's update channel.
    /// </summary>
    public async Task LeaveSubmissionGroup(Guid submissionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"submission-{submissionId}");
    }
}

/// <summary>
/// Extension methods for sending notifications via the SubmissionHub.
/// </summary>
public static class SubmissionHubExtensions
{
    /// <summary>
    /// Notify all underwriters that a new submission is available.
    /// </summary>
    public static async Task NotifyNewSubmission(
        this IHubContext<SubmissionHub> hubContext,
        Guid submissionId,
        string submissionNumber,
        string insuredName)
    {
        await hubContext.Clients.Group("underwriters").SendAsync(
            "NewSubmission",
            new { submissionId, submissionNumber, insuredName, timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Notify clients that a submission has been updated.
    /// </summary>
    public static async Task NotifySubmissionUpdated(
        this IHubContext<SubmissionHub> hubContext,
        Guid submissionId,
        string updateType)
    {
        await hubContext.Clients.Group($"submission-{submissionId}").SendAsync(
            "SubmissionUpdated",
            new { submissionId, updateType, timestamp = DateTime.UtcNow });

        await hubContext.Clients.Group("underwriters").SendAsync(
            "QueueUpdated",
            new { submissionId, updateType, timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Notify that a submission has been assigned to an underwriter.
    /// </summary>
    public static async Task NotifySubmissionAssigned(
        this IHubContext<SubmissionHub> hubContext,
        Guid submissionId,
        Guid underwriterId,
        string underwriterName)
    {
        await hubContext.Clients.Group($"submission-{submissionId}").SendAsync(
            "SubmissionAssigned",
            new { submissionId, underwriterId, underwriterName, timestamp = DateTime.UtcNow });

        await hubContext.Clients.Group("underwriters").SendAsync(
            "QueueUpdated",
            new { submissionId, updateType = "Assigned", timestamp = DateTime.UtcNow });
    }
}
