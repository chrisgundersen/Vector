using MediatR;
using Vector.Application.Common.Interfaces;
using Vector.Domain.Common;
using Vector.Domain.UnderwritingGuidelines;

namespace Vector.Application.UnderwritingGuidelines.Commands;

/// <summary>
/// Handler for RemoveRuleCommand.
/// </summary>
public sealed class RemoveRuleCommandHandler(
    IUnderwritingGuidelineRepository repository,
    ICurrentUserService currentUserService) : IRequestHandler<RemoveRuleCommand, Result>
{
    public async Task<Result> Handle(
        RemoveRuleCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = currentUserService.TenantId
            ?? throw new InvalidOperationException("Tenant ID is required");

        var guideline = await repository.GetByIdWithRulesAsync(request.GuidelineId, cancellationToken);
        if (guideline is null)
        {
            return Result.Failure(new Error("Guideline.NotFound", $"Guideline with ID '{request.GuidelineId}' was not found."));
        }

        if (guideline.TenantId != tenantId)
        {
            return Result.Failure(new Error("Guideline.NotFound", $"Guideline with ID '{request.GuidelineId}' was not found."));
        }

        try
        {
            guideline.RemoveRule(request.RuleId);

            repository.Update(guideline);
            await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(new Error("Guideline.RuleRemoveFailed", ex.Message));
        }
    }
}
