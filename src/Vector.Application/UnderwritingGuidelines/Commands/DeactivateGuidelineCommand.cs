using MediatR;
using Vector.Domain.Common;

namespace Vector.Application.UnderwritingGuidelines.Commands;

/// <summary>
/// Command to deactivate an underwriting guideline.
/// </summary>
public sealed record DeactivateGuidelineCommand(Guid Id) : IRequest<Result>;
