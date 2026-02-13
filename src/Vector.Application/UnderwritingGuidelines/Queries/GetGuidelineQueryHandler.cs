using MediatR;
using Vector.Application.UnderwritingGuidelines.DTOs;
using Vector.Domain.UnderwritingGuidelines;

namespace Vector.Application.UnderwritingGuidelines.Queries;

/// <summary>
/// Handler for GetGuidelineQuery.
/// </summary>
public sealed class GetGuidelineQueryHandler(
    IUnderwritingGuidelineRepository repository) : IRequestHandler<GetGuidelineQuery, GuidelineDto?>
{
    public async Task<GuidelineDto?> Handle(
        GetGuidelineQuery request,
        CancellationToken cancellationToken)
    {
        var guideline = await repository.GetByIdWithRulesAsync(request.Id, cancellationToken);
        return guideline?.ToDto();
    }
}
