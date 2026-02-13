using MediatR;
using Vector.Application.Common.Interfaces;
using Vector.Domain.Common;
using Vector.Domain.UnderwritingGuidelines;

namespace Vector.Application.UnderwritingGuidelines.Commands;

/// <summary>
/// Handler for DeactivateGuidelineCommand.
/// </summary>
public sealed class DeactivateGuidelineCommandHandler(
    IUnderwritingGuidelineRepository repository,
    ICurrentUserService currentUserService) : IRequestHandler<DeactivateGuidelineCommand, Result>
{
    public async Task<Result> Handle(
        DeactivateGuidelineCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = currentUserService.TenantId
            ?? throw new InvalidOperationException("Tenant ID is required");

        var guideline = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (guideline is null)
        {
            return Result.Failure(new Error("Guideline.NotFound", $"Guideline with ID '{request.Id}' was not found."));
        }

        if (guideline.TenantId != tenantId)
        {
            return Result.Failure(new Error("Guideline.NotFound", $"Guideline with ID '{request.Id}' was not found."));
        }

        guideline.Deactivate();

        repository.Update(guideline);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
