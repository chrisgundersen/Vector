using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vector.Application.Common.Interfaces;
using Vector.Domain.Common;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Events;
using Vector.Infrastructure.Persistence;

namespace Vector.Infrastructure.IntegrationTests.Persistence;

public class VectorDbContextDomainEventTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock;
    private readonly Mock<ILogger<VectorDbContext>> _loggerMock;

    public VectorDbContextDomainEventTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _dispatcherMock = new Mock<IDomainEventDispatcher>();
        _loggerMock = new Mock<ILogger<VectorDbContext>>();
    }

    private VectorDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VectorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new VectorDbContext(options, _currentUserServiceMock.Object, _loggerMock.Object, _dispatcherMock.Object);
    }

    [Fact]
    public async Task SaveChangesAsync_WithDomainEvents_DispatchesAfterSave()
    {
        using var context = CreateContext();
        var submissionResult = Submission.Create(Guid.NewGuid(), "SUB-001", "Test Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();

        context.Submissions.Add(submission);

        await context.SaveChangesAsync();

        _dispatcherMock.Verify(
            d => d.DispatchEventsAsync(
                It.Is<IEnumerable<IDomainEvent>>(events => events.Any()),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveChangesAsync_ClearsEventsFromAggregate()
    {
        using var context = CreateContext();
        var submissionResult = Submission.Create(Guid.NewGuid(), "SUB-001", "Test Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();

        context.Submissions.Add(submission);

        await context.SaveChangesAsync();

        submission.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenDispatcherThrows_DoesNotPropagateException()
    {
        using var context = CreateContext();
        var submissionResult = Submission.Create(Guid.NewGuid(), "SUB-001", "Test Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();

        context.Submissions.Add(submission);

        _dispatcherMock.Setup(d => d.DispatchEventsAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SignalR failure"));

        var act = () => context.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenDispatcherThrows_DataIsStillPersisted()
    {
        using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var submissionResult = Submission.Create(tenantId, "SUB-001", "Test Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();

        context.Submissions.Add(submission);

        _dispatcherMock.Setup(d => d.DispatchEventsAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SignalR failure"));

        await context.SaveChangesAsync();

        var saved = await context.Submissions.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.SubmissionNumber == "SUB-001");
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoEvents_DoesNotCallDispatcher()
    {
        using var context = CreateContext();
        var submissionResult = Submission.Create(Guid.NewGuid(), "SUB-001", "Test Corp");
        var submission = submissionResult.Value;
        // Not calling MarkAsReceived, so no events

        context.Submissions.Add(submission);

        await context.SaveChangesAsync();

        _dispatcherMock.Verify(
            d => d.DispatchEventsAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SaveChangesAsync_WithNullDispatcher_DoesNotThrow()
    {
        var options = new DbContextOptionsBuilder<VectorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new VectorDbContext(options, _currentUserServiceMock.Object);
        var submissionResult = Submission.Create(Guid.NewGuid(), "SUB-001", "Test Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();

        context.Submissions.Add(submission);

        var act = () => context.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_DispatchesCorrectEventTypes()
    {
        using var context = CreateContext();
        var submissionResult = Submission.Create(Guid.NewGuid(), "SUB-001", "Test Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();

        context.Submissions.Add(submission);
        IEnumerable<IDomainEvent>? capturedEvents = null;

        _dispatcherMock.Setup(d => d.DispatchEventsAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => capturedEvents = events.ToList());

        await context.SaveChangesAsync();

        capturedEvents.Should().NotBeNull();
        capturedEvents.Should().HaveCount(2);
        capturedEvents!.OfType<SubmissionCreatedEvent>().Should().ContainSingle();
        capturedEvents!.OfType<SubmissionStatusChangedEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task SaveChangesAsync_UsesCancellationTokenNone_ForDispatch()
    {
        using var context = CreateContext();
        var submissionResult = Submission.Create(Guid.NewGuid(), "SUB-001", "Test Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();

        context.Submissions.Add(submission);
        CancellationToken capturedToken = default;

        _dispatcherMock.Setup(d => d.DispatchEventsAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((_, token) => capturedToken = token);

        using var cts = new CancellationTokenSource();
        await context.SaveChangesAsync(cts.Token);

        capturedToken.Should().Be(CancellationToken.None);
    }
}
