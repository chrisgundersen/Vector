using MediatR;
using Vector.Domain.Common;
using Vector.Domain.Routing;
using Vector.Domain.Routing.Aggregates;

namespace Vector.Application.Routing.Commands;

/// <summary>
/// Handler for CreateRoutingRuleCommand.
/// </summary>
public sealed class CreateRoutingRuleCommandHandler(
    IRoutingRuleRepository repository) : IRequestHandler<CreateRoutingRuleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateRoutingRuleCommand request,
        CancellationToken cancellationToken)
    {
        var createResult = RoutingRule.Create(
            request.Name,
            request.Description,
            request.Strategy);

        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        var rule = createResult.Value;

        if (request.Priority > 0)
        {
            try
            {
                rule.SetPriority(request.Priority);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Result.Failure<Guid>(new Error("RoutingRule.InvalidPriority", ex.Message));
            }
        }

        if (request.TargetUnderwriterId.HasValue && !string.IsNullOrEmpty(request.TargetUnderwriterName))
        {
            rule.SetTargetUnderwriter(request.TargetUnderwriterId.Value, request.TargetUnderwriterName);
        }
        else if (request.TargetTeamId.HasValue && !string.IsNullOrEmpty(request.TargetTeamName))
        {
            rule.SetTargetTeam(request.TargetTeamId.Value, request.TargetTeamName);
        }

        await repository.AddAsync(rule, cancellationToken);

        return Result.Success(rule.Id);
    }
}
