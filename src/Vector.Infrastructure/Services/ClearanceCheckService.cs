using System.Text.RegularExpressions;
using Vector.Domain.Submission;
using Vector.Domain.Submission.Entities;
using Vector.Domain.Submission.Enums;
using Vector.Domain.Submission.Services;

namespace Vector.Infrastructure.Services;

/// <summary>
/// Clearance check service that identifies potential duplicate submissions
/// based on FEIN, insured name, and mailing address matching.
/// </summary>
public sealed partial class ClearanceCheckService(
    ISubmissionRepository submissionRepository) : IClearanceCheckService
{
    private const double NameSimilarityThreshold = 0.75;
    private const double AddressSimilarityThreshold = 0.85;

    private static readonly string[] EntitySuffixes =
        ["LLC", "INC", "CORP", "LTD", "CO", "COMPANY", "CORPORATION", "INCORPORATED", "LIMITED"];

    private static readonly Dictionary<string, string> AddressAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["STREET"] = "ST",
        ["AVENUE"] = "AVE",
        ["BOULEVARD"] = "BLVD",
        ["DRIVE"] = "DR",
        ["LANE"] = "LN",
        ["ROAD"] = "RD",
        ["COURT"] = "CT",
        ["PLACE"] = "PL",
        ["CIRCLE"] = "CIR",
        ["HIGHWAY"] = "HWY",
        ["PARKWAY"] = "PKWY",
        ["SUITE"] = "STE",
        ["APARTMENT"] = "APT",
        ["NORTH"] = "N",
        ["SOUTH"] = "S",
        ["EAST"] = "E",
        ["WEST"] = "W"
    };

    public async Task<IReadOnlyList<ClearanceMatch>> CheckAsync(
        Domain.Submission.Aggregates.Submission submission,
        CancellationToken cancellationToken = default)
    {
        var candidates = await submissionRepository.FindPotentialDuplicatesAsync(
            submission.TenantId,
            submission.Id,
            cancellationToken);

        var matches = new List<ClearanceMatch>();

        foreach (var candidate in candidates)
        {
            CheckFeinMatch(submission, candidate, matches);
            CheckNameMatch(submission, candidate, matches);
            CheckAddressMatch(submission, candidate, matches);
        }

        return matches;
    }

    private static void CheckFeinMatch(
        Domain.Submission.Aggregates.Submission submission,
        Domain.Submission.Aggregates.Submission candidate,
        List<ClearanceMatch> matches)
    {
        if (string.IsNullOrWhiteSpace(submission.Insured.FeinNumber) ||
            string.IsNullOrWhiteSpace(candidate.Insured.FeinNumber))
        {
            return;
        }

        var submissionFein = NormalizeFein(submission.Insured.FeinNumber);
        var candidateFein = NormalizeFein(candidate.Insured.FeinNumber);

        if (string.Equals(submissionFein, candidateFein, StringComparison.OrdinalIgnoreCase))
        {
            matches.Add(new ClearanceMatch(
                Guid.NewGuid(),
                submission.Id,
                candidate.Id,
                candidate.SubmissionNumber,
                ClearanceMatchType.FeinMatch,
                1.0,
                $"Exact FEIN match: {candidateFein}"));
        }
    }

    private static void CheckNameMatch(
        Domain.Submission.Aggregates.Submission submission,
        Domain.Submission.Aggregates.Submission candidate,
        List<ClearanceMatch> matches)
    {
        var normalizedSubmissionName = NormalizeName(submission.Insured.Name);
        var normalizedCandidateName = NormalizeName(candidate.Insured.Name);

        if (string.IsNullOrWhiteSpace(normalizedSubmissionName) ||
            string.IsNullOrWhiteSpace(normalizedCandidateName))
        {
            return;
        }

        var similarity = CalculateSimilarity(normalizedSubmissionName, normalizedCandidateName);

        if (similarity >= NameSimilarityThreshold)
        {
            matches.Add(new ClearanceMatch(
                Guid.NewGuid(),
                submission.Id,
                candidate.Id,
                candidate.SubmissionNumber,
                ClearanceMatchType.NameMatch,
                similarity,
                $"Name similarity: '{submission.Insured.Name}' vs '{candidate.Insured.Name}' ({similarity:P0})"));
        }
    }

    private static void CheckAddressMatch(
        Domain.Submission.Aggregates.Submission submission,
        Domain.Submission.Aggregates.Submission candidate,
        List<ClearanceMatch> matches)
    {
        if (submission.Insured.MailingAddress is null || candidate.Insured.MailingAddress is null)
        {
            return;
        }

        var normalizedSubmissionAddr = NormalizeAddress(
            submission.Insured.MailingAddress.Street1,
            submission.Insured.MailingAddress.City,
            submission.Insured.MailingAddress.State,
            submission.Insured.MailingAddress.PostalCode);

        var normalizedCandidateAddr = NormalizeAddress(
            candidate.Insured.MailingAddress.Street1,
            candidate.Insured.MailingAddress.City,
            candidate.Insured.MailingAddress.State,
            candidate.Insured.MailingAddress.PostalCode);

        var similarity = CalculateSimilarity(normalizedSubmissionAddr, normalizedCandidateAddr);

        if (similarity >= AddressSimilarityThreshold)
        {
            matches.Add(new ClearanceMatch(
                Guid.NewGuid(),
                submission.Id,
                candidate.Id,
                candidate.SubmissionNumber,
                ClearanceMatchType.AddressMatch,
                similarity,
                $"Address similarity: ({similarity:P0})"));
        }
    }

    public static string NormalizeFein(string fein)
    {
        return DigitsOnly().Replace(fein, "");
    }

    public static string NormalizeName(string name)
    {
        var normalized = name.Trim().ToUpperInvariant();

        // Remove only the outermost entity suffix to preserve meaningful name parts
        // (e.g., "Test Company LLC" → "TEST COMPANY", not "TEST").
        foreach (var suffix in EntitySuffixes)
        {
            var matched = false;
            foreach (var pattern in new[] { $" {suffix}.", $",{suffix}.", $" {suffix}", $",{suffix}" })
            {
                if (normalized.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    normalized = normalized[..^pattern.Length];
                    matched = true;
                    break;
                }
            }

            if (matched) break;
        }

        // Remove punctuation and collapse whitespace
        normalized = PunctuationPattern().Replace(normalized, "");
        normalized = WhitespacePattern().Replace(normalized.Trim(), " ");

        return normalized;
    }

    public static string NormalizeAddress(string street, string city, string state, string postalCode)
    {
        var parts = street.ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var normalizedParts = new List<string>();

        foreach (var part in parts)
        {
            var cleaned = PunctuationPattern().Replace(part, "");
            if (AddressAbbreviations.TryGetValue(cleaned, out var abbreviation))
            {
                normalizedParts.Add(abbreviation);
            }
            else
            {
                normalizedParts.Add(cleaned);
            }
        }

        var normalizedStreet = string.Join(" ", normalizedParts);
        var normalizedCity = city.Trim().ToUpperInvariant();
        var normalizedState = state.Trim().ToUpperInvariant();
        var normalizedZip = DigitsOnly().Replace(postalCode.Trim(), "");
        if (normalizedZip.Length > 5)
        {
            normalizedZip = normalizedZip[..5];
        }

        return $"{normalizedStreet} {normalizedCity} {normalizedState} {normalizedZip}";
    }

    /// <summary>
    /// Calculates string similarity using Levenshtein distance, returning a value between 0.0 and 1.0.
    /// </summary>
    public static double CalculateSimilarity(string source, string target)
    {
        if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
        {
            return 1.0;
        }

        var maxLength = Math.Max(source.Length, target.Length);
        if (maxLength == 0)
        {
            return 1.0;
        }

        var distance = LevenshteinDistance(source, target);
        return 1.0 - ((double)distance / maxLength);
    }

    public static int LevenshteinDistance(string source, string target)
    {
        var sourceLength = source.Length;
        var targetLength = target.Length;

        if (sourceLength == 0) return targetLength;
        if (targetLength == 0) return sourceLength;

        var matrix = new int[sourceLength + 1, targetLength + 1];

        for (var i = 0; i <= sourceLength; i++)
        {
            matrix[i, 0] = i;
        }

        for (var j = 0; j <= targetLength; j++)
        {
            matrix[0, j] = j;
        }

        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = char.ToUpperInvariant(source[i - 1]) == char.ToUpperInvariant(target[j - 1]) ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[sourceLength, targetLength];
    }

    [GeneratedRegex(@"[^\w\s]")]
    private static partial Regex PunctuationPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex(@"\D")]
    private static partial Regex DigitsOnly();
}
