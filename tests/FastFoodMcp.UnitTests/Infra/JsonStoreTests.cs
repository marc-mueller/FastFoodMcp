using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using FastFoodMcp.Infra;

namespace FastFoodMcp.UnitTests.Infra;

public class JsonStoreTests : IDisposable
{
    private readonly string _tempFilePath;
    private readonly Mock<ILogger<JsonStore<TestData>>> _mockLogger;

    public JsonStoreTests()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.json");
        _mockLogger = new Mock<ILogger<JsonStore<TestData>>>();
    }

    public void Dispose()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Fact]
    public void Constructor_LoadsDataFromFile()
    {
        // Arrange
        var testData = new TestData { Name = "Test", Value = 42 };
        File.WriteAllText(_tempFilePath, JsonSerializer.Serialize(testData));

        // Act
        var store = new JsonStore<TestData>(_tempFilePath, _mockLogger.Object);

        // Assert
        store.Data.Should().NotBeNull();
        store.Data.Name.Should().Be("Test");
        store.Data.Value.Should().Be(42);
    }

    [Fact]
    public void Constructor_ThrowsWhenFileDoesNotExist()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.json");

        // Act & Assert
        var act = () => new JsonStore<TestData>(nonExistentPath, _mockLogger.Object);
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void Constructor_ThrowsWhenFileIsInvalidJson()
    {
        // Arrange
        File.WriteAllText(_tempFilePath, "{ invalid json }");

        // Act & Assert
        var act = () => new JsonStore<TestData>(_tempFilePath, _mockLogger.Object);
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public async Task HotReload_UpdatesDataWhenFileChanges()
    {
        // Arrange
        var initialData = new TestData { Name = "Initial", Value = 1 };
        File.WriteAllText(_tempFilePath, JsonSerializer.Serialize(initialData));
        var store = new JsonStore<TestData>(_tempFilePath, _mockLogger.Object);

        store.Data.Name.Should().Be("Initial");

        // Act
        var updatedData = new TestData { Name = "Updated", Value = 2 };
        File.WriteAllText(_tempFilePath, JsonSerializer.Serialize(updatedData));
        
        // Wait for file watcher to trigger (with timeout)
        await Task.Delay(1500);

        // Assert
        store.Data.Name.Should().Be("Updated");
        store.Data.Value.Should().Be(2);
    }

    [Fact]
    public void Data_IsThreadSafe()
    {
        // Arrange
        var testData = new TestData { Name = "Test", Value = 42 };
        File.WriteAllText(_tempFilePath, JsonSerializer.Serialize(testData));
        var store = new JsonStore<TestData>(_tempFilePath, _mockLogger.Object);

        // Act - Access from multiple threads
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                var name = store.Data.Name;
                var value = store.Data.Value;
            }
        }));

        var act = async () => await Task.WhenAll(tasks);

        // Assert - Should not throw any threading exceptions
        act.Should().NotThrowAsync();
    }

    [Fact]
    public void HotReload_LogsErrorOnInvalidJson()
    {
        // Arrange
        var initialData = new TestData { Name = "Initial", Value = 1 };
        File.WriteAllText(_tempFilePath, JsonSerializer.Serialize(initialData));
        var store = new JsonStore<TestData>(_tempFilePath, _mockLogger.Object);

        // Act - Write invalid JSON
        File.WriteAllText(_tempFilePath, "{ invalid }");
        
        // Wait for file watcher
        Thread.Sleep(1500);

        // Assert - Data should remain unchanged after failed reload
        store.Data.Name.Should().Be("Initial");
        store.Data.Value.Should().Be(1);
    }

    // Test data model
    public class TestData
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }
}
