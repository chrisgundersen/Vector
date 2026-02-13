using MediatR;
using Vector.Application.EmailIntake.DTOs;
using Vector.Domain.EmailIntake;

namespace Vector.Application.EmailIntake.Queries;

/// <summary>
/// Handler for GetInboundEmailQuery.
/// </summary>
public sealed class GetInboundEmailQueryHandler(
    IInboundEmailRepository emailRepository) : IRequestHandler<GetInboundEmailQuery, InboundEmailDto?>
{
    public async Task<InboundEmailDto?> Handle(
        GetInboundEmailQuery request,
        CancellationToken cancellationToken)
    {
        var email = await emailRepository.GetByIdAsync(request.EmailId, cancellationToken);

        return email?.ToDto();
    }
}
