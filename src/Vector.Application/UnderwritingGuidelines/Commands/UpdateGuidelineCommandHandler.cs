using MediatR;
using Vector.Application.Common.Interfaces;
using Vector.Domain.Common;
using Vector.Domain.UnderwritingGuidelines;

namespace Vector.Application.UnderwritingGuidelines.Commands;

/// <summary>
/// Handler for UpdateGuidelineCommand.
/// </summary>
public sealed class UpdateGuidelineCommandHandler(
    IUnderwritingGuidelineRepository repository,
    ICurrentUserService currentUserService) : IRequestHandler<UpdateGuidelineCommand, Result>
{
    public async Task<Result> Handle(
        UpdateGuidelineCommand request,
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

        // Check for duplicate name (excluding current guideline)
        var exists = await repository.ExistsByNameAsync(tenantId, request.Name, request.Id, cancellationToken);
        if (exists)
        {
            return Result.Failure(new Error("Guideline.DuplicateName", $"A guideline with name '{request.Name}' already exists."));
        }

        guideline.UpdateDetails(request.Name, request.Description);
        guideline.SetApplicability(
            request.ApplicableCoverageTypes,
            request.ApplicableStates,
            request.ApplicableNAICSCodes);
        guideline.SetEffectiveDates(request.EffectiveDate, request.ExpirationDate);

        repository.Update(guideline);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
