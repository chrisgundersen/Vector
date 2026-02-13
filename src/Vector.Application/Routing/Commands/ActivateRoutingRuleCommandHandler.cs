using MediatR;
using Vector.Domain.Common;
using Vector.Domain.Routing;

namespace Vector.Application.Routing.Commands;

/// <summary>
/// Handler for ActivateRoutingRuleCommand.
/// </summary>
public sealed class ActivateRoutingRuleCommandHandler(
    IRoutingRuleRepository repository) : IRequestHandler<ActivateRoutingRuleCommand, Result>
{
    public async Task<Result> Handle(
        ActivateRoutingRuleCommand request,
        CancellationToken cancellationToken)
    {
        var rule = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (rule is null)
        {
            return Result.Failure(new Error("RoutingRule.NotFound", $"Routing rule with ID '{request.Id}' was not found."));
        }

        var activateResult = rule.Activate();
        if (activateResult.IsFailure)
        {
            return activateResult;
        }

        await repository.UpdateAsync(rule, cancellationToken);

        return Result.Success();
    }
}
