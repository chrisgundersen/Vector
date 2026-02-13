using System.Linq.Expressions;
using Vector.Domain.Common;

namespace Vector.Domain.UnitTests.Common;

public class SpecificationTests
{
    private class TestEntity
    {
        public int Value { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class GreaterThanSpecification(int threshold) : Specification<TestEntity>
    {
        public override Expression<Func<TestEntity, bool>> ToExpression()
        {
            return entity => entity.Value > threshold;
        }
    }

    private class LessThanSpecification(int threshold) : Specification<TestEntity>
    {
        public override Expression<Func<TestEntity, bool>> ToExpression()
        {
            return entity => entity.Value < threshold;
        }
    }

    private class NameStartsWithSpecification(string prefix) : Specification<TestEntity>
    {
        public override Expression<Func<TestEntity, bool>> ToExpression()
        {
            return entity => entity.Name.StartsWith(prefix);
        }
    }

    [Fact]
    public void IsSatisfiedBy_WithMatchingEntity_ReturnsTrue()
    {
        // Arrange
        var spec = new GreaterThanSpecification(5);
        var entity = new TestEntity { Value = 10 };

        // Act
        var result = spec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_WithNonMatchingEntity_ReturnsFalse()
    {
        // Arrange
        var spec = new GreaterThanSpecification(5);
        var entity = new TestEntity { Value = 3 };

        // Act
        var result = spec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void And_WhenBothSatisfied_ReturnsTrue()
    {
        // Arrange
        var greaterThan5 = new GreaterThanSpecification(5);
        var lessThan15 = new LessThanSpecification(15);
        var combinedSpec = greaterThan5.And(lessThan15);
        var entity = new TestEntity { Value = 10 };

        // Act
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void And_WhenOnlyOneSatisfied_ReturnsFalse()
    {
        // Arrange
        var greaterThan5 = new GreaterThanSpecification(5);
        var lessThan8 = new LessThanSpecification(8);
        var combinedSpec = greaterThan5.And(lessThan8);
        var entity = new TestEntity { Value = 10 };

        // Act
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void And_WhenNeitherSatisfied_ReturnsFalse()
    {
        // Arrange
        var greaterThan15 = new GreaterThanSpecification(15);
        var lessThan5 = new LessThanSpecification(5);
        var combinedSpec = greaterThan15.And(lessThan5);
        var entity = new TestEntity { Value = 10 };

        // Act
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Or_WhenBothSatisfied_ReturnsTrue()
    {
        // Arrange
        var greaterThan5 = new GreaterThanSpecification(5);
        var lessThan15 = new LessThanSpecification(15);
        var combinedSpec = greaterThan5.Or(lessThan15);
        var entity = new TestEntity { Value = 10 };

        // Act
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Or_WhenOnlyFirstSatisfied_ReturnsTrue()
    {
        // Arrange
        var greaterThan5 = new GreaterThanSpecification(5);
        var lessThan8 = new LessThanSpecification(8);
        var combinedSpec = greaterThan5.Or(lessThan8);
        var entity = new TestEntity { Value = 10 };

        // Act
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Or_WhenOnlySecondSatisfied_ReturnsTrue()
    {
        // Arrange
        var greaterThan15 = new GreaterThanSpecification(15);
        var lessThan12 = new LessThanSpecification(12);
        var combinedSpec = greaterThan15.Or(lessThan12);
        var entity = new TestEntity { Value = 10 };

        // Act
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Or_WhenNeitherSatisfied_ReturnsFalse()
    {
        // Arrange
        var greaterThan15 = new GreaterThanSpecification(15);
        var lessThan5 = new LessThanSpecification(5);
        var combinedSpec = greaterThan15.Or(lessThan5);
        var entity = new TestEntity { Value = 10 };

        // Act
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Not_WhenOriginalSatisfied_ReturnsFalse()
    {
        // Arrange
        var greaterThan5 = new GreaterThanSpecification(5);
        var notSpec = greaterThan5.Not();
        var entity = new TestEntity { Value = 10 };

        // Act
        var result = notSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Not_WhenOriginalNotSatisfied_ReturnsTrue()
    {
        // Arrange
        var greaterThan15 = new GreaterThanSpecification(15);
        var notSpec = greaterThan15.Not();
        var entity = new TestEntity { Value = 10 };

        // Act
        var result = notSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ToExpression_CanBeUsedInLinqQuery()
    {
        // Arrange
        var spec = new GreaterThanSpecification(5);
        var entities = new List<TestEntity>
        {
            new() { Value = 3 },
            new() { Value = 6 },
            new() { Value = 10 },
            new() { Value = 4 }
        };

        // Act
        var expression = spec.ToExpression();
        var result = entities.AsQueryable().Where(expression).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(e => e.Value > 5);
    }

    [Fact]
    public void ComplexCombination_And_Or_Not_WorksTogether()
    {
        // Arrange
        var greaterThan5 = new GreaterThanSpecification(5);
        var lessThan20 = new LessThanSpecification(20);
        var startsWithA = new NameStartsWithSpecification("A");

        // (Value > 5 AND Value < 20) OR Name starts with "A"
        var complexSpec = greaterThan5.And(lessThan20).Or(startsWithA);

        var entity1 = new TestEntity { Value = 10, Name = "Bob" }; // Should match (10 > 5 AND 10 < 20)
        var entity2 = new TestEntity { Value = 25, Name = "Alice" }; // Should match (starts with A)
        var entity3 = new TestEntity { Value = 3, Name = "Charlie" }; // Should not match

        // Act & Assert
        complexSpec.IsSatisfiedBy(entity1).Should().BeTrue();
        complexSpec.IsSatisfiedBy(entity2).Should().BeTrue();
        complexSpec.IsSatisfiedBy(entity3).Should().BeFalse();
    }

    [Fact]
    public void ChainedAnd_AllMustBeSatisfied()
    {
        // Arrange
        var greaterThan5 = new GreaterThanSpecification(5);
        var lessThan15 = new LessThanSpecification(15);
        var startsWithT = new NameStartsWithSpecification("T");

        var chainedSpec = greaterThan5.And(lessThan15).And(startsWithT);
        var entity = new TestEntity { Value = 10, Name = "Test" };

        // Act
        var result = chainedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ChainedOr_AnyCanBeSatisfied()
    {
        // Arrange
        var greaterThan100 = new GreaterThanSpecification(100);
        var lessThan0 = new LessThanSpecification(0);
        var startsWithT = new NameStartsWithSpecification("T");

        var chainedSpec = greaterThan100.Or(lessThan0).Or(startsWithT);
        var entity = new TestEntity { Value = 50, Name = "Test" };

        // Act
        var result = chainedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void DoubleNot_ReturnsOriginal()
    {
        // Arrange
        var greaterThan5 = new GreaterThanSpecification(5);
        var doubleNotSpec = greaterThan5.Not().Not();
        var entity = new TestEntity { Value = 10 };

        // Act
        var result = doubleNotSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }
}
