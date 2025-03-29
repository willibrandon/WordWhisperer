using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WordWhisperer.Core.Data;
using WordWhisperer.Core.Data.Models;
using WordWhisperer.Core.Interfaces;

namespace WordWhisperer.Core.Services;

public class DictionaryService : IDictionaryService
{
    private readonly DatabaseContext _db;
    private readonly ILogger<DictionaryService> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized;

    public DictionaryService(DatabaseContext db, ILogger<DictionaryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        // Use a lock to prevent multiple simultaneous initializations
        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized) return;

            // Check if we have any words in the database
            if (!await _db.Words.AnyAsync())
            {
                _logger.LogInformation("Initializing dictionary database from CMU dictionary...");
                var dictionaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "MLModels", "cmudict.txt");
                
                if (!File.Exists(dictionaryPath))
                {
                    _logger.LogError("CMU dictionary file not found at {Path}", dictionaryPath);
                    return;
                }

                var lines = await File.ReadAllLinesAsync(dictionaryPath);
                var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var batch = new List<Word>();
                var batchSize = 1000;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";;;"))
                        continue;

                    // Split on any number of spaces and remove empty entries
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)  // Need at least a word and one phoneme
                        continue;

                    var word = parts[0].ToLowerInvariant();
                    // Remove variant numbers (e.g., "WORD(2)" becomes "WORD")
                    word = word.Split('(')[0];

                    if (words.Contains(word))
                        continue;

                    // Join all remaining parts as the pronunciation
                    var pronunciation = string.Join(" ", parts.Skip(1));

                    words.Add(word);
                    var wordEntry = new Word
                    {
                        WordText = word,
                        // For now, we'll use a simple description since CMU dict doesn't include definitions
                        Definition = $"A word that can be pronounced as: {pronunciation}",
                        PartOfSpeech = "unknown", // CMU dict doesn't include part of speech
                        CreatedAt = DateTime.UtcNow,
                        LastAccessedAt = DateTime.UtcNow
                    };

                    batch.Add(wordEntry);

                    // Save in batches to improve performance
                    if (batch.Count >= batchSize)
                    {
                        _db.Words.AddRange(batch);
                        await _db.SaveChangesAsync();
                        batch.Clear();
                        _logger.LogInformation("Added batch of {Count} words to dictionary", batchSize);
                    }
                }

                // Add any remaining words
                if (batch.Count > 0)
                {
                    _db.Words.AddRange(batch);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Added final batch of {Count} words to dictionary", batch.Count);
                }

                _logger.LogInformation("Added total of {Count} words to dictionary database", words.Count);
            }

            _isInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<string?> GetDefinitionAsync(string word)
    {
        await InitializeAsync();
        var normalizedWord = word.ToLower();
        var wordEntry = await _db.Words
            .FirstOrDefaultAsync(w => w.WordText.ToLower() == normalizedWord);

        if (wordEntry != null)
        {
            wordEntry.LastAccessedAt = DateTime.UtcNow;
            wordEntry.AccessCount++;
            await _db.SaveChangesAsync();
        }

        return wordEntry?.Definition;
    }

    public async Task<string?> GetPartOfSpeechAsync(string word)
    {
        await InitializeAsync();
        var normalizedWord = word.ToLower();
        var wordEntry = await _db.Words
            .FirstOrDefaultAsync(w => w.WordText.ToLower() == normalizedWord);

        return wordEntry?.PartOfSpeech;
    }

    public async Task<(string? definition, string? partOfSpeech)> GetWordInfoAsync(string word)
    {
        await InitializeAsync();
        var normalizedWord = word.ToLower();
        var wordEntry = await _db.Words
            .FirstOrDefaultAsync(w => w.WordText.ToLower() == normalizedWord);

        if (wordEntry != null)
        {
            wordEntry.LastAccessedAt = DateTime.UtcNow;
            wordEntry.AccessCount++;
            await _db.SaveChangesAsync();
        }

        return (wordEntry?.Definition, wordEntry?.PartOfSpeech);
    }
}
