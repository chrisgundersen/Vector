using Microsoft.Extensions.Logging;
using Vector.Application.Common.Interfaces;
using Vector.Domain.DocumentProcessing.Enums;

namespace Vector.Infrastructure.DocumentAI;

/// <summary>
/// Mock document intelligence service for development/testing.
/// </summary>
public class MockDocumentIntelligenceService(
    ILogger<MockDocumentIntelligenceService> logger) : IDocumentIntelligenceService
{
    public Task<DocumentClassificationResult> ClassifyDocumentAsync(
        Stream documentStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Mock: Classifying document {FileName}", fileName);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var nameLower = fileName.ToLowerInvariant();

        DocumentType documentType;
        decimal confidence;

        if (nameLower.Contains("acord125") || nameLower.Contains("acord 125"))
        {
            documentType = DocumentType.Acord125;
            confidence = 0.95m;
        }
        else if (nameLower.Contains("acord126") || nameLower.Contains("acord 126"))
        {
            documentType = DocumentType.Acord126;
            confidence = 0.93m;
        }
        else if (nameLower.Contains("lossrun") || nameLower.Contains("loss run") || nameLower.Contains("loss_run"))
        {
            documentType = DocumentType.LossRunReport;
            confidence = 0.88m;
        }
        else if (nameLower.Contains("sov") || nameLower.Contains("schedule of values") || nameLower.Contains("exposure"))
        {
            documentType = DocumentType.ExposureSchedule;
            confidence = 0.85m;
        }
        else if (extension == ".pdf")
        {
            documentType = DocumentType.Acord125;
            confidence = 0.72m;
        }
        else if (extension is ".xlsx" or ".xls")
        {
            documentType = DocumentType.ExposureSchedule;
            confidence = 0.68m;
        }
        else
        {
            documentType = DocumentType.Unknown;
            confidence = 0.30m;
        }

        return Task.FromResult(new DocumentClassificationResult(documentType, confidence, "mock-1.0"));
    }

    public Task<DocumentExtractionResult> ExtractAcordFormAsync(
        Stream documentStream,
        DocumentType documentType,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Mock: Extracting ACORD form type {DocumentType}", documentType);

        var fields = new Dictionary<string, ExtractedFieldResult>
        {
            ["InsuredName"] = new("ABC Manufacturing Corp", 0.98m, null, 1),
            ["InsuredDBA"] = new("ABC Mfg", 0.85m, null, 1),
            ["InsuredAddress"] = new("123 Industrial Way", 0.96m, null, 1),
            ["InsuredCity"] = new("Chicago", 0.97m, null, 1),
            ["InsuredState"] = new("IL", 0.99m, null, 1),
            ["InsuredZip"] = new("60601", 0.98m, null, 1),
            ["FEIN"] = new("12-3456789", 0.92m, null, 1),
            ["BusinessDescription"] = new("Metal fabrication and assembly", 0.88m, null, 1),
            ["NAICSCode"] = new("332710", 0.90m, null, 1),
            ["EffectiveDate"] = new("04/01/2024", 0.95m, null, 1),
            ["ExpirationDate"] = new("04/01/2025", 0.95m, null, 1),
            ["GLLimit"] = new("1,000,000", 0.93m, null, 2),
            ["GLDeductible"] = new("5,000", 0.91m, null, 2),
            ["PropertyLimit"] = new("2,500,000", 0.92m, null, 2),
            ["PropertyDeductible"] = new("10,000", 0.90m, null, 2),
            ["YearsInBusiness"] = new("15", 0.85m, null, 1),
            ["EmployeeCount"] = new("45", 0.88m, null, 1),
            ["AnnualRevenue"] = new("8,500,000", 0.86m, null, 1)
        };

        return Task.FromResult(new DocumentExtractionResult(fields, 0.91m, 4));
    }

    public Task<LossRunExtractionResult> ExtractLossRunAsync(
        Stream documentStream,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Mock: Extracting loss run data");

        var losses = new List<ExtractedLoss>
        {
            new(
                DateOfLoss: DateTime.Parse("2022-03-15"),
                ClaimNumber: "CLM-2022-001",
                Description: "Slip and fall injury on premises",
                PaidAmount: 15000m,
                ReservedAmount: 5000m,
                IncurredAmount: 20000m,
                Status: "Closed",
                CoverageType: "GeneralLiability"),
            new(
                DateOfLoss: DateTime.Parse("2021-08-22"),
                ClaimNumber: "CLM-2021-003",
                Description: "Property damage from equipment malfunction",
                PaidAmount: 8500m,
                ReservedAmount: 0m,
                IncurredAmount: 8500m,
                Status: "Closed",
                CoverageType: "PropertyDamage"),
            new(
                DateOfLoss: DateTime.Parse("2023-01-10"),
                ClaimNumber: "CLM-2023-001",
                Description: "Workers compensation - back injury",
                PaidAmount: 12000m,
                ReservedAmount: 8000m,
                IncurredAmount: 20000m,
                Status: "Open",
                CoverageType: "WorkersCompensation")
        };

        return Task.FromResult(new LossRunExtractionResult(
            losses,
            "Current Carrier Insurance Co",
            DateTime.UtcNow.AddDays(-5),
            0.87m));
    }

    public Task<ExposureScheduleExtractionResult> ExtractExposureScheduleAsync(
        Stream documentStream,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Mock: Extracting exposure schedule data");

        var locations = new List<ExtractedLocation>
        {
            new(
                LocationNumber: 1,
                Street1: "123 Industrial Way",
                Street2: null,
                City: "Chicago",
                State: "IL",
                PostalCode: "60601",
                BuildingDescription: "Main manufacturing facility - steel frame construction",
                BuildingValue: 2500000m,
                ContentsValue: 1500000m,
                BusinessIncomeValue: 500000m,
                ConstructionType: "Steel Frame",
                YearBuilt: 1995,
                SquareFootage: 45000),
            new(
                LocationNumber: 2,
                Street1: "456 Warehouse Blvd",
                Street2: "Suite A",
                City: "Chicago",
                State: "IL",
                PostalCode: "60605",
                BuildingDescription: "Warehouse and distribution center",
                BuildingValue: 800000m,
                ContentsValue: 600000m,
                BusinessIncomeValue: 200000m,
                ConstructionType: "Masonry",
                YearBuilt: 2005,
                SquareFootage: 25000)
        };

        return Task.FromResult(new ExposureScheduleExtractionResult(locations, 0.85m));
    }
}
