using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Entities;
using Vector.Domain.Submission.Enums;
using Vector.Domain.Submission.ValueObjects;

namespace Vector.Domain.UnitTests.Submission.Entities;

public class LossHistoryTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    private LossHistory CreateLoss(DateTime? dateOfLoss = null, string description = "Test loss")
    {
        var submissionResult = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured");
        var submission = submissionResult.Value;
        return submission.AddLoss(dateOfLoss ?? DateTime.UtcNow.AddMonths(-6), description);
    }

    [Fact]
    public void AddLoss_CreatesLossWithCorrectData()
    {
        // Arrange
        var dateOfLoss = new DateTime(2023, 6, 15);

        // Act
        var loss = CreateLoss(dateOfLoss, "Water damage claim");

        // Assert
        loss.Should().NotBeNull();
        loss.Id.Should().NotBeEmpty();
        loss.DateOfLoss.Should().Be(dateOfLoss);
        loss.Description.Should().Be("Water damage claim");
        loss.Status.Should().Be(LossStatus.Open);
    }

    [Fact]
    public void UpdateClaimInfo_SetsClaimDetails()
    {
        // Arrange
        var loss = CreateLoss();

        // Act
        loss.UpdateClaimInfo("CLM-2023-001", CoverageType.PropertyDamage, "ABC Insurance");

        // Assert
        loss.ClaimNumber.Should().Be("CLM-2023-001");
        loss.CoverageType.Should().Be(CoverageType.PropertyDamage);
        loss.Carrier.Should().Be("ABC Insurance");
    }

    [Fact]
    public void UpdateClaimInfo_TrimsWhitespace()
    {
        // Arrange
        var loss = CreateLoss();

        // Act
        loss.UpdateClaimInfo("  CLM-2023-001  ", CoverageType.PropertyDamage, "  ABC Insurance  ");

        // Assert
        loss.ClaimNumber.Should().Be("CLM-2023-001");
        loss.Carrier.Should().Be("ABC Insurance");
    }

    [Fact]
    public void UpdateClaimInfo_AcceptsNullValues()
    {
        // Arrange
        var loss = CreateLoss();
        loss.UpdateClaimInfo("CLM-001", CoverageType.PropertyDamage, "Test Carrier");

        // Act
        loss.UpdateClaimInfo(null, null, null);

        // Assert
        loss.ClaimNumber.Should().BeNull();
        loss.CoverageType.Should().BeNull();
        loss.Carrier.Should().BeNull();
    }

    [Fact]
    public void UpdateAmounts_SetsPaidAndReserved()
    {
        // Arrange
        var loss = CreateLoss();
        var paid = Money.FromDecimal(5000);
        var reserved = Money.FromDecimal(3000);

        // Act
        loss.UpdateAmounts(paid, reserved, null);

        // Assert
        loss.PaidAmount.Should().NotBeNull();
        loss.PaidAmount!.Amount.Should().Be(5000);
        loss.ReservedAmount.Should().NotBeNull();
        loss.ReservedAmount!.Amount.Should().Be(3000);
    }

    [Fact]
    public void UpdateAmounts_WithIncurred_UsesProvidedIncurred()
    {
        // Arrange
        var loss = CreateLoss();
        var paid = Money.FromDecimal(5000);
        var reserved = Money.FromDecimal(3000);
        var incurred = Money.FromDecimal(10000);

        // Act
        loss.UpdateAmounts(paid, reserved, incurred);

        // Assert
        loss.IncurredAmount.Should().NotBeNull();
        loss.IncurredAmount!.Amount.Should().Be(10000);
    }

    [Fact]
    public void UpdateAmounts_WithoutIncurred_CalculatesFromPaidAndReserved()
    {
        // Arrange
        var loss = CreateLoss();
        var paid = Money.FromDecimal(5000);
        var reserved = Money.FromDecimal(3000);

        // Act
        loss.UpdateAmounts(paid, reserved, null);

        // Assert
        loss.IncurredAmount.Should().NotBeNull();
        loss.IncurredAmount!.Amount.Should().Be(8000);
    }

    [Fact]
    public void UpdateStatus_ChangesStatus()
    {
        // Arrange
        var loss = CreateLoss();

        // Act
        loss.UpdateStatus(LossStatus.ClosedWithPayment);

        // Assert
        loss.Status.Should().Be(LossStatus.ClosedWithPayment);
    }

    [Fact]
    public void MarkAsSubrogation_SetsFlag()
    {
        // Arrange
        var loss = CreateLoss();

        // Act
        loss.MarkAsSubrogation(true);

        // Assert
        loss.IsSubrogation.Should().BeTrue();
    }

    [Fact]
    public void TotalIncurred_WithIncurredSet_ReturnsIncurred()
    {
        // Arrange
        var loss = CreateLoss();
        var incurred = Money.FromDecimal(15000);
        loss.UpdateAmounts(Money.FromDecimal(5000), Money.FromDecimal(3000), incurred);

        // Act
        var total = loss.TotalIncurred;

        // Assert
        total.Amount.Should().Be(15000);
    }

    [Fact]
    public void TotalIncurred_WithoutIncurred_CalculatesFromPaidAndReserved()
    {
        // Arrange
        var loss = CreateLoss();
        loss.UpdateAmounts(Money.FromDecimal(5000), Money.FromDecimal(3000), null);
        // Clear the calculated incurred to test the property
        loss.UpdateAmounts(Money.FromDecimal(5000), Money.FromDecimal(3000), null);

        // Act
        var total = loss.TotalIncurred;

        // Assert
        total.Amount.Should().Be(8000);
    }

    [Fact]
    public void TotalIncurred_WithNoAmounts_ReturnsZero()
    {
        // Arrange
        var loss = CreateLoss();

        // Act
        var total = loss.TotalIncurred;

        // Assert
        total.Amount.Should().Be(0);
    }

    [Fact]
    public void TotalIncurred_WithOnlyPaid_ReturnsPaid()
    {
        // Arrange
        var loss = CreateLoss();
        loss.UpdateAmounts(Money.FromDecimal(5000), null, null);

        // Act
        var total = loss.TotalIncurred;

        // Assert
        total.Amount.Should().Be(5000);
    }

    [Fact]
    public void TotalIncurred_WithOnlyReserved_ReturnsReserved()
    {
        // Arrange
        var loss = CreateLoss();
        loss.UpdateAmounts(null, Money.FromDecimal(3000), null);

        // Act
        var total = loss.TotalIncurred;

        // Assert
        total.Amount.Should().Be(3000);
    }
}
