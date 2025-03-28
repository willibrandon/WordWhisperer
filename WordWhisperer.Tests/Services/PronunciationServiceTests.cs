using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WordWhisperer.Core.Data;
using WordWhisperer.Core.Services;
using Xunit;

namespace WordWhisperer.Tests.Services;

public class PronunciationServiceTests : IDisposable
{
    private readonly DatabaseContext _db;
    private readonly PronunciationService _service;
    private readonly string _testDir;

    public PronunciationServiceTests()
    {
        // Setup test database
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(databaseName: $"WordWhisperer_Test_{Guid.NewGuid()}")
            .Options;
        _db = new DatabaseContext(options);

        // Setup test directory for audio files
        _testDir = Path.Combine(Path.GetTempPath(), $"WordWhisperer_Test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);

        // Setup configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AudioCachePath"] = _testDir
            })
            .Build();

        _service = new PronunciationService(_db, configuration);
    }

    [Fact]
    public async Task GetOrGenerateAudioAsync_NewWord_GeneratesAudioAndSavesToDb()
    {
        // Arrange
        const string word = "hello";
        const string accent = "american";

        // Act
        var audioPath = await _service.GetOrGenerateAudioAsync(word, accent, false);

        // Assert
        Assert.NotNull(audioPath);
        Assert.True(File.Exists(audioPath));

        // Check database entry
        var dbWord = await _db.Words.FirstOrDefaultAsync(w => w.WordText == word);
        Assert.NotNull(dbWord);
        Assert.Equal(audioPath, dbWord.AudioPath);
    }

    [Fact]
    public async Task GetOrGenerateAudioAsync_ExistingWord_ReturnsCachedAudio()
    {
        // Arrange
        const string word = "test";
        const string accent = "american";
        
        // Generate audio first time
        var firstPath = await _service.GetOrGenerateAudioAsync(word, accent, false);
        var firstModified = File.GetLastWriteTime(firstPath!);

        // Wait a moment to ensure timestamps would be different
        await Task.Delay(100);

        // Act
        var secondPath = await _service.GetOrGenerateAudioAsync(word, accent, false);
        var secondModified = File.GetLastWriteTime(secondPath!);

        // Assert
        Assert.Equal(firstPath, secondPath);
        Assert.Equal(firstModified, secondModified); // File wasn't regenerated
    }

    [Fact]
    public async Task GetOrGenerateAudioAsync_DifferentAccents_GeneratesSeparateFiles()
    {
        // Arrange
        const string word = "world";

        // Act
        var americanPath = await _service.GetOrGenerateAudioAsync(word, "american", false);
        var britishPath = await _service.GetOrGenerateAudioAsync(word, "british", false);

        // Assert
        Assert.NotNull(americanPath);
        Assert.NotNull(britishPath);
        Assert.NotEqual(americanPath, britishPath);
        Assert.True(File.Exists(americanPath));
        Assert.True(File.Exists(britishPath));

        // Check database entries
        var dbWord = await _db.Words.Include(w => w.Variants).FirstAsync(w => w.WordText == word);
        Assert.Equal(americanPath, dbWord.AudioPath); // American is default
        Assert.Contains(dbWord.Variants, v => v.Variant == "british" && v.AudioPath == britishPath);
    }

    [Fact]
    public async Task AudioExistsAsync_ReturnsTrueForExistingAudio()
    {
        // Arrange
        const string word = "check";
        const string accent = "american";
        await _service.GetOrGenerateAudioAsync(word, accent, false);

        // Act
        var exists = await _service.AudioExistsAsync(word, accent, false);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task AudioExistsAsync_ReturnsFalseForNonExistentAudio()
    {
        // Act
        var exists = await _service.AudioExistsAsync("nonexistent", "american", false);

        // Assert
        Assert.False(exists);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();

        // Cleanup test directory
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }
} 