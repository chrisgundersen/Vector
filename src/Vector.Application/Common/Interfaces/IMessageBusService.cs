namespace Vector.Application.Common.Interfaces;

/// <summary>
/// Interface for message bus operations.
/// </summary>
public interface IMessageBusService
{
    /// <summary>
    /// Publishes a message to a topic/queue.
    /// </summary>
    Task PublishAsync<T>(
        string topicOrQueue,
        T message,
        string? sessionId = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Schedules a message for future delivery.
    /// </summary>
    Task ScheduleAsync<T>(
        string topicOrQueue,
        T message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken = default) where T : class;
}
