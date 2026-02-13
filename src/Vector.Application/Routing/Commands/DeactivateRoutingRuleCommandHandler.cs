using MediatR;
using Vector.Domain.Common;
using Vector.Domain.Routing;

namespace Vector.Application.Routing.Commands;

/// <summary>
/// Handler for DeactivateRoutingRuleCommand.
/// </summary>
public sealed class DeactivateRoutingRuleCommandHandler(
    IRoutingRuleRepository repository) : IRequestHandler<DeactivateRoutingRuleCommand, Result>
{
    public async Task<Result> Handle(
        DeactivateRoutingRuleCommand request,
        CancellationToken cancellationToken)
    {
        var rule = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (rule is null)
        {
            return Result.Failure(new Error("RoutingRule.NotFound", $"Routing rule with ID '{request.Id}' was not found."));
        }

        rule.Deactivate();

        await repository.UpdateAsync(rule, cancellationToken);

        return Result.Success();
    }
}
