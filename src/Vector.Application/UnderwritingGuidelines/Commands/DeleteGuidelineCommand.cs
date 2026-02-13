using MediatR;
using Vector.Domain.Common;

namespace Vector.Application.UnderwritingGuidelines.Commands;

/// <summary>
/// Command to delete (archive) an underwriting guideline.
/// </summary>
public sealed record DeleteGuidelineCommand(Guid Id) : IRequest<Result>;
