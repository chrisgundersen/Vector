using MediatR;
using Vector.Domain.Common;

namespace Vector.Application.UnderwritingGuidelines.Commands;

/// <summary>
/// Command to activate an underwriting guideline.
/// </summary>
public sealed record ActivateGuidelineCommand(Guid Id) : IRequest<Result>;
