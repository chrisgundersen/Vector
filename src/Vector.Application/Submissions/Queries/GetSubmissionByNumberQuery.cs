using MediatR;
using Vector.Application.Submissions.DTOs;

namespace Vector.Application.Submissions.Queries;

/// <summary>
/// Query to retrieve a submission by its submission number.
/// </summary>
public sealed record GetSubmissionByNumberQuery(string SubmissionNumber) : IRequest<SubmissionDto?>;
