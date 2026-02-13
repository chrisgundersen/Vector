using System.Collections.Concurrent;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vector.Application.Common.Interfaces;

namespace Vector.Infrastructure.Messaging;

/// <summary>
/// Azure Service Bus message bus implementation for production use.
/// </summary>
public sealed class AzureServiceBusService : IMessageBusService, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();
    private readonly ILogger<AzureServiceBusService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public AzureServiceBusService(
        ServiceBusClient client,
        ILogger<AzureServiceBusService> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logger);

        _client = client;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task PublishAsync<T>(
        string topicOrQueue,
        T message,
        string? sessionId = null,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicOrQueue);
        ArgumentNullException.ThrowIfNull(message);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var sender = GetOrCreateSender(topicOrQueue);

        var serviceBusMessage = CreateMessage(message, sessionId);

        try
        {
            _logger.LogDebug(
                "Publishing message to {Queue} (session: {SessionId}, messageId: {MessageId})",
                topicOrQueue,
                sessionId ?? "none",
                serviceBusMessage.MessageId);

            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);

            _logger.LogInformation(
                "Published message {MessageId} to {Queue}",
                serviceBusMessage.MessageId,
                topicOrQueue);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error publishing message to {Queue}",
                topicOrQueue);
            throw;
        }
    }

    public async Task ScheduleAsync<T>(
        string topicOrQueue,
        T message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicOrQueue);
        ArgumentNullException.ThrowIfNull(message);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var sender = GetOrCreateSender(topicOrQueue);

        var serviceBusMessage = CreateMessage(message, sessionId: null);
        serviceBusMessage.ScheduledEnqueueTime = scheduledTime;

        try
        {
            _logger.LogDebug(
                "Scheduling message to {Queue} for {ScheduledTime} (messageId: {MessageId})",
                topicOrQueue,
                scheduledTime,
                serviceBusMessage.MessageId);

            var sequenceNumber = await sender.ScheduleMessageAsync(
                serviceBusMessage,
                scheduledTime,
                cancellationToken);

            _logger.LogInformation(
                "Scheduled message {MessageId} to {Queue} for {ScheduledTime} (sequence: {SequenceNumber})",
                serviceBusMessage.MessageId,
                topicOrQueue,
                scheduledTime,
                sequenceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error scheduling message to {Queue} for {ScheduledTime}",
                topicOrQueue,
                scheduledTime);
            throw;
        }
    }

    private ServiceBusSender GetOrCreateSender(string topicOrQueue)
    {
        return _senders.GetOrAdd(topicOrQueue, queueName =>
        {
            _logger.LogDebug("Creating sender for {Queue}", queueName);
            return _client.CreateSender(queueName);
        });
    }

    private ServiceBusMessage CreateMessage<T>(T message, string? sessionId) where T : class
    {
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var serviceBusMessage = new ServiceBusMessage(json)
        {
            MessageId = Guid.NewGuid().ToString(),
            ContentType = "application/json",
            Subject = typeof(T).Name
        };

        if (!string.IsNullOrEmpty(sessionId))
        {
            serviceBusMessage.SessionId = sessionId;
        }

        serviceBusMessage.ApplicationProperties["MessageType"] = typeof(T).FullName;
        serviceBusMessage.ApplicationProperties["Timestamp"] = DateTimeOffset.UtcNow.ToString("O");

        return serviceBusMessage;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        var disposeTasks = _senders.Values
            .Select(sender => sender.DisposeAsync().AsTask())
            .ToList();

        await Task.WhenAll(disposeTasks);
        _senders.Clear();

        _logger.LogDebug("Disposed Azure Service Bus service");
    }
}

/// <summary>
/// Configuration options for Azure Service Bus.
/// </summary>
public class AzureServiceBusOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "MessageBus:AzureServiceBus";

    /// <summary>
    /// Azure Service Bus connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Name of the queue for email ingestion messages.
    /// </summary>
    public string EmailIngestionQueue { get; set; } = "email-ingestion";

    /// <summary>
    /// Name of the queue for document processing messages.
    /// </summary>
    public string DocumentProcessingQueue { get; set; } = "document-processing";
}
