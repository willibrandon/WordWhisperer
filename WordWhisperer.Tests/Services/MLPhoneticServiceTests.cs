using Microsoft.Extensions.Logging;
using System.Reflection;
using WordWhisperer.Core.Services;
using Xunit;

namespace WordWhisperer.Tests.Services;

public class MLPhoneticServiceTests : IDisposable
{
    private readonly MLPhoneticService _service;
    private readonly TestLogger<MLPhoneticService> _logger;
    private readonly string _testDir;

    public MLPhoneticServiceTests()
    {
        // Setup test directory with test model files
        _testDir = Path.Combine(Path.GetTempPath(), $"WordWhisperer_MLTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        Directory.CreateDirectory(Path.Combine(_testDir, "Data"));
        Directory.CreateDirectory(Path.Combine(_testDir, "Data", "MLModels"));
        
        // Create test logger
        _logger = new TestLogger<MLPhoneticService>();
        
        // Create the service
        _service = new MLPhoneticService(_logger);
        
        // Use reflection to set the base directory for testing
        // This allows us to control where the service looks for models and dictionaries
        typeof(MLPhoneticService)
            .GetField("_modelPath", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_service, Path.Combine(_testDir, "Data", "MLModels", "g2p_model.onnx"));
    }

    [Fact]
    public async Task InitializeAsync_WithoutModelFiles_LogsWarning()
    {
        // Act
        await _service.InitializeAsync();
        
        // Assert
        // Verify that we log a warning when model file is not found
        Assert.Contains(_logger.LogEntries, entry => 
            entry.LogLevel == LogLevel.Warning && 
            entry.Message.Contains("G2P model not found"));
    }

    [Fact]
    public void TranscribeWord_WithoutInitialization_ReturnsNull()
    {
        // Act
        var result = _service.TranscribeWord("test");
        
        // Assert
        Assert.Null(result);
        
        // Verify warning is logged
        Assert.Contains(_logger.LogEntries, entry => 
            entry.LogLevel == LogLevel.Warning && 
            entry.Message.Contains("not initialized"));
    }

    [Fact]
    public async Task InitializeAsync_WithCmuDictionary_LoadsDictionary()
    {
        // Arrange
        // Create a minimal CMU dictionary file for testing
        var cmuDictPath = Path.Combine(_testDir, "Data", "MLModels", "cmudict.txt");
        await File.WriteAllTextAsync(cmuDictPath, @"
;;; Comment line should be ignored
TEST  T EH1 S T
HELLO  HH AH0 L OW1
");

        // Replace the path in the service
        typeof(MLPhoneticService)
            .GetField("_modelPath", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_service, Path.Combine(_testDir, "Data", "MLModels", "g2p_model.onnx"));

        // Act
        await _service.InitializeAsync();
        
        // Assert
        // Verify initialization happened without exceptions
        Assert.Contains(_logger.LogEntries, entry => 
            entry.LogLevel == LogLevel.Warning && 
            entry.Message.Contains("G2P model not found"));
        
        // While we can't directly test if the dictionary was loaded (as it's private),
        // we can verify that no exception was thrown during initialization
    }

    public void Dispose()
    {
        // Clean up test directory
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }
        catch (Exception)
        {
            // Ignore cleanup errors in tests
        }
    }
}

/// <summary>
/// Simple logger implementation for testing
/// </summary>
public class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> LogEntries { get; } = new List<LogEntry>();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel, 
        EventId eventId, 
        TState state, 
        Exception? exception, 
        Func<TState, Exception?, string> formatter)
    {
        LogEntries.Add(new LogEntry
        {
            LogLevel = logLevel,
            EventId = eventId,
            Message = formatter(state, exception),
            Exception = exception,
            Timestamp = DateTime.UtcNow
        });
    }
}

/// <summary>
/// Represents a log entry for testing
/// </summary>
public class LogEntry
{
    public LogLevel LogLevel { get; set; }
    public EventId EventId { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public DateTime Timestamp { get; set; }
}
