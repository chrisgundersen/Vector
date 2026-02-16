using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vector.Application.UnderwritingGuidelines.Commands;
using Vector.Application.UnderwritingGuidelines.DTOs;
using Vector.Application.UnderwritingGuidelines.Queries;
using Vector.Domain.UnderwritingGuidelines.Enums;

namespace Vector.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Admin")]
public class GuidelinesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Lists all guidelines with optional status filter.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GuidelineSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] GuidelineStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = new GetGuidelinesQuery(status);
        var result = await mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a guideline by ID with all rules.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GuidelineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var guideline = await mediator.Send(new GetGuidelineQuery(id), cancellationToken);

        if (guideline is null)
        {
            return NotFound();
        }

        return Ok(guideline);
    }

    /// <summary>
    /// Creates a new guideline.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateGuidelineRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateGuidelineCommand(
            request.Name,
            request.Description,
            request.ApplicableCoverageTypes,
            request.ApplicableStates,
            request.ApplicableNAICSCodes,
            request.EffectiveDate,
            request.ExpirationDate);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to create guideline",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }

    /// <summary>
    /// Updates an existing guideline.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGuidelineRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateGuidelineCommand(
            id,
            request.Name,
            request.Description,
            request.ApplicableCoverageTypes,
            request.ApplicableStates,
            request.ApplicableNAICSCodes,
            request.EffectiveDate,
            request.ExpirationDate);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Guideline.NotFound")
            {
                return NotFound();
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Failed to update guideline",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Activates a guideline.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ActivateGuidelineCommand(id), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Guideline.NotFound")
            {
                return NotFound();
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Failed to activate guideline",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Deactivates a guideline.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeactivateGuidelineCommand(id), cancellationToken);

        if (result.IsFailure && result.Error.Code == "Guideline.NotFound")
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Archives (soft deletes) a guideline.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteGuidelineCommand(id), cancellationToken);

        if (result.IsFailure && result.Error.Code == "Guideline.NotFound")
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Adds a rule to a guideline.
    /// </summary>
    [HttpPost("{id:guid}/rules")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddRule(Guid id, [FromBody] AddRuleRequest request, CancellationToken cancellationToken)
    {
        var command = new AddRuleCommand(
            id,
            request.Name,
            request.Description,
            request.Type,
            request.Action,
            request.Priority,
            request.ScoreAdjustment,
            request.PricingModifier,
            request.Message);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Guideline.NotFound")
            {
                return NotFound();
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Failed to add rule",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Created($"/api/v1/guidelines/{id}/rules/{result.Value}", result.Value);
    }

    /// <summary>
    /// Removes a rule from a guideline.
    /// </summary>
    [HttpDelete("{id:guid}/rules/{ruleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRule(Guid id, Guid ruleId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RemoveRuleCommand(id, ruleId), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Guideline.NotFound")
            {
                return NotFound();
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Failed to remove rule",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return NoContent();
    }
}

public record CreateGuidelineRequest(
    string Name,
    string? Description,
    string? ApplicableCoverageTypes,
    string? ApplicableStates,
    string? ApplicableNAICSCodes,
    DateTime? EffectiveDate,
    DateTime? ExpirationDate);

public record UpdateGuidelineRequest(
    string Name,
    string? Description,
    string? ApplicableCoverageTypes,
    string? ApplicableStates,
    string? ApplicableNAICSCodes,
    DateTime? EffectiveDate,
    DateTime? ExpirationDate);

public record AddRuleRequest(
    string Name,
    string? Description,
    RuleType Type,
    RuleAction Action,
    int Priority,
    int? ScoreAdjustment,
    decimal? PricingModifier,
    string? Message);
