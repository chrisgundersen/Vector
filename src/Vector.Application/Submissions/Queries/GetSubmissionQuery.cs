using MediatR;
using Vector.Application.Submissions.DTOs;

namespace Vector.Application.Submissions.Queries;

/// <summary>
/// Query to get a submission by ID.
/// </summary>
public sealed record GetSubmissionQuery(Guid SubmissionId) : IRequest<SubmissionDto?>;
