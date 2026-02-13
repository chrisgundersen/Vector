using MediatR;
using Vector.Application.Common.Interfaces;
using Vector.Domain.Common;
using Vector.Domain.UnderwritingGuidelines;

namespace Vector.Application.UnderwritingGuidelines.Commands;

/// <summary>
/// Handler for DeleteGuidelineCommand.
/// Archives the guideline rather than hard-deleting it.
/// </summary>
public sealed class DeleteGuidelineCommandHandler(
    IUnderwritingGuidelineRepository repository,
    ICurrentUserService currentUserService) : IRequestHandler<DeleteGuidelineCommand, Result>
{
    public async Task<Result> Handle(
        DeleteGuidelineCommand request,
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

        guideline.Archive();

        repository.Update(guideline);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
