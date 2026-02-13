using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Entities;
using Vector.Domain.Submission.Enums;
using Vector.Domain.Submission.ValueObjects;

namespace Vector.Domain.UnitTests.Submission;

public class SubmissionTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var submissionNumber = "SUB-2024-000001";
        var insuredName = "ABC Manufacturing Corp";

        // Act
        var result = Domain.Submission.Aggregates.Submission.Create(
            _tenantId,
            submissionNumber,
            insuredName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(_tenantId);
        result.Value.SubmissionNumber.Should().Be(submissionNumber);
        result.Value.Insured.Name.Should().Be(insuredName);
        result.Value.Status.Should().Be(SubmissionStatus.Draft);
    }

    [Fact]
    public void Create_WithEmptyTenantId_ReturnsFailure()
    {
        // Act
        var result = Domain.Submission.Aggregates.Submission.Create(
            Guid.Empty,
            "SUB-2024-000001",
            "Test Insured");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.InvalidTenant");
    }

    [Fact]
    public void Create_WithEmptyInsuredName_ReturnsFailure()
    {
        // Act
        var result = Domain.Submission.Aggregates.Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.InsuredNameRequired");
    }

    [Fact]
    public void MarkAsReceived_FromDraftStatus_RaisesSubmissionCreatedEvent()
    {
        // Arrange
        var submissionResult = Domain.Submission.Aggregates.Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "Test Insured");

        var submission = submissionResult.Value;

        // Act
        submission.MarkAsReceived();

        // Assert
        submission.Status.Should().Be(SubmissionStatus.Received);
        submission.DomainEvents.Should().HaveCount(2); // Created + StatusChanged
    }

    [Fact]
    public void AssignToUnderwriter_FromReceivedStatus_UpdatesStatusAndRaisesEvent()
    {
        // Arrange
        var submissionResult = Domain.Submission.Aggregates.Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "Test Insured");

        var submission = submissionResult.Value;
        submission.MarkAsReceived();
        submission.ClearDomainEvents();

        var underwriterId = Guid.NewGuid();
        var underwriterName = "John Smith";

        // Act
        var result = submission.AssignToUnderwriter(underwriterId, underwriterName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        submission.Status.Should().Be(SubmissionStatus.InReview);
        submission.AssignedUnderwriterId.Should().Be(underwriterId);
        submission.AssignedUnderwriterName.Should().Be(underwriterName);
        submission.AssignedAt.Should().NotBeNull();
    }

    [Fact]
    public void AssignToUnderwriter_WhenDeclined_ReturnsFailure()
    {
        // Arrange
        var submissionResult = Domain.Submission.Aggregates.Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "Test Insured");

        var submission = submissionResult.Value;
        submission.MarkAsReceived();
        submission.Decline("Out of appetite");

        // Act
        var result = submission.AssignToUnderwriter(Guid.NewGuid(), "John Smith");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.CannotAssignClosedSubmission");
    }

    [Fact]
    public void AddCoverage_AddsToCollection()
    {
        // Arrange
        var submissionResult = Domain.Submission.Aggregates.Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "Test Insured");

        var submission = submissionResult.Value;

        // Act
        var coverage = submission.AddCoverage(CoverageType.GeneralLiability);

        // Assert
        submission.Coverages.Should().ContainSingle();
        coverage.Type.Should().Be(CoverageType.GeneralLiability);
    }

    [Fact]
    public void AddLocation_AddsWithCorrectLocationNumber()
    {
        // Arrange
        var submissionResult = Domain.Submission.Aggregates.Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "Test Insured");

        var submission = submissionResult.Value;

        var addressResult = Address.Create("123 Main St", null, "Chicago", "IL", "60601");

        // Act
        var location1 = submission.AddLocation(addressResult.Value);
        var location2 = submission.AddLocation(addressResult.Value);

        // Assert
        submission.Locations.Should().HaveCount(2);
        location1.LocationNumber.Should().Be(1);
        location2.LocationNumber.Should().Be(2);
    }

    [Fact]
    public void Quote_FromInReview_UpdatesStatusAndSetsPremium()
    {
        // Arrange
        var submissionResult = Domain.Submission.Aggregates.Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "Test Insured");

        var submission = submissionResult.Value;
        submission.MarkAsReceived();
        submission.AssignToUnderwriter(Guid.NewGuid(), "John Smith");

        var premium = Money.FromDecimal(25000m);

        // Act
        var result = submission.Quote(premium);

        // Assert
        result.IsSuccess.Should().BeTrue();
        submission.Status.Should().Be(SubmissionStatus.Quoted);
        submission.QuotedPremium.Should().Be(premium);
    }

    [Fact]
    public void Bind_WhenQuoted_UpdatesStatusToBound()
    {
        // Arrange
        var submissionResult = Domain.Submission.Aggregates.Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "Test Insured");

        var submission = submissionResult.Value;
        submission.MarkAsReceived();
        submission.AssignToUnderwriter(Guid.NewGuid(), "John Smith");
        submission.Quote(Money.FromDecimal(25000m));

        // Act
        var result = submission.Bind();

        // Assert
        result.IsSuccess.Should().BeTrue();
        submission.Status.Should().Be(SubmissionStatus.Bound);
    }

    [Fact]
    public void Bind_WhenNotQuoted_ReturnsFailure()
    {
        // Arrange
        var submissionResult = Domain.Submission.Aggregates.Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "Test Insured");

        var submission = submissionResult.Value;
        submission.MarkAsReceived();

        // Act
        var result = submission.Bind();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.MustBeQuotedToBind");
    }

    [Fact]
    public void Create_WithEmptySubmissionNumber_ReturnsFailure()
    {
        // Act
        var result = Domain.Submission.Aggregates.Submission.Create(
            _tenantId,
            "",
            "Test Insured");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.SubmissionNumberRequired");
    }

    [Fact]
    public void Create_WithProcessingJobId_SetsProcessingJobId()
    {
        // Arrange
        var processingJobId = Guid.NewGuid();

        // Act
        var result = Domain.Submission.Aggregates.Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "Test Insured",
            processingJobId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ProcessingJobId.Should().Be(processingJobId);
    }

    [Fact]
    public void Create_WithInboundEmailId_SetsInboundEmailId()
    {
        // Arrange
        var inboundEmailId = Guid.NewGuid();

        // Act
        var result = Domain.Submission.Aggregates.Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "Test Insured",
            null,
            inboundEmailId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.InboundEmailId.Should().Be(inboundEmailId);
    }

    [Fact]
    public void MarkAsReceived_WhenNotDraft_DoesNotChangeStatus()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.MarkAsReceived();
        var eventCount = submission.DomainEvents.Count;

        // Act
        submission.MarkAsReceived();

        // Assert
        submission.Status.Should().Be(SubmissionStatus.Received);
        submission.DomainEvents.Should().HaveCount(eventCount);
    }

    [Fact]
    public void RequestInformation_FromInReview_UpdatesStatus()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.MarkAsReceived();
        submission.AssignToUnderwriter(Guid.NewGuid(), "John Smith");

        // Act
        var result = submission.RequestInformation("Need more loss history");

        // Assert
        result.IsSuccess.Should().BeTrue();
        submission.Status.Should().Be(SubmissionStatus.PendingInformation);
    }

    [Fact]
    public void RequestInformation_FromQuoted_ReturnsFailure()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.MarkAsReceived();
        submission.AssignToUnderwriter(Guid.NewGuid(), "John Smith");
        submission.Quote(Money.FromDecimal(10000));

        // Act
        var result = submission.RequestInformation("Need more data");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.InvalidStatusTransition");
    }

    [Fact]
    public void Quote_FromPendingInformation_UpdatesStatus()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.MarkAsReceived();
        submission.AssignToUnderwriter(Guid.NewGuid(), "John Smith");
        submission.RequestInformation("Need more data");

        // Act
        var result = submission.Quote(Money.FromDecimal(15000));

        // Assert
        result.IsSuccess.Should().BeTrue();
        submission.Status.Should().Be(SubmissionStatus.Quoted);
    }

    [Fact]
    public void Quote_FromReceived_ReturnsFailure()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.MarkAsReceived();

        // Act
        var result = submission.Quote(Money.FromDecimal(10000));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.InvalidStatusTransition");
    }

    [Fact]
    public void Decline_FromReceived_UpdatesStatus()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.MarkAsReceived();

        // Act
        var result = submission.Decline("Out of appetite");

        // Assert
        result.IsSuccess.Should().BeTrue();
        submission.Status.Should().Be(SubmissionStatus.Declined);
        submission.DeclineReason.Should().Be("Out of appetite");
    }

    [Fact]
    public void Decline_WhenBound_ReturnsFailure()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.MarkAsReceived();
        submission.AssignToUnderwriter(Guid.NewGuid(), "John Smith");
        submission.Quote(Money.FromDecimal(10000));
        submission.Bind();

        // Act
        var result = submission.Decline("Changed mind");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.InvalidStatusTransition");
    }

    [Fact]
    public void Withdraw_FromInReview_UpdatesStatus()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.MarkAsReceived();
        submission.AssignToUnderwriter(Guid.NewGuid(), "John Smith");

        // Act
        var result = submission.Withdraw("Customer withdrew");

        // Assert
        result.IsSuccess.Should().BeTrue();
        submission.Status.Should().Be(SubmissionStatus.Withdrawn);
    }

    [Fact]
    public void Withdraw_WhenBound_ReturnsFailure()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.MarkAsReceived();
        submission.AssignToUnderwriter(Guid.NewGuid(), "John Smith");
        submission.Quote(Money.FromDecimal(10000));
        submission.Bind();

        // Act
        var result = submission.Withdraw("Customer wants to withdraw");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.InvalidStatusTransition");
    }

    [Fact]
    public void AssignToUnderwriter_WhenBound_ReturnsFailure()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.MarkAsReceived();
        submission.AssignToUnderwriter(Guid.NewGuid(), "John Smith");
        submission.Quote(Money.FromDecimal(10000));
        submission.Bind();

        // Act
        var result = submission.AssignToUnderwriter(Guid.NewGuid(), "Jane Doe");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.CannotAssignClosedSubmission");
    }

    [Fact]
    public void AssignToUnderwriter_WhenWithdrawn_ReturnsFailure()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.MarkAsReceived();
        submission.Withdraw("Customer withdrew");

        // Act
        var result = submission.AssignToUnderwriter(Guid.NewGuid(), "John Smith");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.CannotAssignClosedSubmission");
    }

    [Fact]
    public void UpdateProducerInfo_SetsAllFields()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        var producerId = Guid.NewGuid();

        // Act
        submission.UpdateProducerInfo(producerId, "ABC Agency", "contact@abc.com");

        // Assert
        submission.ProducerId.Should().Be(producerId);
        submission.ProducerName.Should().Be("ABC Agency");
        submission.ProducerContactEmail.Should().Be("contact@abc.com");
    }

    [Fact]
    public void UpdateProducerInfo_TrimsWhitespace()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;

        // Act
        submission.UpdateProducerInfo(null, "  ABC Agency  ", "  contact@abc.com  ");

        // Assert
        submission.ProducerName.Should().Be("ABC Agency");
        submission.ProducerContactEmail.Should().Be("contact@abc.com");
    }

    [Fact]
    public void UpdatePolicyDates_SetsBothDates()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        var effectiveDate = new DateTime(2024, 4, 1);
        var expirationDate = new DateTime(2025, 4, 1);

        // Act
        submission.UpdatePolicyDates(effectiveDate, expirationDate);

        // Assert
        submission.EffectiveDate.Should().Be(effectiveDate);
        submission.ExpirationDate.Should().Be(expirationDate);
    }

    [Fact]
    public void UpdateScores_SetsValidScores()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;

        // Act
        submission.UpdateScores(85, 70, 95);

        // Assert
        submission.AppetiteScore.Should().Be(85);
        submission.WinnabilityScore.Should().Be(70);
        submission.DataQualityScore.Should().Be(95);
    }

    [Fact]
    public void UpdateScores_IgnoresOutOfRangeScores()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;

        // Act
        submission.UpdateScores(-10, 150, 50);

        // Assert
        submission.AppetiteScore.Should().BeNull();
        submission.WinnabilityScore.Should().BeNull();
        submission.DataQualityScore.Should().Be(50);
    }

    [Fact]
    public void RemoveCoverage_RemovesFromCollection()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        var coverage = submission.AddCoverage(CoverageType.GeneralLiability);
        var coverageId = coverage.Id;

        // Act
        submission.RemoveCoverage(coverageId);

        // Assert
        submission.Coverages.Should().BeEmpty();
    }

    [Fact]
    public void RemoveCoverage_WithNonExistentId_DoesNothing()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.AddCoverage(CoverageType.GeneralLiability);

        // Act
        submission.RemoveCoverage(Guid.NewGuid());

        // Assert
        submission.Coverages.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveLocation_RemovesFromCollection()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        var address = Address.Create("123 Main St", null, "Austin", "TX", "78701").Value;
        var location = submission.AddLocation(address);
        var locationId = location.Id;

        // Act
        submission.RemoveLocation(locationId);

        // Assert
        submission.Locations.Should().BeEmpty();
    }

    [Fact]
    public void RemoveLocation_WithNonExistentId_DoesNothing()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        var address = Address.Create("123 Main St", null, "Austin", "TX", "78701").Value;
        submission.AddLocation(address);

        // Act
        submission.RemoveLocation(Guid.NewGuid());

        // Assert
        submission.Locations.Should().HaveCount(1);
    }

    [Fact]
    public void AddLoss_AddsToCollection()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        var dateOfLoss = new DateTime(2023, 6, 15);

        // Act
        var loss = submission.AddLoss(dateOfLoss, "Water damage claim");

        // Assert
        submission.LossHistory.Should().ContainSingle();
        loss.DateOfLoss.Should().Be(dateOfLoss);
        loss.Description.Should().Be("Water damage claim");
    }

    [Fact]
    public void RemoveLoss_RemovesFromCollection()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        var loss = submission.AddLoss(new DateTime(2023, 6, 15), "Test loss");
        var lossId = loss.Id;

        // Act
        submission.RemoveLoss(lossId);

        // Assert
        submission.LossHistory.Should().BeEmpty();
    }

    [Fact]
    public void RemoveLoss_WithNonExistentId_DoesNothing()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.AddLoss(new DateTime(2023, 6, 15), "Test loss");

        // Act
        submission.RemoveLoss(Guid.NewGuid());

        // Assert
        submission.LossHistory.Should().HaveCount(1);
    }

    [Fact]
    public void LossCount_ReturnsCorrectCount()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.AddLoss(new DateTime(2023, 1, 1), "Loss 1");
        submission.AddLoss(new DateTime(2023, 6, 1), "Loss 2");

        // Act
        var count = submission.LossCount;

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public void TotalInsuredValue_SumsAllLocationValues()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        var address = Address.Create("123 Main St", null, "Austin", "TX", "78701").Value;

        var location1 = submission.AddLocation(address);
        location1.UpdateValues(Money.FromDecimal(1000000), Money.FromDecimal(500000), null);

        var location2 = submission.AddLocation(address);
        location2.UpdateValues(Money.FromDecimal(2000000), null, Money.FromDecimal(250000));

        // Act
        var total = submission.TotalInsuredValue;

        // Assert
        total.Amount.Should().Be(3750000);
    }

    [Fact]
    public void TotalInsuredValue_WithNoLocations_ReturnsZero()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;

        // Act
        var total = submission.TotalInsuredValue;

        // Assert
        total.Amount.Should().Be(0);
    }

    [Fact]
    public void TotalIncurredLosses_SumsAllLossAmounts()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;

        var loss1 = submission.AddLoss(new DateTime(2023, 1, 1), "Loss 1");
        loss1.UpdateAmounts(Money.FromDecimal(5000), Money.FromDecimal(3000), null);

        var loss2 = submission.AddLoss(new DateTime(2023, 6, 1), "Loss 2");
        loss2.UpdateAmounts(Money.FromDecimal(10000), null, Money.FromDecimal(15000));

        // Act
        var total = submission.TotalIncurredLosses;

        // Assert
        total.Amount.Should().Be(23000); // 8000 (5000+3000) + 15000
    }

    [Fact]
    public void TotalIncurredLosses_WithNoLosses_ReturnsZero()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;

        // Act
        var total = submission.TotalIncurredLosses;

        // Assert
        total.Amount.Should().Be(0);
    }

    [Fact]
    public void HasOpenClaims_WithOpenClaim_ReturnsTrue()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.AddLoss(new DateTime(2023, 1, 1), "Open claim");
        // Status defaults to Open

        // Act
        var hasOpenClaims = submission.HasOpenClaims;

        // Assert
        hasOpenClaims.Should().BeTrue();
    }

    [Fact]
    public void HasOpenClaims_WithReopenedClaim_ReturnsTrue()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        var loss = submission.AddLoss(new DateTime(2023, 1, 1), "Reopened claim");
        loss.UpdateStatus(LossStatus.Reopened);

        // Act
        var hasOpenClaims = submission.HasOpenClaims;

        // Assert
        hasOpenClaims.Should().BeTrue();
    }

    [Fact]
    public void HasOpenClaims_WithAllClosedClaims_ReturnsFalse()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        var loss = submission.AddLoss(new DateTime(2023, 1, 1), "Closed claim");
        loss.UpdateStatus(LossStatus.ClosedWithPayment);

        // Act
        var hasOpenClaims = submission.HasOpenClaims;

        // Assert
        hasOpenClaims.Should().BeFalse();
    }

    [Fact]
    public void HasOpenClaims_WithNoLosses_ReturnsFalse()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;

        // Act
        var hasOpenClaims = submission.HasOpenClaims;

        // Assert
        hasOpenClaims.Should().BeFalse();
    }

    [Fact]
    public void AssignToUnderwriter_FromDraft_DoesNotChangeStatus()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        // Not calling MarkAsReceived, so still in Draft

        // Act
        var result = submission.AssignToUnderwriter(Guid.NewGuid(), "John Smith");

        // Assert
        result.IsSuccess.Should().BeTrue();
        submission.Status.Should().Be(SubmissionStatus.Draft);
        submission.AssignedUnderwriterName.Should().Be("John Smith");
    }

    [Fact]
    public void Decline_WhenAlreadyDeclined_ReturnsFailure()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.MarkAsReceived();
        submission.Decline("First decline");

        // Act
        var result = submission.Decline("Second decline");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.InvalidStatusTransition");
    }

    [Fact]
    public void Withdraw_WhenAlreadyWithdrawn_ReturnsFailure()
    {
        // Arrange
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.MarkAsReceived();
        submission.Withdraw("First withdraw");

        // Act
        var result = submission.Withdraw("Second withdraw");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.InvalidStatusTransition");
    }
}
