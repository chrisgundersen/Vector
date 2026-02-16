using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vector.Application.Routing.Commands;
using Vector.Application.Routing.DTOs;
using Vector.Application.Routing.Queries;
using Vector.Domain.Routing.Enums;

namespace Vector.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/routing-rules")]
public class RoutingRulesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Lists all routing rules with optional status filter.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoutingRuleSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] RoutingRuleStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRoutingRulesQuery(status);
        var result = await mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a routing rule by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoutingRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var rule = await mediator.Send(new GetRoutingRuleQuery(id), cancellationToken);

        if (rule is null)
        {
            return NotFound();
        }

        return Ok(rule);
    }

    /// <summary>
    /// Creates a new routing rule.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateRoutingRuleRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateRoutingRuleCommand(
            request.Name,
            request.Description,
            request.Strategy,
            request.Priority,
            request.TargetUnderwriterId,
            request.TargetUnderwriterName,
            request.TargetTeamId,
            request.TargetTeamName);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to create routing rule",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }

    /// <summary>
    /// Updates an existing routing rule.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoutingRuleRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateRoutingRuleCommand(
            id,
            request.Name,
            request.Description,
            request.Strategy,
            request.Priority,
            request.TargetUnderwriterId,
            request.TargetUnderwriterName,
            request.TargetTeamId,
            request.TargetTeamName);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "RoutingRule.NotFound")
            {
                return NotFound();
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Failed to update routing rule",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Activates a routing rule.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ActivateRoutingRuleCommand(id), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "RoutingRule.NotFound")
            {
                return NotFound();
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Failed to activate routing rule",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Deactivates a routing rule.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeactivateRoutingRuleCommand(id), cancellationToken);

        if (result.IsFailure && result.Error.Code == "RoutingRule.NotFound")
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Archives (soft deletes) a routing rule.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteRoutingRuleCommand(id), cancellationToken);

        if (result.IsFailure && result.Error.Code == "RoutingRule.NotFound")
        {
            return NotFound();
        }

        return NoContent();
    }
}

public record UpdateRoutingRuleRequest(
    string Name,
    string Description,
    RoutingStrategy Strategy,
    int Priority,
    Guid? TargetUnderwriterId,
    string? TargetUnderwriterName,
    Guid? TargetTeamId,
    string? TargetTeamName);

public record CreateRoutingRuleRequest(
    string Name,
    string Description,
    RoutingStrategy Strategy,
    int Priority,
    Guid? TargetUnderwriterId,
    string? TargetUnderwriterName,
    Guid? TargetTeamId,
    string? TargetTeamName);
