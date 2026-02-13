namespace Vector.Domain.Submission.Enums;

/// <summary>
/// Type of data correction being requested.
/// </summary>
public enum DataCorrectionType
{
    InsuredInformation = 0,
    CoverageDetails = 1,
    LocationData = 2,
    LossHistory = 3,
    PolicyDates = 4,
    AdditionalDocuments = 5,
    Other = 99
}
