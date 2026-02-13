namespace Vector.Application.Submissions.DTOs;

public sealed record SubmissionDto(
    Guid Id,
    Guid TenantId,
    string SubmissionNumber,
    InsuredPartyDto Insured,
    string Status,
    DateTime ReceivedAt,
    DateTime? EffectiveDate,
    DateTime? ExpirationDate,
    string? ProducerName,
    string? AssignedUnderwriterName,
    DateTime? AssignedAt,
    int? AppetiteScore,
    int? WinnabilityScore,
    int? DataQualityScore,
    IReadOnlyList<CoverageDto> Coverages,
    IReadOnlyList<ExposureLocationDto> Locations,
    int LossCount);

public sealed record InsuredPartyDto(
    Guid Id,
    string Name,
    string? DbaName,
    string? FeinNumber,
    AddressDto? MailingAddress,
    string? Industry,
    string? Website,
    int? YearsInBusiness,
    int? EmployeeCount,
    decimal? AnnualRevenue);

public sealed record AddressDto(
    string Street1,
    string? Street2,
    string City,
    string State,
    string PostalCode,
    string Country);

public sealed record CoverageDto(
    Guid Id,
    string Type,
    decimal? RequestedLimit,
    decimal? RequestedDeductible,
    DateTime? EffectiveDate,
    DateTime? ExpirationDate,
    bool IsCurrentlyInsured,
    string? CurrentCarrier,
    decimal? CurrentPremium);

public sealed record ExposureLocationDto(
    Guid Id,
    int LocationNumber,
    AddressDto Address,
    string? BuildingDescription,
    string? OccupancyType,
    string? ConstructionType,
    int? YearBuilt,
    int? SquareFootage,
    decimal? BuildingValue,
    decimal? ContentsValue,
    decimal? BusinessIncomeValue,
    decimal TotalInsuredValue);

public sealed record LossHistoryDto(
    Guid Id,
    DateTime DateOfLoss,
    string? CoverageType,
    string? ClaimNumber,
    string Description,
    decimal? PaidAmount,
    decimal? ReservedAmount,
    decimal? IncurredAmount,
    string Status,
    string? Carrier);
