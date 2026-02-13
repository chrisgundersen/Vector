using Vector.Infrastructure.Caching;

namespace Vector.Infrastructure.IntegrationTests.Caching;

public class InMemoryCacheServiceTests
{
    private readonly InMemoryCacheService _cacheService;

    public InMemoryCacheServiceTests()
    {
        _cacheService = new InMemoryCacheService();
    }

    [Fact]
    public async Task SetAsync_StoresValueInCache()
    {
        // Arrange
        var key = "test-key";
        var value = new TestCacheItem("Test Value", 42);

        // Act
        await _cacheService.SetAsync(key, value);

        // Assert
        var result = await _cacheService.GetAsync<TestCacheItem>(key);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Value");
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task GetAsync_WithNonExistentKey_ReturnsDefault()
    {
        // Act
        var result = await _cacheService.GetAsync<TestCacheItem>("non-existent-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_RemovesValueFromCache()
    {
        // Arrange
        var key = "test-key";
        var value = new TestCacheItem("Test Value", 42);
        await _cacheService.SetAsync(key, value);

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        var result = await _cacheService.GetAsync<TestCacheItem>(key);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        var key = "test-key";
        var value = new TestCacheItem("Test Value", 42);
        await _cacheService.SetAsync(key, value);

        // Act
        var exists = await _cacheService.ExistsAsync(key);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentKey_ReturnsFalse()
    {
        // Act
        var exists = await _cacheService.ExistsAsync("non-existent-key");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrSetAsync_WithNonExistentKey_CallsFactoryAndCaches()
    {
        // Arrange
        var key = "test-key";
        var factoryCalled = false;

        // Act
        var result = await _cacheService.GetOrSetAsync<TestCacheItem>(
            key,
            async ct =>
            {
                factoryCalled = true;
                await Task.Delay(1, ct);
                return new TestCacheItem("Factory Value", 100);
            });

        // Assert
        factoryCalled.Should().BeTrue();
        result.Name.Should().Be("Factory Value");
        result.Value.Should().Be(100);

        // Verify it was cached
        var cachedValue = await _cacheService.GetAsync<TestCacheItem>(key);
        cachedValue.Should().NotBeNull();
        cachedValue!.Name.Should().Be("Factory Value");
    }

    [Fact]
    public async Task GetOrSetAsync_WithExistingKey_ReturnsCachedValueWithoutCallingFactory()
    {
        // Arrange
        var key = "test-key";
        var existingValue = new TestCacheItem("Existing Value", 50);
        await _cacheService.SetAsync(key, existingValue);
        var factoryCalled = false;

        // Act
        var result = await _cacheService.GetOrSetAsync<TestCacheItem>(
            key,
            async ct =>
            {
                factoryCalled = true;
                await Task.Delay(1, ct);
                return new TestCacheItem("Factory Value", 100);
            });

        // Assert
        factoryCalled.Should().BeFalse();
        result.Name.Should().Be("Existing Value");
        result.Value.Should().Be(50);
    }

    [Fact]
    public async Task SetAsync_WithExpiration_StoresValueWithExpiration()
    {
        // Arrange
        var key = "expiring-key";
        var value = new TestCacheItem("Expiring Value", 42);

        // Act
        await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(30));

        // Assert - Value should exist immediately
        var exists = await _cacheService.ExistsAsync(key);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task SetAsync_OverwritesExistingValue()
    {
        // Arrange
        var key = "test-key";
        var originalValue = new TestCacheItem("Original", 1);
        var newValue = new TestCacheItem("New", 2);

        // Act
        await _cacheService.SetAsync(key, originalValue);
        await _cacheService.SetAsync(key, newValue);

        // Assert
        var result = await _cacheService.GetAsync<TestCacheItem>(key);
        result.Should().NotBeNull();
        result!.Name.Should().Be("New");
        result.Value.Should().Be(2);
    }

    [Fact]
    public async Task GetAsync_WithDifferentTypes_ReturnsCorrectType()
    {
        // Arrange
        await _cacheService.SetAsync("string-key", "test string");
        await _cacheService.SetAsync("int-key", 42);
        await _cacheService.SetAsync("object-key", new TestCacheItem("Test", 100));

        // Act & Assert
        var stringValue = await _cacheService.GetAsync<string>("string-key");
        stringValue.Should().Be("test string");

        var intValue = await _cacheService.GetAsync<int>("int-key");
        intValue.Should().Be(42);

        var objectValue = await _cacheService.GetAsync<TestCacheItem>("object-key");
        objectValue.Should().NotBeNull();
        objectValue!.Name.Should().Be("Test");
    }

    private record TestCacheItem(string Name, int Value);
}
