using Microsoft.EntityFrameworkCore;
using Vector.Domain.Submission;
using Vector.Domain.Submission.Entities;
using Vector.Domain.Submission.Enums;

namespace Vector.Infrastructure.Persistence.Repositories;

public class DataCorrectionRepository(VectorDbContext context) : IDataCorrectionRepository
{
    public async Task<DataCorrectionRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.DataCorrectionRequests
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<DataCorrectionRequest>> GetBySubmissionIdAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        return await context.DataCorrectionRequests
            .Where(r => r.SubmissionId == submissionId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DataCorrectionRequest>> GetByStatusAsync(
        DataCorrectionStatus status,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await context.DataCorrectionRequests
            .Where(r => r.Status == status)
            .OrderBy(r => r.RequestedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DataCorrectionRequest>> GetPendingBySubmissionIdAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        return await context.DataCorrectionRequests
            .Where(r => r.SubmissionId == submissionId &&
                       (r.Status == DataCorrectionStatus.Pending ||
                        r.Status == DataCorrectionStatus.UnderReview))
            .OrderBy(r => r.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(DataCorrectionRequest Correction, string SubmissionNumber, string InsuredName)>> GetByProducerIdAsync(
        Guid producerId,
        DataCorrectionStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.DataCorrectionRequests
            .Join(
                context.Submissions,
                correction => correction.SubmissionId,
                submission => submission.Id,
                (correction, submission) => new { Correction = correction, Submission = submission })
            .Where(x => x.Submission.ProducerId == producerId);

        if (status.HasValue)
        {
            query = query.Where(x => x.Correction.Status == status.Value);
        }

        var results = await query
            .OrderByDescending(x => x.Correction.RequestedAt)
            .Select(x => new
            {
                x.Correction,
                x.Submission.SubmissionNumber,
                InsuredName = x.Submission.Insured.Name
            })
            .ToListAsync(cancellationToken);

        return results
            .Select(x => (x.Correction, x.SubmissionNumber, x.InsuredName))
            .ToList();
    }

    public async Task AddAsync(DataCorrectionRequest request, CancellationToken cancellationToken = default)
    {
        await context.DataCorrectionRequests.AddAsync(request, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DataCorrectionRequest request, CancellationToken cancellationToken = default)
    {
        context.DataCorrectionRequests.Update(request);
        await context.SaveChangesAsync(cancellationToken);
    }
}
