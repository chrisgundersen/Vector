using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Vector.Application.Common.Interfaces;

namespace Vector.Infrastructure.Messaging;

/// <summary>
/// In-memory message bus for development/testing.
/// </summary>
public class InMemoryMessageBusService(
    ILogger<InMemoryMessageBusService> logger) : IMessageBusService
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<object>> _queues = new();

    public Task PublishAsync<T>(
        string topicOrQueue,
        T message,
        string? sessionId = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var queue = _queues.GetOrAdd(topicOrQueue, _ => new ConcurrentQueue<object>());
        queue.Enqueue(message);

        logger.LogInformation(
            "Published message to {Queue} (session: {SessionId})",
            topicOrQueue, sessionId ?? "none");

        return Task.CompletedTask;
    }

    public Task ScheduleAsync<T>(
        string topicOrQueue,
        T message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken = default) where T : class
    {
        logger.LogInformation(
            "Scheduled message to {Queue} for {ScheduledTime}",
            topicOrQueue, scheduledTime);

        // In-memory implementation just publishes immediately
        return PublishAsync(topicOrQueue, message, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Dequeues a message (for testing purposes).
    /// </summary>
    public T? TryDequeue<T>(string topicOrQueue) where T : class
    {
        if (_queues.TryGetValue(topicOrQueue, out var queue) && queue.TryDequeue(out var message))
        {
            return message as T;
        }

        return null;
    }

    /// <summary>
    /// Gets the count of messages in a queue (for testing purposes).
    /// </summary>
    public int GetQueueCount(string topicOrQueue)
    {
        return _queues.TryGetValue(topicOrQueue, out var queue) ? queue.Count : 0;
    }
}
