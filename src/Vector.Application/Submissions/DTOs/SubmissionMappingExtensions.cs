using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Entities;
using Vector.Domain.Submission.ValueObjects;

namespace Vector.Application.Submissions.DTOs;

public static class SubmissionMappingExtensions
{
    public static SubmissionDto ToDto(this Submission submission)
    {
        return new SubmissionDto(
            submission.Id,
            submission.TenantId,
            submission.SubmissionNumber,
            submission.Insured.ToDto(),
            submission.Status.ToString(),
            submission.ReceivedAt,
            submission.EffectiveDate,
            submission.ExpirationDate,
            submission.ProducerName,
            submission.AssignedUnderwriterName,
            submission.AssignedAt,
            submission.AppetiteScore,
            submission.WinnabilityScore,
            submission.DataQualityScore,
            submission.Coverages.Select(c => c.ToDto()).ToList(),
            submission.Locations.Select(l => l.ToDto()).ToList(),
            submission.LossCount);
    }

    public static InsuredPartyDto ToDto(this InsuredParty insured)
    {
        return new InsuredPartyDto(
            insured.Id,
            insured.Name,
            insured.DbaName,
            insured.FeinNumber,
            insured.MailingAddress?.ToDto(),
            insured.Industry?.ToString(),
            insured.Website,
            insured.YearsInBusiness,
            insured.EmployeeCount,
            insured.AnnualRevenue?.Amount);
    }

    public static AddressDto ToDto(this Address address)
    {
        return new AddressDto(
            address.Street1,
            address.Street2,
            address.City,
            address.State,
            address.PostalCode,
            address.Country);
    }

    public static CoverageDto ToDto(this Coverage coverage)
    {
        return new CoverageDto(
            coverage.Id,
            coverage.Type.ToString(),
            coverage.RequestedLimit?.Amount,
            coverage.RequestedDeductible?.Amount,
            coverage.EffectiveDate,
            coverage.ExpirationDate,
            coverage.IsCurrentlyInsured,
            coverage.CurrentCarrier,
            coverage.CurrentPremium?.Amount);
    }

    public static ExposureLocationDto ToDto(this ExposureLocation location)
    {
        return new ExposureLocationDto(
            location.Id,
            location.LocationNumber,
            location.Address.ToDto(),
            location.BuildingDescription,
            location.OccupancyType,
            location.ConstructionType,
            location.YearBuilt,
            location.SquareFootage,
            location.BuildingValue?.Amount,
            location.ContentsValue?.Amount,
            location.BusinessIncomeValue?.Amount,
            location.TotalInsuredValue.Amount);
    }

    public static LossHistoryDto ToDto(this LossHistory loss)
    {
        return new LossHistoryDto(
            loss.Id,
            loss.DateOfLoss,
            loss.CoverageType?.ToString(),
            loss.ClaimNumber,
            loss.Description,
            loss.PaidAmount?.Amount,
            loss.ReservedAmount?.Amount,
            loss.IncurredAmount?.Amount,
            loss.Status.ToString(),
            loss.Carrier);
    }
}
