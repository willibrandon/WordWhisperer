using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Speech.Synthesis;
using WordWhisperer.Core.Data;
using WordWhisperer.Core.Data.Models;
using WordWhisperer.Core.Interfaces;

namespace WordWhisperer.Core.Services;

public class PronunciationService : IPronunciationService, IDisposable
{
    private readonly DatabaseContext _db;
    private readonly string _audioCachePath;
    private readonly SpeechSynthesizer _synthesizer;

    public PronunciationService(DatabaseContext db, IConfiguration configuration)
    {
        _db = db;
        _audioCachePath = configuration["AudioCachePath"] ?? "AudioCache";
        Directory.CreateDirectory(_audioCachePath);

        _synthesizer = new SpeechSynthesizer();

        // Configure default voice settings
        _synthesizer.Rate = 0; // Normal speed
        _synthesizer.Volume = 100;
    }

    public async Task<string?> GetOrGenerateAudioAsync(string word, string accent, bool slow)
    {
        // Check if we have a cached audio file
        var audioPath = await GetAudioPathAsync(word, accent, slow);
        if (audioPath != null && File.Exists(audioPath))
        {
            return audioPath;
        }

        // Generate new audio file
        var fileName = GetAudioFileName(word, accent, slow);
        var filePath = Path.Combine(_audioCachePath, fileName);

        // Adjust speed if needed
        _synthesizer.Rate = slow ? -2 : 0;

        // Generate and save audio
        try
        {
            _synthesizer.SetOutputToWaveFile(filePath);
            _synthesizer.Speak(word);

            // Update database with new audio path
            var wordEntry = await _db.Words
                .FirstOrDefaultAsync(w => w.WordText.ToLower() == word.ToLower());

            if (wordEntry != null)
            {
                if (accent == "american") // Default accent
                {
                    wordEntry.AudioPath = filePath;
                }
                else
                {
                    var variant = await _db.WordVariants
                        .FirstOrDefaultAsync(v => v.WordId == wordEntry.Id && v.Variant == accent);

                    if (variant == null)
                    {
                        variant = new WordVariant
                        {
                            WordId = wordEntry.Id,
                            Variant = accent,
                            AudioPath = filePath
                        };
                        _db.WordVariants.Add(variant);
                    }
                    else
                    {
                        variant.AudioPath = filePath;
                    }
                }

                await _db.SaveChangesAsync();
            }

            return filePath;
        }
        finally
        {
            _synthesizer.SetOutputToNull();
        }
    }

    public async Task<bool> AudioExistsAsync(string word, string accent, bool slow)
    {
        var audioPath = await GetAudioPathAsync(word, accent, slow);
        return audioPath != null && File.Exists(audioPath);
    }

    public async Task<string?> GetAudioPathAsync(string word, string accent, bool slow)
    {
        var wordEntry = await _db.Words
            .Include(w => w.Variants)
            .FirstOrDefaultAsync(w => w.WordText.ToLower() == word.ToLower());

        if (wordEntry == null)
            return null;

        if (accent == "american") // Default accent
            return wordEntry.AudioPath;

        var variant = wordEntry.Variants.FirstOrDefault(v => v.Variant == accent);
        return variant?.AudioPath;
    }

    private string GetAudioFileName(string word, string accent, bool slow)
    {
        var speedSuffix = slow ? "_slow" : "";
        return $"{word}_{accent}{speedSuffix}.wav";
    }

    public void Dispose()
    {
        _synthesizer.Dispose();
        GC.SuppressFinalize(this);
    }
}
