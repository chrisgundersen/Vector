namespace Vector.Domain.DocumentProcessing.Enums;

/// <summary>
/// Status of document processing workflow.
/// </summary>
public enum ProcessingStatus
{
    Pending = 0,
    Classifying = 1,
    Classified = 2,
    Extracting = 3,
    Extracted = 4,
    Validating = 5,
    Completed = 6,
    Failed = 7,
    ManualReviewRequired = 8
}
