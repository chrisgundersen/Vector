using Bogus;
using Vector.Domain.Submission.Enums;

namespace Vector.EndToEnd.Tests.TestData.Generators;

/// <summary>
/// Generates realistic insurance submission test data.
/// </summary>
public class SubmissionGenerator
{
    private readonly Faker _faker;
    private readonly Random _random;
    private int _submissionCounter;

    public SubmissionGenerator(int seed = 12345)
    {
        _faker = new Faker();
        _faker.Random = new Randomizer(seed);
        _random = new Random(seed);
        _submissionCounter = 0;
    }

    /// <summary>
    /// Generates a batch of realistic submission scenarios.
    /// </summary>
    public List<SubmissionScenario> GenerateSubmissions(int count)
    {
        var scenarios = new List<SubmissionScenario>();

        for (int i = 0; i < count; i++)
        {
            var scenario = GenerateSubmissionScenario(i);
            scenarios.Add(scenario);
        }

        return scenarios;
    }

    private SubmissionScenario GenerateSubmissionScenario(int index)
    {
        _submissionCounter++;
        var submissionNumber = $"SUB-{DateTime.Now.Year}-{_submissionCounter:D6}";

        // Determine scenario characteristics
        var scenarioType = DetermineScenarioType(index);
        var producer = SelectProducer(scenarioType);
        var industry = SelectIndustry(scenarioType);
        var coverageTypes = SelectCoverageTypes(scenarioType);
        var locationCount = DetermineLocationCount(scenarioType);
        var hasLossHistory = _random.NextDouble() > 0.3;
        var dataQuality = DetermineDataQuality();

        // Generate insured information
        var insured = GenerateInsured(industry);
        var locations = GenerateLocations(locationCount, industry, insured.State);
        var coverages = GenerateCoverages(coverageTypes, locations);
        var losses = hasLossHistory ? GenerateLossHistory(coverageTypes) : [];

        // Generate email content
        var email = GenerateEmail(producer, insured, coverages, locations);

        // Generate attachments
        var attachments = GenerateAttachments(coverages);

        // Determine expected routing
        var expectedRouting = DetermineExpectedRouting(scenarioType, producer, industry);

        return new SubmissionScenario
        {
            SubmissionNumber = submissionNumber,
            ScenarioType = scenarioType,
            DataQuality = dataQuality,
            Producer = producer,
            Industry = industry,
            Insured = insured,
            Coverages = coverages,
            Locations = locations,
            Losses = losses,
            Email = email,
            Attachments = attachments,
            ExpectedRouting = expectedRouting,
            ExpectedStatus = SubmissionStatus.InReview,
            ValidationExpectations = GenerateValidationExpectations(dataQuality)
        };
    }

    private ScenarioType DetermineScenarioType(int index)
    {
        return (index % 10) switch
        {
            0 => ScenarioType.HighValueManufacturing,
            1 => ScenarioType.SmallRetail,
            2 => ScenarioType.MultiLocationProperty,
            3 => ScenarioType.ProfessionalServices,
            4 => ScenarioType.HighHazardConstruction,
            5 => ScenarioType.RestaurantHospitality,
            6 => ScenarioType.TechnologyStartup,
            7 => ScenarioType.HealthcareServices,
            8 => ScenarioType.TransportationLogistics,
            9 => ScenarioType.LargeCommercial,
            _ => ScenarioType.SmallRetail
        };
    }

    private SubmissionProducer SelectProducer(ScenarioType scenarioType)
    {
        var producers = RealisticTestData.Producers;
        var profile = producers[_random.Next(producers.Length)];
        return new SubmissionProducer
        {
            Code = profile.Code,
            Name = profile.Name,
            ContactName = profile.ContactName,
            Email = profile.Email,
            Phone = "(555) 123-4567",
            Type = profile.Type,
            State = profile.State
        };
    }

    private RealisticTestData.IndustryProfile SelectIndustry(ScenarioType scenarioType)
    {
        var industries = RealisticTestData.Industries;
        return industries[_random.Next(industries.Length)];
    }

    private List<CoverageType> SelectCoverageTypes(ScenarioType scenarioType)
    {
        var coverages = new List<CoverageType>();

        switch (scenarioType)
        {
            case ScenarioType.HighValueManufacturing:
                coverages.AddRange([CoverageType.PropertyDamage, CoverageType.GeneralLiability, CoverageType.ProductsCompleted]);
                break;
            case ScenarioType.SmallRetail:
                coverages.AddRange([CoverageType.PropertyDamage, CoverageType.GeneralLiability]);
                break;
            case ScenarioType.MultiLocationProperty:
                coverages.Add(CoverageType.PropertyDamage);
                break;
            case ScenarioType.ProfessionalServices:
                coverages.AddRange([CoverageType.ProfessionalLiability, CoverageType.GeneralLiability, CoverageType.Cyber]);
                break;
            case ScenarioType.HighHazardConstruction:
                coverages.AddRange([CoverageType.GeneralLiability, CoverageType.BuildersRisk, CoverageType.Auto]);
                break;
            case ScenarioType.RestaurantHospitality:
                coverages.AddRange([CoverageType.PropertyDamage, CoverageType.GeneralLiability]);
                break;
            case ScenarioType.TechnologyStartup:
                coverages.AddRange([CoverageType.Cyber, CoverageType.ProfessionalLiability, CoverageType.DirectorsOfficers]);
                break;
            case ScenarioType.HealthcareServices:
                coverages.AddRange([CoverageType.ProfessionalLiability, CoverageType.GeneralLiability, CoverageType.Cyber]);
                break;
            case ScenarioType.TransportationLogistics:
                coverages.AddRange([CoverageType.Auto, CoverageType.GeneralLiability, CoverageType.InlandMarine]);
                break;
            case ScenarioType.LargeCommercial:
                coverages.AddRange([CoverageType.PropertyDamage, CoverageType.GeneralLiability, CoverageType.Umbrella]);
                break;
        }

        return coverages.Distinct().ToList();
    }

    private int DetermineLocationCount(ScenarioType scenarioType)
    {
        return scenarioType switch
        {
            ScenarioType.MultiLocationProperty => _random.Next(5, 50),
            ScenarioType.LargeCommercial => _random.Next(3, 20),
            ScenarioType.SmallRetail => _random.Next(1, 3),
            _ => _random.Next(1, 5)
        };
    }

    private DataQualityLevel DetermineDataQuality()
    {
        var roll = _random.NextDouble();
        return roll switch
        {
            < 0.1 => DataQualityLevel.Poor,
            < 0.3 => DataQualityLevel.Fair,
            < 0.7 => DataQualityLevel.Good,
            _ => DataQualityLevel.Excellent
        };
    }

    private SubmissionInsured GenerateInsured(RealisticTestData.IndustryProfile industry)
    {
        var prefixes = RealisticTestData.CompanyPrefixes;
        var industryTerms = RealisticTestData.IndustryTerms;
        var suffixes = RealisticTestData.CompanySuffixes;
        var states = RealisticTestData.States;

        var prefix = prefixes[_random.Next(prefixes.Length)];
        var industryTerm = industryTerms[_random.Next(industryTerms.Length)];
        var suffix = suffixes[_random.Next(suffixes.Length)];
        var state = states[_random.Next(states.Length)];

        var companyName = $"{prefix} {industryTerm} {suffix}";

        return new SubmissionInsured
        {
            Name = companyName,
            DbaName = _random.NextDouble() > 0.7 ? $"{prefix} {industryTerm}" : null,
            Fein = $"{_random.Next(10, 99)}-{_random.Next(1000000, 9999999)}",
            Street = $"{_random.Next(100, 9999)} Main Street",
            City = "Anytown",
            State = state.Code,
            ZipCode = $"{_random.Next(10000, 99999)}",
            Website = $"www.{prefix.ToLower().Replace(" ", "")}{industryTerm.ToLower().Replace(" ", "")}.com",
            NaicsCode = industry.NaicsCode,
            SicCode = $"{_random.Next(1000, 9999)}",
            IndustryDescription = industry.Description,
            YearsInBusiness = _random.Next(1, 75),
            EmployeeCount = _random.Next(5, 5000),
            AnnualRevenue = _random.Next(500000, 100000000)
        };
    }

    private List<SubmissionLocation> GenerateLocations(int count, RealisticTestData.IndustryProfile industry, string primaryState)
    {
        var locations = new List<SubmissionLocation>();
        var occupancies = RealisticTestData.OccupancyTypes;
        var constructions = RealisticTestData.ConstructionTypes;

        for (int i = 0; i < count; i++)
        {
            var sqft = _random.Next(5000, 200000);
            var buildingValue = sqft * _random.Next(80, 250);
            var contentsValue = (decimal)(buildingValue * (_random.NextDouble() * 0.5 + 0.1));
            var biValue = (decimal)(buildingValue * (_random.NextDouble() * 0.3 + 0.05));

            locations.Add(new SubmissionLocation
            {
                LocationNumber = i + 1,
                Street = $"{_random.Next(100, 9999)} Business Drive",
                City = "Commerce City",
                State = primaryState,
                ZipCode = $"{_random.Next(10000, 99999)}",
                OccupancyType = occupancies[_random.Next(occupancies.Length)],
                ConstructionType = constructions[_random.Next(constructions.Length)],
                YearBuilt = _random.Next(1950, 2024),
                SquareFootage = sqft,
                NumberOfStories = _random.Next(1, 10),
                BuildingValue = buildingValue,
                ContentsValue = contentsValue,
                BusinessIncomeValue = biValue,
                HasSprinklers = _random.NextDouble() > 0.3,
                HasFireAlarm = _random.NextDouble() > 0.2,
                HasSecuritySystem = _random.NextDouble() > 0.4,
                ProtectionClass = _random.Next(1, 10).ToString()
            });
        }

        return locations;
    }

    private List<SubmissionCoverage> GenerateCoverages(List<CoverageType> coverageTypes, List<SubmissionLocation> locations)
    {
        var totalTiv = locations.Sum(l => l.BuildingValue + l.ContentsValue + l.BusinessIncomeValue);
        var effectiveDate = DateTime.Today.AddDays(_random.Next(7, 90));

        return coverageTypes.Select(ct => new SubmissionCoverage
        {
            CoverageType = ct,
            RequestedLimit = GetLimitForCoverage(ct, totalTiv),
            RequestedDeductible = GetDeductibleForCoverage(ct),
            EffectiveDate = effectiveDate,
            ExpirationDate = effectiveDate.AddYears(1),
            IsCurrentlyInsured = _random.NextDouble() > 0.2,
            CurrentCarrier = _random.NextDouble() > 0.2 ? RealisticTestData.InsuranceCarriers[_random.Next(RealisticTestData.InsuranceCarriers.Length)] : null,
            CurrentPremium = _random.NextDouble() > 0.2 ? _random.Next(5000, 500000) : null
        }).ToList();
    }

    private decimal GetLimitForCoverage(CoverageType coverageType, decimal totalTiv)
    {
        return coverageType switch
        {
            CoverageType.PropertyDamage => totalTiv,
            CoverageType.GeneralLiability => 1_000_000m,
            CoverageType.Umbrella or CoverageType.ExcessLiability => 10_000_000m,
            CoverageType.ProfessionalLiability => 2_000_000m,
            CoverageType.Cyber => 5_000_000m,
            CoverageType.DirectorsOfficers => 5_000_000m,
            CoverageType.WorkersCompensation => 1_000_000m,
            CoverageType.Auto => 1_000_000m,
            _ => 1_000_000m
        };
    }

    private decimal GetDeductibleForCoverage(CoverageType coverageType)
    {
        return coverageType switch
        {
            CoverageType.PropertyDamage => 25_000m,
            CoverageType.GeneralLiability => 5_000m,
            CoverageType.Cyber => 50_000m,
            CoverageType.ProfessionalLiability => 10_000m,
            _ => 5_000m
        };
    }

    private List<SubmissionLoss> GenerateLossHistory(List<CoverageType> coverageTypes)
    {
        var lossCount = _random.Next(0, 5);
        var losses = new List<SubmissionLoss>();

        for (int i = 0; i < lossCount; i++)
        {
            var yearsAgo = _random.Next(0, 5);
            var dateOfLoss = DateTime.Today.AddYears(-yearsAgo).AddDays(-_random.Next(1, 365));
            var coverageType = coverageTypes[_random.Next(coverageTypes.Count)];
            var paidAmount = _random.Next(1000, 500000);
            var reservedAmount = _random.NextDouble() > 0.7 ? _random.Next(1000, 100000) : 0;

            losses.Add(new SubmissionLoss
            {
                DateOfLoss = dateOfLoss,
                CoverageType = coverageType,
                ClaimNumber = $"CLM-{dateOfLoss.Year}-{_random.Next(10000, 99999)}",
                Description = "Insurance claim",
                PaidAmount = paidAmount,
                ReservedAmount = reservedAmount,
                IncurredAmount = paidAmount + reservedAmount,
                Status = reservedAmount > 0 ? "Open" : "Closed",
                Carrier = RealisticTestData.InsuranceCarriers[_random.Next(RealisticTestData.InsuranceCarriers.Length)]
            });
        }

        return losses.OrderByDescending(l => l.DateOfLoss).ToList();
    }

    private SubmissionEmail GenerateEmail(
        SubmissionProducer producer,
        SubmissionInsured insured,
        List<SubmissionCoverage> coverages,
        List<SubmissionLocation> locations)
    {
        var coverageTypeNames = string.Join(", ", coverages.Select(c => c.CoverageType.ToString()));
        var effectiveDate = coverages.First().EffectiveDate;
        var totalTiv = locations.Sum(l => l.BuildingValue + l.ContentsValue + l.BusinessIncomeValue);

        var subject = $"New Submission - {insured.Name}";
        var body = $@"Dear Underwriting Team,

Please find attached the submission documents for our client:

Insured: {insured.Name}
Coverage Requested: {coverageTypeNames}
Effective Date: {effectiveDate:MM/dd/yyyy}
Total Insured Value: ${totalTiv:N0}
Locations: {locations.Count}

Attached Documents:
- ACORD 125 (Commercial Insurance Application)
- Statement of Values
- Loss Run Report

Please let me know if you need any additional information.

Best regards,
{producer.ContactName}
{producer.Name}
{producer.Email}";

        return new SubmissionEmail
        {
            FromAddress = producer.Email,
            FromName = producer.ContactName,
            Subject = subject,
            Body = body,
            ReceivedAt = DateTime.UtcNow.AddMinutes(-_random.Next(1, 1440))
        };
    }

    private List<SubmissionAttachment> GenerateAttachments(List<SubmissionCoverage> coverages)
    {
        var attachments = new List<SubmissionAttachment>
        {
            new()
            {
                FileName = "ACORD125_Application.pdf",
                ContentType = "application/pdf",
                DocumentType = DocumentType.Acord125,
                Size = _random.Next(50000, 200000)
            },
            new()
            {
                FileName = "Statement_of_Values.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                DocumentType = DocumentType.SOV,
                Size = _random.Next(20000, 100000)
            },
            new()
            {
                FileName = "LossRuns_5Year.pdf",
                ContentType = "application/pdf",
                DocumentType = DocumentType.LossRun,
                Size = _random.Next(20000, 200000)
            }
        };

        if (coverages.Any(c => c.CoverageType == CoverageType.GeneralLiability))
        {
            attachments.Add(new SubmissionAttachment
            {
                FileName = "ACORD126_GL_Section.pdf",
                ContentType = "application/pdf",
                DocumentType = DocumentType.Acord126,
                Size = _random.Next(30000, 100000)
            });
        }

        return attachments;
    }

    private ExpectedRouting DetermineExpectedRouting(
        ScenarioType scenarioType,
        SubmissionProducer producer,
        RealisticTestData.IndustryProfile industry)
    {
        var underwriter = scenarioType switch
        {
            ScenarioType.HighValueManufacturing => RealisticTestData.Underwriters[0],
            ScenarioType.LargeCommercial => RealisticTestData.Underwriters[0],
            ScenarioType.ProfessionalServices => RealisticTestData.Underwriters[2],
            ScenarioType.TechnologyStartup => RealisticTestData.Underwriters[2],
            ScenarioType.HealthcareServices => RealisticTestData.Underwriters[2],
            ScenarioType.HighHazardConstruction => RealisticTestData.Underwriters[1],
            _ => RealisticTestData.Underwriters[5]
        };

        return new ExpectedRouting
        {
            ExpectedUnderwriterCode = underwriter.Code,
            ExpectedUnderwriterName = underwriter.Name,
            RoutingReason = $"Routed based on {scenarioType} scenario type",
            ShouldBeDeclined = false,
            DeclineReason = null
        };
    }

    private List<ValidationExpectation> GenerateValidationExpectations(DataQualityLevel dataQuality)
    {
        return
        [
            new() { Field = "InsuredName", ShouldBeExtracted = true },
            new() { Field = "EffectiveDate", ShouldBeExtracted = true },
            new() { Field = "ProducerName", ShouldBeExtracted = true }
        ];
    }
}

#region Models

public enum ScenarioType
{
    HighValueManufacturing,
    SmallRetail,
    MultiLocationProperty,
    ProfessionalServices,
    HighHazardConstruction,
    RestaurantHospitality,
    TechnologyStartup,
    HealthcareServices,
    TransportationLogistics,
    LargeCommercial
}

public enum DataQualityLevel
{
    Poor,
    Fair,
    Good,
    Excellent
}

public enum DocumentType
{
    Acord125,
    Acord126,
    Acord127,
    Acord130,
    Acord140,
    SOV,
    LossRun,
    Supplement,
    Other
}

public class SubmissionScenario
{
    public string SubmissionNumber { get; set; } = null!;
    public ScenarioType ScenarioType { get; set; }
    public DataQualityLevel DataQuality { get; set; }
    public SubmissionProducer Producer { get; set; } = null!;
    public RealisticTestData.IndustryProfile Industry { get; set; } = null!;
    public SubmissionInsured Insured { get; set; } = null!;
    public List<SubmissionCoverage> Coverages { get; set; } = [];
    public List<SubmissionLocation> Locations { get; set; } = [];
    public List<SubmissionLoss> Losses { get; set; } = [];
    public SubmissionEmail Email { get; set; } = null!;
    public List<SubmissionAttachment> Attachments { get; set; } = [];
    public ExpectedRouting ExpectedRouting { get; set; } = null!;
    public SubmissionStatus ExpectedStatus { get; set; }
    public List<ValidationExpectation> ValidationExpectations { get; set; } = [];
    public Guid CreatedSubmissionId { get; set; }
}

public class SubmissionProducer
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string ContactName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string State { get; set; } = null!;
}

public class SubmissionInsured
{
    public string Name { get; set; } = null!;
    public string? DbaName { get; set; }
    public string Fein { get; set; } = null!;
    public string Street { get; set; } = null!;
    public string City { get; set; } = null!;
    public string State { get; set; } = null!;
    public string ZipCode { get; set; } = null!;
    public string? Website { get; set; }
    public string NaicsCode { get; set; } = null!;
    public string SicCode { get; set; } = null!;
    public string IndustryDescription { get; set; } = null!;
    public int YearsInBusiness { get; set; }
    public int EmployeeCount { get; set; }
    public decimal AnnualRevenue { get; set; }
}

public class SubmissionCoverage
{
    public CoverageType CoverageType { get; set; }
    public decimal RequestedLimit { get; set; }
    public decimal RequestedDeductible { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public bool IsCurrentlyInsured { get; set; }
    public string? CurrentCarrier { get; set; }
    public decimal? CurrentPremium { get; set; }
}

public class SubmissionLocation
{
    public int LocationNumber { get; set; }
    public string Street { get; set; } = null!;
    public string City { get; set; } = null!;
    public string State { get; set; } = null!;
    public string ZipCode { get; set; } = null!;
    public string OccupancyType { get; set; } = null!;
    public string ConstructionType { get; set; } = null!;
    public int YearBuilt { get; set; }
    public int SquareFootage { get; set; }
    public int NumberOfStories { get; set; }
    public decimal BuildingValue { get; set; }
    public decimal ContentsValue { get; set; }
    public decimal BusinessIncomeValue { get; set; }
    public bool HasSprinklers { get; set; }
    public bool HasFireAlarm { get; set; }
    public bool HasSecuritySystem { get; set; }
    public string ProtectionClass { get; set; } = null!;
}

public class SubmissionLoss
{
    public DateTime DateOfLoss { get; set; }
    public CoverageType CoverageType { get; set; }
    public string ClaimNumber { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal PaidAmount { get; set; }
    public decimal ReservedAmount { get; set; }
    public decimal IncurredAmount { get; set; }
    public string Status { get; set; } = null!;
    public string Carrier { get; set; } = null!;
}

public class SubmissionEmail
{
    public string FromAddress { get; set; } = null!;
    public string FromName { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public DateTime ReceivedAt { get; set; }
    public string? MessageId { get; set; }
}

public class SubmissionAttachment
{
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public DocumentType DocumentType { get; set; }
    public int Size { get; set; }
    public int? LocationCount { get; set; }
}

public class ExpectedRouting
{
    public string ExpectedUnderwriterCode { get; set; } = null!;
    public string ExpectedUnderwriterName { get; set; } = null!;
    public string RoutingReason { get; set; } = null!;
    public bool ShouldBeDeclined { get; set; }
    public string? DeclineReason { get; set; }
}

public class ValidationExpectation
{
    public string Field { get; set; } = null!;
    public bool ShouldBeExtracted { get; set; }
    public string? ExpectedValue { get; set; }
}

#endregion
