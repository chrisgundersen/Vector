using System.Globalization;
using Microsoft.Extensions.Logging;
using Vector.Domain.Common;
using Vector.Domain.DocumentProcessing.Aggregates;
using Vector.Domain.DocumentProcessing.Enums;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Entities;
using Vector.Domain.Submission.Enums;
using Vector.Domain.Submission.ValueObjects;

// ReSharper disable ConvertIfStatementToConditionalTernaryExpression

namespace Vector.Application.DocumentProcessing.Services;

/// <summary>
/// Service for creating Submission aggregates from processed document data.
/// </summary>
public class SubmissionCreationService(
    ILogger<SubmissionCreationService> logger) : ISubmissionCreationService
{
    public Task<Result<Submission>> CreateSubmissionFromJobAsync(
        ProcessingJob processingJob,
        CancellationToken cancellationToken = default)
    {
        if (processingJob.Status != ProcessingStatus.Completed)
        {
            return Task.FromResult(Result.Failure<Submission>(
                SubmissionCreationErrors.ProcessingJobNotCompleted));
        }

        var acordDocument = GetPrimaryAcordDocument(processingJob);
        if (acordDocument is null)
        {
            return Task.FromResult(Result.Failure<Submission>(
                SubmissionCreationErrors.NoAcordFormFound));
        }

        // Extract insured name (required)
        var insuredName = acordDocument.GetFieldValue("InsuredName");
        if (string.IsNullOrWhiteSpace(insuredName))
        {
            return Task.FromResult(Result.Failure<Submission>(
                SubmissionCreationErrors.InsuredNameNotExtracted));
        }

        // Generate submission number
        var submissionNumber = GenerateSubmissionNumber();

        // Create the submission
        var submissionResult = Submission.Create(
            processingJob.TenantId,
            submissionNumber,
            insuredName,
            processingJob.Id,
            processingJob.InboundEmailId);

        if (submissionResult.IsFailure)
        {
            return Task.FromResult(submissionResult);
        }

        var submission = submissionResult.Value;

        // Enrich with data from all documents
        EnrichSubmissionFromAcord(submission, acordDocument);
        EnrichSubmissionFromLossRuns(submission, processingJob);
        EnrichSubmissionFromExposureSchedules(submission, processingJob);

        // Mark as received
        submission.MarkAsReceived();

        logger.LogInformation(
            "Created submission {SubmissionNumber} from processing job {JobId}. " +
            "Coverages: {CoverageCount}, Locations: {LocationCount}, Losses: {LossCount}",
            submission.SubmissionNumber,
            processingJob.Id,
            submission.Coverages.Count,
            submission.Locations.Count,
            submission.LossHistory.Count);

        return Task.FromResult(Result.Success(submission));
    }

    public Task<Result> EnrichSubmissionAsync(
        Submission submission,
        ProcessingJob processingJob,
        CancellationToken cancellationToken = default)
    {
        if (processingJob.Status != ProcessingStatus.Completed)
        {
            return Task.FromResult(Result.Failure(
                SubmissionCreationErrors.ProcessingJobNotCompleted));
        }

        var acordDocument = GetPrimaryAcordDocument(processingJob);
        if (acordDocument is not null)
        {
            EnrichSubmissionFromAcord(submission, acordDocument);
        }

        EnrichSubmissionFromLossRuns(submission, processingJob);
        EnrichSubmissionFromExposureSchedules(submission, processingJob);

        logger.LogInformation(
            "Enriched submission {SubmissionNumber} from processing job {JobId}",
            submission.SubmissionNumber,
            processingJob.Id);

        return Task.FromResult(Result.Success());
    }

    private static ProcessedDocument? GetPrimaryAcordDocument(ProcessingJob job)
    {
        // Priority: ACORD 125 > ACORD 127 > other ACORD forms
        return job.GetDocumentsByType(DocumentType.Acord125).FirstOrDefault() ??
               job.GetDocumentsByType(DocumentType.Acord127).FirstOrDefault() ??
               job.Documents.FirstOrDefault(d =>
                   d.DocumentType is DocumentType.Acord126 or DocumentType.Acord130 or
                       DocumentType.Acord140 or DocumentType.Acord137);
    }

    private void EnrichSubmissionFromAcord(Submission submission, ProcessedDocument acordDocument)
    {
        // Update insured information
        var insuredAddress = TryCreateAddress(
            acordDocument.GetFieldValue("InsuredAddress"),
            acordDocument.GetFieldValue("InsuredAddress2"),
            acordDocument.GetFieldValue("InsuredCity"),
            acordDocument.GetFieldValue("InsuredState"),
            acordDocument.GetFieldValue("InsuredZip"));

        if (insuredAddress is not null)
        {
            submission.Insured.UpdateMailingAddress(insuredAddress);
        }

        // Update DBA
        var dba = acordDocument.GetFieldValue("InsuredDBA");
        if (!string.IsNullOrWhiteSpace(dba))
        {
            submission.Insured.UpdateDbaName(dba);
        }

        // Update FEIN
        var fein = acordDocument.GetFieldValue("FEIN");
        if (!string.IsNullOrWhiteSpace(fein))
        {
            submission.Insured.UpdateFein(fein);
        }

        // Update business classification (using IndustryClassification)
        var naicsCode = acordDocument.GetFieldValue("NAICSCode");
        var sicCode = acordDocument.GetFieldValue("SICCode");
        var businessDescription = acordDocument.GetFieldValue("BusinessDescription") ?? "Not specified";

        if (!string.IsNullOrWhiteSpace(naicsCode))
        {
            var industryResult = IndustryClassification.Create(naicsCode, sicCode, businessDescription);
            if (industryResult.IsSuccess)
            {
                submission.Insured.UpdateIndustry(industryResult.Value);
            }
        }

        // Update years in business
        var yearsInBusinessStr = acordDocument.GetFieldValue("YearsInBusiness");
        if (int.TryParse(yearsInBusinessStr, out var yearsInBusiness))
        {
            submission.Insured.UpdateYearsInBusiness(yearsInBusiness);
        }

        // Update employee count
        var employeeCountStr = acordDocument.GetFieldValue("EmployeeCount");
        if (int.TryParse(employeeCountStr?.Replace(",", ""), out var employeeCount))
        {
            submission.Insured.UpdateEmployeeCount(employeeCount);
        }

        // Update annual revenue
        var annualRevenueStr = acordDocument.GetFieldValue("AnnualRevenue");
        var annualRevenue = TryParseMoney(annualRevenueStr);
        if (annualRevenue is not null)
        {
            submission.Insured.UpdateAnnualRevenue(annualRevenue);
        }

        // Update policy dates
        var effectiveDate = TryParseDate(acordDocument.GetFieldValue("EffectiveDate"));
        var expirationDate = TryParseDate(acordDocument.GetFieldValue("ExpirationDate"));
        if (effectiveDate.HasValue || expirationDate.HasValue)
        {
            submission.UpdatePolicyDates(effectiveDate, expirationDate);
        }

        // Add coverages based on document type and extracted fields
        AddCoveragesFromAcord(submission, acordDocument);

        logger.LogDebug(
            "Enriched submission from ACORD document {DocumentId}: {DocumentType}",
            acordDocument.Id,
            acordDocument.DocumentType);
    }

    private void AddCoveragesFromAcord(Submission submission, ProcessedDocument acordDocument)
    {
        // Add coverages based on document type
        switch (acordDocument.DocumentType)
        {
            case DocumentType.Acord125:
                // Commercial Insurance Application - may have multiple coverages
                TryAddCoverage(submission, acordDocument, CoverageType.GeneralLiability, "GLLimit", "GLDeductible");
                TryAddCoverage(submission, acordDocument, CoverageType.PropertyDamage, "PropertyLimit", "PropertyDeductible");
                break;

            case DocumentType.Acord126:
                // General Liability Section
                TryAddCoverage(submission, acordDocument, CoverageType.GeneralLiability, "GLLimit", "GLDeductible");
                TryAddCoverage(submission, acordDocument, CoverageType.ProductsCompleted, "ProductsLimit", "ProductsDeductible");
                break;

            case DocumentType.Acord130:
                // Workers Compensation
                TryAddCoverage(submission, acordDocument, CoverageType.WorkersCompensation, "WCLimit", "WCDeductible");
                break;

            case DocumentType.Acord140:
                // Property Section
                TryAddCoverage(submission, acordDocument, CoverageType.PropertyDamage, "PropertyLimit", "PropertyDeductible");
                break;

            case DocumentType.Acord127:
                // Business Owners
                TryAddCoverage(submission, acordDocument, CoverageType.GeneralLiability, "GLLimit", "GLDeductible");
                TryAddCoverage(submission, acordDocument, CoverageType.PropertyDamage, "PropertyLimit", "PropertyDeductible");
                break;

            case DocumentType.Acord137:
                // Inland Marine
                TryAddCoverage(submission, acordDocument, CoverageType.InlandMarine, "InlandMarineLimit", "InlandMarineDeductible");
                break;
        }
    }

    private void TryAddCoverage(
        Submission submission,
        ProcessedDocument document,
        CoverageType coverageType,
        string limitFieldName,
        string deductibleFieldName)
    {
        var limitStr = document.GetFieldValue(limitFieldName);
        var limit = TryParseMoney(limitStr);

        // Only add if we have at least a limit
        if (limit is null) return;

        var coverage = submission.AddCoverage(coverageType);
        coverage.UpdateRequestedLimit(limit);

        var deductibleStr = document.GetFieldValue(deductibleFieldName);
        var deductible = TryParseMoney(deductibleStr);
        if (deductible is not null)
        {
            coverage.UpdateRequestedDeductible(deductible);
        }

        logger.LogDebug(
            "Added {CoverageType} coverage with limit {Limit} and deductible {Deductible}",
            coverageType,
            limit,
            deductible);
    }

    private void EnrichSubmissionFromLossRuns(Submission submission, ProcessingJob job)
    {
        var lossRunDocuments = job.GetDocumentsByType(DocumentType.LossRunReport).ToList();
        if (lossRunDocuments.Count == 0) return;

        foreach (var lossRunDoc in lossRunDocuments)
        {
            var lossCountStr = lossRunDoc.GetFieldValue("LossCount");
            if (!int.TryParse(lossCountStr, out var lossCount) || lossCount == 0) continue;

            for (var i = 1; i <= lossCount; i++)
            {
                var prefix = $"Loss_{i}_";

                var dateOfLossStr = lossRunDoc.GetFieldValue($"{prefix}DateOfLoss");
                var dateOfLoss = TryParseDate(dateOfLossStr);
                if (!dateOfLoss.HasValue) continue;

                var description = lossRunDoc.GetFieldValue($"{prefix}Description") ?? "Loss extracted from loss run";

                var loss = submission.AddLoss(dateOfLoss.Value, description);

                // Update claim info
                var claimNumber = lossRunDoc.GetFieldValue($"{prefix}ClaimNumber");
                var coverageTypeStr = lossRunDoc.GetFieldValue($"{prefix}CoverageType");
                var coverageType = TryParseCoverageType(coverageTypeStr);
                var carrier = lossRunDoc.GetFieldValue("CarrierName");

                loss.UpdateClaimInfo(claimNumber, coverageType, carrier);

                // Update amounts
                var paid = TryParseMoney(lossRunDoc.GetFieldValue($"{prefix}PaidAmount"));
                var reserved = TryParseMoney(lossRunDoc.GetFieldValue($"{prefix}ReservedAmount"));
                var incurred = TryParseMoney(lossRunDoc.GetFieldValue($"{prefix}IncurredAmount"));

                loss.UpdateAmounts(paid, reserved, incurred);

                // Update status
                var statusStr = lossRunDoc.GetFieldValue($"{prefix}Status");
                var status = TryParseLossStatus(statusStr);
                loss.UpdateStatus(status);
            }
        }

        logger.LogDebug(
            "Added {LossCount} losses from {DocumentCount} loss run documents",
            submission.LossHistory.Count,
            lossRunDocuments.Count);
    }

    private void EnrichSubmissionFromExposureSchedules(Submission submission, ProcessingJob job)
    {
        var exposureDocuments = job.GetDocumentsByType(DocumentType.ExposureSchedule).ToList();
        if (exposureDocuments.Count == 0) return;

        foreach (var exposureDoc in exposureDocuments)
        {
            var locationCountStr = exposureDoc.GetFieldValue("LocationCount");
            if (!int.TryParse(locationCountStr, out var locationCount) || locationCount == 0) continue;

            for (var i = 1; i <= locationCount; i++)
            {
                // Try both naming conventions
                var prefix = $"Location_{i}_";

                var street1 = exposureDoc.GetFieldValue($"{prefix}Street1");
                var city = exposureDoc.GetFieldValue($"{prefix}City");
                var state = exposureDoc.GetFieldValue($"{prefix}State");
                var postalCode = exposureDoc.GetFieldValue($"{prefix}PostalCode");

                var address = TryCreateAddress(street1, null, city, state, postalCode);
                if (address is null) continue;

                var location = submission.AddLocation(address);

                // Update building description
                var description = exposureDoc.GetFieldValue($"{prefix}Description");
                location.UpdateBuildingDescription(description);

                // Update construction
                var constructionType = exposureDoc.GetFieldValue($"{prefix}ConstructionType");
                var yearBuiltStr = exposureDoc.GetFieldValue($"{prefix}YearBuilt");
                int? yearBuilt = int.TryParse(yearBuiltStr, out var yb) ? yb : null;

                location.UpdateConstruction(constructionType, yearBuilt, null);

                // Update square footage
                var sqftStr = exposureDoc.GetFieldValue($"{prefix}SquareFootage");
                if (int.TryParse(sqftStr?.Replace(",", ""), out var sqft))
                {
                    location.UpdateSquareFootage(sqft);
                }

                // Update values
                var buildingValue = TryParseMoney(exposureDoc.GetFieldValue($"{prefix}BuildingValue"));
                var contentsValue = TryParseMoney(exposureDoc.GetFieldValue($"{prefix}ContentsValue"));
                var biValue = TryParseMoney(exposureDoc.GetFieldValue($"{prefix}BusinessIncomeValue"));

                location.UpdateValues(buildingValue, contentsValue, biValue);
            }
        }

        logger.LogDebug(
            "Added {LocationCount} locations from {DocumentCount} exposure schedule documents",
            submission.Locations.Count,
            exposureDocuments.Count);
    }

    private static string GenerateSubmissionNumber()
    {
        // Format: SUB-YYYYMMDD-XXXX (random 4 digits)
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomPart = Random.Shared.Next(1000, 9999);
        return $"SUB-{datePart}-{randomPart}";
    }

    private static Address? TryCreateAddress(
        string? street1,
        string? street2,
        string? city,
        string? state,
        string? postalCode)
    {
        if (string.IsNullOrWhiteSpace(street1) ||
            string.IsNullOrWhiteSpace(city) ||
            string.IsNullOrWhiteSpace(state))
        {
            return null;
        }

        var result = Address.Create(
            street1,
            street2,
            city,
            state,
            postalCode ?? "00000");

        return result.IsSuccess ? result.Value : null;
    }

    private static DateTime? TryParseDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString)) return null;

        // Try common date formats
        string[] formats =
        [
            "yyyy-MM-dd",
            "MM/dd/yyyy",
            "MM-dd-yyyy",
            "M/d/yyyy",
            "MM/dd/yy",
            "yyyy/MM/dd"
        ];

        if (DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        if (DateTime.TryParse(dateString, out date))
        {
            return date;
        }

        return null;
    }

    private static Money? TryParseMoney(string? amountString)
    {
        if (string.IsNullOrWhiteSpace(amountString)) return null;

        // Remove currency symbols and formatting
        var cleaned = amountString
            .Replace("$", "")
            .Replace(",", "")
            .Replace(" ", "")
            .Trim();

        if (decimal.TryParse(cleaned, out var amount))
        {
            return Money.FromDecimal(amount);
        }

        return null;
    }

    private static CoverageType? TryParseCoverageType(string? coverageTypeString)
    {
        if (string.IsNullOrWhiteSpace(coverageTypeString)) return null;

        var normalized = coverageTypeString.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "");

        return normalized switch
        {
            "generalliability" or "gl" => CoverageType.GeneralLiability,
            "property" or "propertydamage" => CoverageType.PropertyDamage,
            "workerscomp" or "workerscompensation" or "wc" => CoverageType.WorkersCompensation,
            "auto" or "automobile" => CoverageType.Auto,
            "umbrella" => CoverageType.Umbrella,
            "productscompleted" or "productsliability" => CoverageType.ProductsCompleted,
            "professionalliability" or "eo" => CoverageType.ProfessionalLiability,
            "cyber" or "cyberliability" => CoverageType.Cyber,
            _ => null
        };
    }

    private static LossStatus TryParseLossStatus(string? statusString)
    {
        if (string.IsNullOrWhiteSpace(statusString)) return LossStatus.Open;

        var normalized = statusString.ToLowerInvariant().Trim();

        return normalized switch
        {
            "closed" or "close" => LossStatus.Closed,
            "closedwithpayment" or "closed with payment" or "paid" => LossStatus.ClosedWithPayment,
            "closedwithoutpayment" or "closed without payment" or "denied" => LossStatus.ClosedWithoutPayment,
            "reopened" or "reopen" => LossStatus.Reopened,
            _ => LossStatus.Open
        };
    }
}

public static class SubmissionCreationErrors
{
    public static readonly Error ProcessingJobNotCompleted = new(
        "SubmissionCreation.ProcessingJobNotCompleted",
        "The processing job must be completed before creating a submission.");

    public static readonly Error NoAcordFormFound = new(
        "SubmissionCreation.NoAcordFormFound",
        "No ACORD form was found in the processed documents.");

    public static readonly Error InsuredNameNotExtracted = new(
        "SubmissionCreation.InsuredNameNotExtracted",
        "Insured name could not be extracted from the documents.");
}
