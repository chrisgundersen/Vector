using MediatR;
using Vector.Application.DocumentProcessing.DTOs;

namespace Vector.Application.DocumentProcessing.Queries;

/// <summary>
/// Query to get a processing job by ID.
/// </summary>
public sealed record GetProcessingJobQuery(Guid JobId) : IRequest<ProcessingJobDto?>;
