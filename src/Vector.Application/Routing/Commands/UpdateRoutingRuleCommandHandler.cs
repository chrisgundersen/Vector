using MediatR;
using Vector.Domain.Common;
using Vector.Domain.Routing;

namespace Vector.Application.Routing.Commands;

/// <summary>
/// Handler for UpdateRoutingRuleCommand.
/// </summary>
public sealed class UpdateRoutingRuleCommandHandler(
    IRoutingRuleRepository repository) : IRequestHandler<UpdateRoutingRuleCommand, Result>
{
    public async Task<Result> Handle(
        UpdateRoutingRuleCommand request,
        CancellationToken cancellationToken)
    {
        var rule = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (rule is null)
        {
            return Result.Failure(new Error("RoutingRule.NotFound", $"Routing rule with ID '{request.Id}' was not found."));
        }

        try
        {
            rule.UpdateDetails(request.Name, request.Description);
            rule.SetStrategy(request.Strategy);

            if (request.Priority > 0)
            {
                rule.SetPriority(request.Priority);
            }

            if (request.TargetUnderwriterId.HasValue && !string.IsNullOrEmpty(request.TargetUnderwriterName))
            {
                rule.SetTargetUnderwriter(request.TargetUnderwriterId.Value, request.TargetUnderwriterName);
            }
            else if (request.TargetTeamId.HasValue && !string.IsNullOrEmpty(request.TargetTeamName))
            {
                rule.SetTargetTeam(request.TargetTeamId.Value, request.TargetTeamName);
            }
            else if (request.TargetUnderwriterId is null && request.TargetTeamId is null)
            {
                rule.ClearTarget();
            }

            await repository.UpdateAsync(rule, cancellationToken);

            return Result.Success();
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Result.Failure(new Error("RoutingRule.InvalidValue", ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(new Error("RoutingRule.InvalidValue", ex.Message));
        }
    }
}
