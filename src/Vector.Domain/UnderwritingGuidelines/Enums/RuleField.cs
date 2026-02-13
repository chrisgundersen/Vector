namespace Vector.Domain.UnderwritingGuidelines.Enums;

/// <summary>
/// Fields that can be evaluated in rules.
/// </summary>
public enum RuleField
{
    // Insured Information
    InsuredName = 100,
    InsuredState = 101,
    InsuredCity = 102,
    InsuredZipCode = 103,
    YearsInBusiness = 104,
    EmployeeCount = 105,
    AnnualRevenue = 106,

    // Industry Classification
    NAICSCode = 200,
    SICCode = 201,
    BusinessDescription = 202,

    // Coverage Information
    CoverageType = 300,
    RequestedLimit = 301,
    RequestedDeductible = 302,

    // Loss History
    TotalLossCount = 400,
    TotalIncurredAmount = 401,
    OpenClaimsCount = 402,
    LargestClaimAmount = 403,
    ClaimFrequency = 404,

    // Property Information
    LocationCount = 500,
    TotalInsuredValue = 501,
    BuildingAge = 502,
    ConstructionType = 503,

    // Submission Metadata
    SubmissionSource = 600,
    ProducerId = 601,
    PriorCarrier = 602
}
