using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;
using WordWhisperer.Core.Data;
using WordWhisperer.Core.Data.Models;
using WordWhisperer.Core.Models;
using WordWhisperer.Core.Services;
using Xunit;

namespace WordWhisperer.Tests.Services;

public class PhoneticServiceTests : IDisposable
{
    private readonly DatabaseContext _db;
    private readonly PhoneticService _service;
    private readonly TestLogger<PhoneticService> _logger;
    private readonly TestLogger<MLPhoneticService> _mlLogger;
    private readonly TestLogger<PhoneticDictionaryService> _dictLogger;
    private readonly PhoneticDictionaryService _dictionaryService;
    private readonly MLPhoneticService _mlPhoneticService;
    private readonly PhoneticServiceConfig _config;
    private readonly string _testDir;

    public PhoneticServiceTests()
    {
        // Setup test database
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(databaseName: $"WordWhisperer_Test_{Guid.NewGuid()}")
            .Options;
        _db = new DatabaseContext(options);

        // Setup test directory
        _testDir = Path.Combine(Path.GetTempPath(), $"WordWhisperer_Test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        Directory.CreateDirectory(Path.Combine(_testDir, "Data"));
        
        // Create loggers
        _logger = new TestLogger<PhoneticService>();
        _mlLogger = new TestLogger<MLPhoneticService>();
        _dictLogger = new TestLogger<PhoneticDictionaryService>();

        // Create services
        _dictionaryService = new PhoneticDictionaryService(_dictLogger);
        _mlPhoneticService = new MLPhoneticService(_mlLogger);

        // Create configuration
        _config = new PhoneticServiceConfig { UseMachineLearning = true };
        var optionsWrapper = new OptionsWrapper<PhoneticServiceConfig>(_config);

        // Create the service
        _service = new PhoneticService(_db, _dictionaryService, _mlPhoneticService, optionsWrapper, _logger);
        
        // Initialize the dictionary service with some default data
        // by using private reflection to set entries
        var entriesField = typeof(PhoneticDictionaryService)
            .GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance);
            
        var rulesField = typeof(PhoneticDictionaryService)
            .GetField("_rules", BindingFlags.NonPublic | BindingFlags.Instance);
            
        if (entriesField != null)
        {
            var entries = new Dictionary<string, PhoneticEntry>
            {
                ["hello"] = new PhoneticEntry
                {
                    Ipa = "həˈloʊ",
                    Simplified = "huh-LOW"
                },
                ["sample"] = new PhoneticEntry
                {
                    Ipa = "ˈsæmpəl",
                    Simplified = "SAM-puhl"
                },
                ["newword"] = new PhoneticEntry
                {
                    Ipa = "ˈnuːwɜːd",
                    Simplified = "NOO-werd"
                }
            };
            
            entriesField.SetValue(_dictionaryService, entries);
        }
        
        if (rulesField != null)
        {
            var rules = new PhoneticRules
            {
                Vowels = new Dictionary<string, VowelRule>
                {
                    ["a"] = new VowelRule
                    {
                        Default = "æ",
                        Simplified = "a",
                        Contexts = new Dictionary<string, string>()
                    }
                },
                Consonants = new Dictionary<string, ConsonantRule>
                {
                    ["th"] = new ConsonantRule
                    {
                        Default = "θ",
                        Simplified = "th"
                    }
                },
                StressPatterns = new StressPatterns
                {
                    NounTwoSyllable = new[] { 1, 0 },
                    ThreeSyllable = new[] { 1, 0, 0 }
                }
            };
            
            rulesField.SetValue(_dictionaryService, rules);
        }
    }

    [Fact]
    public async Task GetOrGeneratePhoneticsAsync_ExistingWordInDb_ReturnsFromDb()
    {
        // Arrange
        var word = new Word
        {
            WordText = "test",
            IpaPhonetic = "tɛst",
            Phonetic = "TEST",
            IsGenerated = false
        };
        _db.Words.Add(word);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetOrGeneratePhoneticsAsync("test", "american");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("tɛst", result.Value.ipa);
        Assert.Equal("TEST", result.Value.simplified);
    }

    [Fact]
    public async Task GetOrGeneratePhoneticsAsync_MlEnabledButUninitializedMl_UsesRuleBased()
    {
        // Act
        var result = await _service.GetOrGeneratePhoneticsAsync("hello", "american");

        // Assert
        Assert.NotNull(result);
        
        // Verify ML was used but returned null (logs will show uninitialized warning)
        Assert.Contains(_mlLogger.LogEntries, entry =>
            entry.LogLevel == LogLevel.Warning &&
            entry.Message.Contains("not initialized"));
            
        // Verify we got results from dictionary service
        Assert.Equal("həˈloʊ", result.Value.ipa);
        Assert.Equal("huh-LOW", result.Value.simplified);
    }

    [Fact]
    public async Task GetOrGeneratePhoneticsAsync_MlDisabled_UsesRuleBased()
    {
        // Arrange - Set ML disabled
        _config.UseMachineLearning = false;

        // Act
        var result = await _service.GetOrGeneratePhoneticsAsync("hello", "american");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("həˈloʊ", result.Value.ipa);
        Assert.Equal("huh-LOW", result.Value.simplified);
    }

    [Fact]
    public async Task GetOrGeneratePhoneticsAsync_SavesGeneratedPhonetics()
    {
        // Act
        var result = await _service.GetOrGeneratePhoneticsAsync("newword", "american");

        // Assert - check result
        Assert.NotNull(result);
        Assert.Equal("ˈnuːwɜːd", result.Value.ipa);
        Assert.Equal("NOO-werd", result.Value.simplified);
        
        // Assert - check DB
        var savedWord = await _db.Words
            .FirstOrDefaultAsync(w => w.WordText.Equals("newword", StringComparison.OrdinalIgnoreCase));
        
        Assert.NotNull(savedWord);
        Assert.Equal("ˈnuːwɜːd", savedWord.IpaPhonetic);
        Assert.Equal("NOO-werd", savedWord.Phonetic);
        Assert.True(savedWord.IsGenerated);
    }

    public void Dispose()
    {
        _db.Dispose();
        
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
