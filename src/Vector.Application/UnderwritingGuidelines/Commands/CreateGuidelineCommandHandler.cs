using MediatR;
using Vector.Application.Common.Interfaces;
using Vector.Domain.Common;
using Vector.Domain.UnderwritingGuidelines;
using Vector.Domain.UnderwritingGuidelines.Aggregates;

namespace Vector.Application.UnderwritingGuidelines.Commands;

/// <summary>
/// Handler for CreateGuidelineCommand.
/// </summary>
public sealed class CreateGuidelineCommandHandler(
    IUnderwritingGuidelineRepository repository,
    ICurrentUserService currentUserService) : IRequestHandler<CreateGuidelineCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateGuidelineCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = currentUserService.TenantId
            ?? throw new InvalidOperationException("Tenant ID is required");

        // Check for duplicate name
        var exists = await repository.ExistsByNameAsync(tenantId, request.Name, null, cancellationToken);
        if (exists)
        {
            return Result.Failure<Guid>(new Error("Guideline.DuplicateName", $"A guideline with name '{request.Name}' already exists."));
        }

        var guideline = UnderwritingGuideline.Create(tenantId, request.Name);

        if (!string.IsNullOrEmpty(request.Description))
        {
            guideline.UpdateDetails(request.Name, request.Description);
        }

        guideline.SetApplicability(
            request.ApplicableCoverageTypes,
            request.ApplicableStates,
            request.ApplicableNAICSCodes);

        if (request.EffectiveDate.HasValue || request.ExpirationDate.HasValue)
        {
            guideline.SetEffectiveDates(request.EffectiveDate, request.ExpirationDate);
        }

        await repository.AddAsync(guideline, cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(guideline.Id);
    }
}
