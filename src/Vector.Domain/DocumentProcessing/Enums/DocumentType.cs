namespace Vector.Domain.DocumentProcessing.Enums;

/// <summary>
/// Types of documents that can be processed.
/// </summary>
public enum DocumentType
{
    Unknown = 0,
    Acord125 = 1,           // Commercial Insurance Application
    Acord126 = 2,           // Commercial General Liability Section
    Acord130 = 3,           // Workers Compensation Application
    Acord140 = 4,           // Property Section
    Acord127 = 5,           // Business Owners Application
    Acord137 = 6,           // Inland Marine
    LossRunReport = 10,
    ExposureSchedule = 11,  // Statement of Values (SOV)
    PolicyDeclaration = 12,
    ManuscriptForm = 13,
    SupplementalApplication = 14,
    FinancialStatement = 15,
    Certificate = 16,
    Endorsement = 17,
    Other = 99
}
