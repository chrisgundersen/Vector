using MediatR;
using Vector.Domain.Common;
using Vector.Domain.Routing;

namespace Vector.Application.Routing.Commands;

/// <summary>
/// Handler for DeletePairingCommand.
/// </summary>
public sealed class DeletePairingCommandHandler(
    IProducerUnderwriterPairingRepository repository) : IRequestHandler<DeletePairingCommand, Result>
{
    public async Task<Result> Handle(
        DeletePairingCommand request,
        CancellationToken cancellationToken)
    {
        var pairing = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (pairing is null)
        {
            return Result.Failure(new Error("Pairing.NotFound", $"Pairing with ID '{request.Id}' was not found."));
        }

        await repository.DeleteAsync(request.Id, cancellationToken);

        return Result.Success();
    }
}
