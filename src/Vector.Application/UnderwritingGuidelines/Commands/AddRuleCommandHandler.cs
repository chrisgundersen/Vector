using MediatR;
using Vector.Application.Common.Interfaces;
using Vector.Domain.Common;
using Vector.Domain.UnderwritingGuidelines;

namespace Vector.Application.UnderwritingGuidelines.Commands;

/// <summary>
/// Handler for AddRuleCommand.
/// </summary>
public sealed class AddRuleCommandHandler(
    IUnderwritingGuidelineRepository repository,
    ICurrentUserService currentUserService) : IRequestHandler<AddRuleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        AddRuleCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = currentUserService.TenantId
            ?? throw new InvalidOperationException("Tenant ID is required");

        var guideline = await repository.GetByIdWithRulesAsync(request.GuidelineId, cancellationToken);
        if (guideline is null)
        {
            return Result.Failure<Guid>(new Error("Guideline.NotFound", $"Guideline with ID '{request.GuidelineId}' was not found."));
        }

        if (guideline.TenantId != tenantId)
        {
            return Result.Failure<Guid>(new Error("Guideline.NotFound", $"Guideline with ID '{request.GuidelineId}' was not found."));
        }

        try
        {
            var rule = guideline.AddRule(
                request.Name,
                request.Type,
                request.Action,
                request.Priority);

            if (!string.IsNullOrEmpty(request.Description))
            {
                rule.UpdateDetails(request.Name, request.Description);
            }

            if (request.ScoreAdjustment.HasValue)
            {
                rule.SetScoreAdjustment(request.ScoreAdjustment.Value);
            }

            if (request.PricingModifier.HasValue)
            {
                rule.SetPricingModifier(request.PricingModifier.Value);
            }

            if (!string.IsNullOrEmpty(request.Message))
            {
                rule.SetMessage(request.Message);
            }

            repository.Update(guideline);
            await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(rule.Id);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<Guid>(new Error("Guideline.RuleAddFailed", ex.Message));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Result.Failure<Guid>(new Error("Guideline.InvalidRuleValue", ex.Message));
        }
    }
}
