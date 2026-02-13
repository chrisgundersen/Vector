using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Domain.Common;
using Vector.Domain.Submission;
using Vector.Domain.Submission.ValueObjects;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Handler for QuoteSubmissionCommand.
/// </summary>
public sealed class QuoteSubmissionCommandHandler(
    ISubmissionRepository submissionRepository,
    ILogger<QuoteSubmissionCommandHandler> logger) : IRequestHandler<QuoteSubmissionCommand, Result>
{
    public async Task<Result> Handle(
        QuoteSubmissionCommand request,
        CancellationToken cancellationToken)
    {
        var submission = await submissionRepository.GetByIdAsync(request.SubmissionId, cancellationToken);

        if (submission is null)
        {
            return Result.Failure(new Error(
                "Submission.NotFound",
                $"Submission with ID {request.SubmissionId} was not found."));
        }

        var premium = Money.FromDecimal(request.PremiumAmount, request.Currency);
        var result = submission.Quote(premium);

        if (result.IsFailure)
        {
            return result;
        }

        submissionRepository.Update(submission);

        logger.LogInformation(
            "Quoted submission {SubmissionId} with premium {Premium}",
            request.SubmissionId, premium);

        return Result.Success();
    }
}
