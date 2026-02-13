using MediatR;
using Vector.Domain.Common;
using Vector.Domain.Routing;

namespace Vector.Application.Routing.Commands;

/// <summary>
/// Handler for DeleteRoutingRuleCommand.
/// Archives the routing rule rather than hard-deleting it.
/// </summary>
public sealed class DeleteRoutingRuleCommandHandler(
    IRoutingRuleRepository repository) : IRequestHandler<DeleteRoutingRuleCommand, Result>
{
    public async Task<Result> Handle(
        DeleteRoutingRuleCommand request,
        CancellationToken cancellationToken)
    {
        var rule = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (rule is null)
        {
            return Result.Failure(new Error("RoutingRule.NotFound", $"Routing rule with ID '{request.Id}' was not found."));
        }

        rule.Archive();

        await repository.UpdateAsync(rule, cancellationToken);

        return Result.Success();
    }
}
