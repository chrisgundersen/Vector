using MediatR;
using Vector.Application.Common.Interfaces;
using Vector.Domain.Common;
using Vector.Domain.UnderwritingGuidelines;

namespace Vector.Application.UnderwritingGuidelines.Commands;

/// <summary>
/// Handler for ActivateGuidelineCommand.
/// </summary>
public sealed class ActivateGuidelineCommandHandler(
    IUnderwritingGuidelineRepository repository,
    ICurrentUserService currentUserService) : IRequestHandler<ActivateGuidelineCommand, Result>
{
    public async Task<Result> Handle(
        ActivateGuidelineCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = currentUserService.TenantId
            ?? throw new InvalidOperationException("Tenant ID is required");

        var guideline = await repository.GetByIdWithRulesAsync(request.Id, cancellationToken);
        if (guideline is null)
        {
            return Result.Failure(new Error("Guideline.NotFound", $"Guideline with ID '{request.Id}' was not found."));
        }

        if (guideline.TenantId != tenantId)
        {
            return Result.Failure(new Error("Guideline.NotFound", $"Guideline with ID '{request.Id}' was not found."));
        }

        try
        {
            guideline.Activate();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(new Error("Guideline.ActivationFailed", ex.Message));
        }

        repository.Update(guideline);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
