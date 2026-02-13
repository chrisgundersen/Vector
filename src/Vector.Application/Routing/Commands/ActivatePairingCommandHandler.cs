using MediatR;
using Vector.Domain.Common;
using Vector.Domain.Routing;

namespace Vector.Application.Routing.Commands;

/// <summary>
/// Handler for ActivatePairingCommand.
/// </summary>
public sealed class ActivatePairingCommandHandler(
    IProducerUnderwriterPairingRepository repository) : IRequestHandler<ActivatePairingCommand, Result>
{
    public async Task<Result> Handle(
        ActivatePairingCommand request,
        CancellationToken cancellationToken)
    {
        var pairing = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (pairing is null)
        {
            return Result.Failure(new Error("Pairing.NotFound", $"Pairing with ID '{request.Id}' was not found."));
        }

        pairing.Activate();

        await repository.UpdateAsync(pairing, cancellationToken);

        return Result.Success();
    }
}
