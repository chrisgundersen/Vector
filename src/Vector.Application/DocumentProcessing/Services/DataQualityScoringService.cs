using Microsoft.Extensions.Logging;
using Vector.Domain.DocumentProcessing.Aggregates;
using Vector.Domain.DocumentProcessing.Enums;
using Vector.Domain.Submission.Aggregates;

namespace Vector.Application.DocumentProcessing.Services;

/// <summary>
/// Service for calculating data quality scores for processed documents and submissions.
/// </summary>
public class DataQualityScoringService(
    ILogger<DataQualityScoringService> logger) : IDataQualityScoringService
{
    // Required fields for ACORD 125 (Commercial Insurance Application)
    private static readonly string[] RequiredAcord125Fields =
    [
        "InsuredName",
        "InsuredAddress",
        "InsuredCity",
        "InsuredState",
        "InsuredZip",
        "EffectiveDate"
    ];

    // Recommended fields for better data quality
    private static readonly string[] RecommendedAcord125Fields =
    [
        "FEIN",
        "BusinessDescription",
        "NAICSCode",
        "YearsInBusiness",
        "EmployeeCount",
        "AnnualRevenue",
        "ExpirationDate",
        "InsuredDBA"
    ];

    // Required fields for loss runs
    private static readonly string[] RequiredLossRunFields =
    [
        "LossCount"
    ];

    // Required fields per loss
    private static readonly string[] RequiredLossFields =
    [
        "DateOfLoss"
    ];

    // Required fields for exposure schedules
    private static readonly string[] RequiredExposureScheduleFields =
    [
        "LocationCount"
    ];

    public DataQualityScore CalculateJobScore(ProcessingJob processingJob)
    {
        var issues = new List<DataQualityIssue>();
        var documentScores = new List<int>();

        // Score each document
        foreach (var document in processingJob.Documents)
        {
            var docScore = CalculateDocumentScore(document);
            documentScores.Add(docScore.OverallScore);
            issues.AddRange(docScore.Issues);
        }

        // Calculate coverage score based on document types present
        var coverageScore = CalculateCoverageScore(processingJob, issues);

        // Calculate aggregate scores
        var avgDocumentScore = documentScores.Count > 0
            ? (int)documentScores.Average()
            : 0;

        // Calculate completeness from document average
        var completenessScore = documentScores.Count > 0
            ? (int)processingJob.Documents.Average(d => CalculateCompletenessScore(d))
            : 0;

        // Calculate confidence from document average
        var confidenceScore = documentScores.Count > 0
            ? (int)processingJob.Documents.Average(d => CalculateConfidenceScore(d))
            : 0;

        // Calculate validation score
        var validationScore = CalculateJobValidationScore(processingJob, issues);

        // Weight the scores
        // Overall = 30% completeness + 25% confidence + 25% validation + 20% coverage
        var overallScore = (int)(
            completenessScore * 0.30 +
            confidenceScore * 0.25 +
            validationScore * 0.25 +
            coverageScore * 0.20);

        logger.LogDebug(
            "Calculated job score for {JobId}: Overall={Overall}, Completeness={Completeness}, " +
            "Confidence={Confidence}, Validation={Validation}, Coverage={Coverage}",
            processingJob.Id,
            overallScore,
            completenessScore,
            confidenceScore,
            validationScore,
            coverageScore);

        return new DataQualityScore(
            overallScore,
            completenessScore,
            confidenceScore,
            validationScore,
            coverageScore,
            issues);
    }

    public DataQualityScore CalculateSubmissionScore(Submission submission)
    {
        var issues = new List<DataQualityIssue>();

        // Check insured information completeness
        var insuredScore = ScoreInsuredInfo(submission, issues);

        // Check coverages
        var coverageScore = ScoreCoverages(submission, issues);

        // Check locations
        var locationScore = ScoreLocations(submission, issues);

        // Check loss history
        var lossHistoryScore = ScoreLossHistory(submission, issues);

        // Calculate weights based on what's present
        var weights = CalculateSubmissionWeights(submission);

        var overallScore = (int)(
            insuredScore * weights.InsuredWeight +
            coverageScore * weights.CoverageWeight +
            locationScore * weights.LocationWeight +
            lossHistoryScore * weights.LossHistoryWeight);

        // Adjust for critical missing data
        if (submission.Insured.Name is null or { Length: 0 })
        {
            overallScore = Math.Min(overallScore, 30);
        }

        if (submission.Coverages.Count == 0)
        {
            overallScore = Math.Min(overallScore, 50);
        }

        logger.LogDebug(
            "Calculated submission score for {SubmissionNumber}: Overall={Overall}, " +
            "Issues={IssueCount}",
            submission.SubmissionNumber,
            overallScore,
            issues.Count);

        return new DataQualityScore(
            overallScore,
            insuredScore,
            coverageScore,
            locationScore,
            lossHistoryScore,
            issues);
    }

    public DataQualityScore CalculateDocumentScore(ProcessedDocument document)
    {
        var issues = new List<DataQualityIssue>();

        var completenessScore = CalculateCompletenessScore(document);
        var confidenceScore = CalculateConfidenceScore(document);
        var validationScore = CalculateDocumentValidationScore(document, issues);

        // Coverage score not applicable for single document
        var coverageScore = 100;

        // Collect issues
        CollectDocumentIssues(document, issues);

        // Overall = 40% completeness + 35% confidence + 25% validation
        var overallScore = (int)(
            completenessScore * 0.40 +
            confidenceScore * 0.35 +
            validationScore * 0.25);

        return new DataQualityScore(
            overallScore,
            completenessScore,
            confidenceScore,
            validationScore,
            coverageScore,
            issues);
    }

    private int CalculateCompletenessScore(ProcessedDocument document)
    {
        var requiredFields = GetRequiredFieldsForDocumentType(document.DocumentType);
        var recommendedFields = GetRecommendedFieldsForDocumentType(document.DocumentType);

        if (requiredFields.Length == 0)
        {
            // For document types without defined requirements, use field count
            return document.ExtractedFields.Count > 10 ? 100 :
                   document.ExtractedFields.Count > 5 ? 80 :
                   document.ExtractedFields.Count > 0 ? 60 : 0;
        }

        // Calculate required field coverage (70% weight)
        var requiredCount = requiredFields.Count(f =>
            !string.IsNullOrWhiteSpace(document.GetFieldValue(f)));
        var requiredScore = requiredFields.Length > 0
            ? (requiredCount * 100 / requiredFields.Length)
            : 100;

        // Calculate recommended field coverage (30% weight)
        var recommendedCount = recommendedFields.Count(f =>
            !string.IsNullOrWhiteSpace(document.GetFieldValue(f)));
        var recommendedScore = recommendedFields.Length > 0
            ? (recommendedCount * 100 / recommendedFields.Length)
            : 100;

        return (int)(requiredScore * 0.70 + recommendedScore * 0.30);
    }

    private static int CalculateConfidenceScore(ProcessedDocument document)
    {
        if (document.ExtractedFields.Count == 0)
        {
            return 0;
        }

        var avgConfidence = document.ExtractedFields.Average(f => f.Confidence.Score);
        return (int)(avgConfidence * 100);
    }

    private static int CalculateDocumentValidationScore(ProcessedDocument document, List<DataQualityIssue> issues)
    {
        if (document.ValidationErrors.Count == 0)
        {
            return 100;
        }

        // Each validation error reduces score by 15 points
        var penalty = document.ValidationErrors.Count * 15;
        var score = Math.Max(0, 100 - penalty);

        foreach (var error in document.ValidationErrors)
        {
            issues.Add(new DataQualityIssue(
                DataQualityIssueType.ValidationError,
                document.OriginalFileName,
                error,
                DataQualitySeverity.Medium));
        }

        return score;
    }

    private static int CalculateJobValidationScore(ProcessingJob job, List<DataQualityIssue> issues)
    {
        var totalErrors = job.Documents.Sum(d => d.ValidationErrors.Count);
        var failedDocuments = job.Documents.Count(d => d.Status == ProcessingStatus.Failed);

        // Start with 100, deduct for errors and failures
        var score = 100;
        score -= totalErrors * 10;
        score -= failedDocuments * 20;

        if (failedDocuments > 0)
        {
            issues.Add(new DataQualityIssue(
                DataQualityIssueType.ValidationError,
                "Processing",
                $"{failedDocuments} document(s) failed to process",
                failedDocuments > job.Documents.Count / 2
                    ? DataQualitySeverity.Critical
                    : DataQualitySeverity.High));
        }

        return Math.Max(0, score);
    }

    private static int CalculateCoverageScore(ProcessingJob job, List<DataQualityIssue> issues)
    {
        var score = 0;

        // Has ACORD form (required) - 50 points
        if (job.HasAcordForms)
        {
            score += 50;
        }
        else
        {
            issues.Add(new DataQualityIssue(
                DataQualityIssueType.MissingDocument,
                "ACORD Form",
                "No ACORD form was found in the submission",
                DataQualitySeverity.High));
        }

        // Has loss runs (recommended) - 25 points
        if (job.HasLossRuns)
        {
            score += 25;
        }
        else
        {
            issues.Add(new DataQualityIssue(
                DataQualityIssueType.MissingDocument,
                "Loss Run",
                "No loss run report was found",
                DataQualitySeverity.Low));
        }

        // Has exposure schedule (recommended for property) - 25 points
        if (job.HasExposureSchedule)
        {
            score += 25;
        }
        else
        {
            issues.Add(new DataQualityIssue(
                DataQualityIssueType.MissingDocument,
                "Exposure Schedule",
                "No exposure schedule/SOV was found",
                DataQualitySeverity.Low));
        }

        return score;
    }

    private void CollectDocumentIssues(ProcessedDocument document, List<DataQualityIssue> issues)
    {
        var requiredFields = GetRequiredFieldsForDocumentType(document.DocumentType);

        // Check for missing required fields
        foreach (var field in requiredFields)
        {
            if (string.IsNullOrWhiteSpace(document.GetFieldValue(field)))
            {
                issues.Add(new DataQualityIssue(
                    DataQualityIssueType.MissingRequiredField,
                    field,
                    $"Required field '{field}' was not extracted",
                    DataQualitySeverity.High));
            }
        }

        // Check for low confidence fields
        foreach (var field in document.ExtractedFields.Where(f => f.Confidence.IsLowConfidence))
        {
            issues.Add(new DataQualityIssue(
                DataQualityIssueType.LowConfidence,
                field.FieldName,
                $"Field '{field.FieldName}' has low confidence ({field.Confidence})",
                DataQualitySeverity.Medium));
        }
    }

    private static string[] GetRequiredFieldsForDocumentType(DocumentType documentType)
    {
        return documentType switch
        {
            DocumentType.Acord125 => RequiredAcord125Fields,
            DocumentType.Acord126 => ["InsuredName", "GLLimit"],
            DocumentType.Acord130 => ["InsuredName", "State"],
            DocumentType.Acord140 => ["InsuredName"],
            DocumentType.LossRunReport => RequiredLossRunFields,
            DocumentType.ExposureSchedule => RequiredExposureScheduleFields,
            _ => []
        };
    }

    private static string[] GetRecommendedFieldsForDocumentType(DocumentType documentType)
    {
        return documentType switch
        {
            DocumentType.Acord125 => RecommendedAcord125Fields,
            DocumentType.Acord126 => ["GLDeductible", "ProductsLimit", "EffectiveDate"],
            DocumentType.Acord130 => ["EmployeeCount", "Payroll", "EffectiveDate"],
            DocumentType.Acord140 => ["PropertyLimit", "BuildingValue", "EffectiveDate"],
            _ => []
        };
    }

    private static int ScoreInsuredInfo(Submission submission, List<DataQualityIssue> issues)
    {
        var score = 0;
        var maxScore = 100;

        // Name (required) - 30 points
        if (!string.IsNullOrWhiteSpace(submission.Insured.Name))
        {
            score += 30;
        }
        else
        {
            issues.Add(new DataQualityIssue(
                DataQualityIssueType.MissingRequiredField,
                "InsuredName",
                "Insured name is missing",
                DataQualitySeverity.Critical));
        }

        // Address - 20 points
        if (submission.Insured.MailingAddress is not null)
        {
            score += 20;
        }
        else
        {
            issues.Add(new DataQualityIssue(
                DataQualityIssueType.MissingRequiredField,
                "MailingAddress",
                "Insured address is missing",
                DataQualitySeverity.High));
        }

        // Industry classification - 15 points
        if (submission.Insured.Industry is not null)
        {
            score += 15;
        }
        else
        {
            issues.Add(new DataQualityIssue(
                DataQualityIssueType.MissingRequiredField,
                "Industry",
                "Industry classification (NAICS) is missing",
                DataQualitySeverity.Medium));
        }

        // FEIN - 10 points
        if (!string.IsNullOrWhiteSpace(submission.Insured.FeinNumber))
        {
            score += 10;
        }

        // Years in business - 5 points
        if (submission.Insured.YearsInBusiness.HasValue)
        {
            score += 5;
        }

        // Employee count - 5 points
        if (submission.Insured.EmployeeCount.HasValue)
        {
            score += 5;
        }

        // Annual revenue - 10 points
        if (submission.Insured.AnnualRevenue is not null)
        {
            score += 10;
        }

        // Policy dates - 5 points
        if (submission.EffectiveDate.HasValue)
        {
            score += 5;
        }

        return Math.Min(score, maxScore);
    }

    private static int ScoreCoverages(Submission submission, List<DataQualityIssue> issues)
    {
        if (submission.Coverages.Count == 0)
        {
            issues.Add(new DataQualityIssue(
                DataQualityIssueType.MissingRequiredField,
                "Coverages",
                "No coverage information found",
                DataQualitySeverity.Critical));
            return 0;
        }

        var totalScore = 0;
        foreach (var coverage in submission.Coverages)
        {
            var coverageScore = 50; // Base score for having coverage

            if (coverage.RequestedLimit is not null)
            {
                coverageScore += 30;
            }
            else
            {
                issues.Add(new DataQualityIssue(
                    DataQualityIssueType.MissingRequiredField,
                    $"{coverage.Type}Limit",
                    $"Requested limit for {coverage.Type} is missing",
                    DataQualitySeverity.Medium));
            }

            if (coverage.RequestedDeductible is not null)
            {
                coverageScore += 20;
            }

            totalScore += coverageScore;
        }

        return Math.Min(totalScore / submission.Coverages.Count, 100);
    }

    private static int ScoreLocations(Submission submission, List<DataQualityIssue> issues)
    {
        if (submission.Locations.Count == 0)
        {
            // Not having locations is only an issue for property coverages
            var hasPropertyCoverage = submission.Coverages.Any(c =>
                c.Type == Domain.Submission.Enums.CoverageType.PropertyDamage);

            if (hasPropertyCoverage)
            {
                issues.Add(new DataQualityIssue(
                    DataQualityIssueType.MissingRequiredField,
                    "Locations",
                    "No location information for property coverage",
                    DataQualitySeverity.High));
                return 50;
            }

            return 100; // No locations needed
        }

        var totalScore = 0;
        foreach (var location in submission.Locations)
        {
            var locationScore = 40; // Base for having address

            if (location.BuildingValue is not null)
                locationScore += 20;

            if (location.ContentsValue is not null)
                locationScore += 15;

            if (!string.IsNullOrWhiteSpace(location.ConstructionType))
                locationScore += 10;

            if (location.YearBuilt.HasValue)
                locationScore += 10;

            if (location.SquareFootage.HasValue)
                locationScore += 5;

            totalScore += locationScore;
        }

        return Math.Min(totalScore / submission.Locations.Count, 100);
    }

    private static int ScoreLossHistory(Submission submission, List<DataQualityIssue> issues)
    {
        // Having loss history data is good, but not required
        if (submission.LossHistory.Count == 0)
        {
            return 80; // Partial score - might have no losses or missing data
        }

        var totalScore = 0;
        foreach (var loss in submission.LossHistory)
        {
            var lossScore = 40; // Base for having date

            if (!string.IsNullOrWhiteSpace(loss.ClaimNumber))
                lossScore += 15;

            if (loss.PaidAmount is not null || loss.IncurredAmount is not null)
                lossScore += 25;

            if (loss.CoverageType.HasValue)
                lossScore += 10;

            if (!string.IsNullOrWhiteSpace(loss.Description) && loss.Description.Length > 10)
                lossScore += 10;

            totalScore += lossScore;
        }

        return Math.Min(totalScore / submission.LossHistory.Count, 100);
    }

    private static (double InsuredWeight, double CoverageWeight, double LocationWeight, double LossHistoryWeight)
        CalculateSubmissionWeights(Submission submission)
    {
        // Base weights
        var insuredWeight = 0.40;
        var coverageWeight = 0.30;
        var locationWeight = 0.15;
        var lossHistoryWeight = 0.15;

        // Adjust for property coverage
        var hasProperty = submission.Coverages.Any(c =>
            c.Type == Domain.Submission.Enums.CoverageType.PropertyDamage);

        if (hasProperty)
        {
            insuredWeight = 0.35;
            coverageWeight = 0.25;
            locationWeight = 0.25;
            lossHistoryWeight = 0.15;
        }

        return (insuredWeight, coverageWeight, locationWeight, lossHistoryWeight);
    }
}
