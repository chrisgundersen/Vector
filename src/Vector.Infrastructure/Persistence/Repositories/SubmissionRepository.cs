using Microsoft.EntityFrameworkCore;
using Vector.Domain.Common;
using Vector.Domain.Submission;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Enums;

namespace Vector.Infrastructure.Persistence.Repositories;

public class SubmissionRepository(VectorDbContext context) : ISubmissionRepository
{
    public IUnitOfWork UnitOfWork => context;

    public async Task<Submission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Submissions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task AddAsync(Submission aggregate, CancellationToken cancellationToken = default)
    {
        await context.Submissions.AddAsync(aggregate, cancellationToken);
    }

    public void Update(Submission aggregate)
    {
        context.Submissions.Update(aggregate);
    }

    public void Remove(Submission aggregate)
    {
        context.Submissions.Remove(aggregate);
    }

    public async Task<Submission?> GetBySubmissionNumberAsync(
        Guid tenantId,
        string submissionNumber,
        CancellationToken cancellationToken = default)
    {
        return await context.Submissions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s =>
                s.TenantId == tenantId &&
                s.SubmissionNumber == submissionNumber,
                cancellationToken);
    }

    public async Task<Submission?> GetByProcessingJobIdAsync(
        Guid processingJobId,
        CancellationToken cancellationToken = default)
    {
        return await context.Submissions
            .FirstOrDefaultAsync(s => s.ProcessingJobId == processingJobId, cancellationToken);
    }

    public async Task<IReadOnlyList<Submission>> GetByStatusAsync(
        Guid tenantId,
        SubmissionStatus status,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await context.Submissions
            .IgnoreQueryFilters()
            .Where(s =>
                s.TenantId == tenantId &&
                s.Status == status)
            .OrderByDescending(s => s.ReceivedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Submission>> GetByUnderwriterAsync(
        Guid underwriterId,
        SubmissionStatus? status,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = context.Submissions
            .Where(s => s.AssignedUnderwriterId == underwriterId);

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        return await query
            .OrderByDescending(s => s.ReceivedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Submission>> GetByProducerAsync(
        Guid producerId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await context.Submissions
            .Where(s => s.ProducerId == producerId)
            .OrderByDescending(s => s.ReceivedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<string> GenerateSubmissionNumberAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"SUB-{year}-";

        var lastSubmission = await context.Submissions
            .IgnoreQueryFilters()
            .Where(s =>
                s.TenantId == tenantId &&
                s.SubmissionNumber.StartsWith(prefix))
            .OrderByDescending(s => s.SubmissionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var nextNumber = 1;

        if (lastSubmission is not null)
        {
            var lastNumberPart = lastSubmission.SubmissionNumber[(prefix.Length)..];
            if (int.TryParse(lastNumberPart, out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D6}";
    }

    public async Task<(IReadOnlyList<Submission> Submissions, int TotalCount)> SearchAsync(
        Guid tenantId,
        Guid? producerId,
        SubmissionStatus? status,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Submissions
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId);

        if (producerId.HasValue)
        {
            query = query.Where(s => s.ProducerId == producerId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(s =>
                s.SubmissionNumber.ToLower().Contains(term) ||
                s.Insured.Name.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var submissions = await query
            .OrderByDescending(s => s.ReceivedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (submissions, totalCount);
    }
}
