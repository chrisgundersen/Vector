using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vector.Application.Routing.Commands;
using Vector.Application.Routing.DTOs;
using Vector.Application.Routing.Queries;

namespace Vector.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Admin")]
public class PairingsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Lists all producer-underwriter pairings.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PairingSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPairingsQuery(activeOnly);
        var result = await mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a pairing by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PairingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var pairing = await mediator.Send(new GetPairingQuery(id), cancellationToken);

        if (pairing is null)
        {
            return NotFound();
        }

        return Ok(pairing);
    }

    /// <summary>
    /// Creates a new producer-underwriter pairing.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePairingRequest request, CancellationToken cancellationToken)
    {
        var command = new CreatePairingCommand(
            request.ProducerId,
            request.ProducerName,
            request.UnderwriterId,
            request.UnderwriterName,
            request.Priority,
            request.EffectiveFrom,
            request.EffectiveUntil,
            request.CoverageTypes);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to create pairing",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }

    /// <summary>
    /// Updates an existing pairing.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePairingRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdatePairingCommand(
            id,
            request.Priority,
            request.EffectiveFrom,
            request.EffectiveUntil,
            request.CoverageTypes);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Pairing.NotFound")
            {
                return NotFound();
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Failed to update pairing",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Activates a pairing.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ActivatePairingCommand(id), cancellationToken);

        if (result.IsFailure && result.Error.Code == "Pairing.NotFound")
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Deactivates a pairing.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeactivatePairingCommand(id), cancellationToken);

        if (result.IsFailure && result.Error.Code == "Pairing.NotFound")
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes a pairing.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeletePairingCommand(id), cancellationToken);

        if (result.IsFailure && result.Error.Code == "Pairing.NotFound")
        {
            return NotFound();
        }

        return NoContent();
    }
}

public record UpdatePairingRequest(
    int Priority,
    DateTime EffectiveFrom,
    DateTime? EffectiveUntil,
    IReadOnlyList<string>? CoverageTypes);

public record CreatePairingRequest(
    Guid ProducerId,
    string ProducerName,
    Guid UnderwriterId,
    string UnderwriterName,
    int Priority,
    DateTime EffectiveFrom,
    DateTime? EffectiveUntil,
    IReadOnlyList<string>? CoverageTypes);
