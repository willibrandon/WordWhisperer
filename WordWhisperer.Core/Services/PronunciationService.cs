using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PiperSharp;
using PiperSharp.Models;
using WordWhisperer.Core.Data;
using WordWhisperer.Core.Data.Models;
using WordWhisperer.Core.Interfaces;

namespace WordWhisperer.Core.Services;

public class PronunciationService : IPronunciationService, IDisposable
{
    private readonly DatabaseContext _db;
    private readonly string _audioCachePath;
    private readonly string _piperPath;
    private PiperProvider? _piper;
    private VoiceModel? _currentModel;
    private readonly Dictionary<string, string> _accentToModelKey = new()
    {
        ["american"] = "en_US-lessac-medium",
        ["british"] = "en_GB-alba-medium"
    };

    public PronunciationService(DatabaseContext db, IConfiguration configuration)
    {
        _db = db;
        _audioCachePath = configuration["AudioCachePath"] ?? "AudioCache";
        _piperPath = Path.Combine(_audioCachePath, "piper");
        Directory.CreateDirectory(_audioCachePath);
    }

    private async Task EnsureInitializedAsync(string accent)
    {
        if (_piper == null)
        {
            // Download and extract Piper if needed
            await PiperDownloader.DownloadPiper().ExtractPiper(_piperPath);
            
            // Create provider
            _piper = new PiperProvider(new PiperConfiguration
            {
                ExecutableLocation = Path.Combine(_piperPath, "piper.exe"),
                WorkingDirectory = _piperPath
            });
        }

        // Get the model key for the accent
        if (!_accentToModelKey.TryGetValue(accent, out var modelKey))
        {
            throw new ArgumentException($"Unsupported accent: {accent}");
        }

        // Download and load the model if needed
        if (_currentModel?.Key != modelKey)
        {
            _currentModel = await PiperDownloader.DownloadModelByKey(modelKey);
            _piper.Configuration.Model = _currentModel;
        }
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

        // Initialize Piper with the correct model
        await EnsureInitializedAsync(accent);

        try
        {
            // Generate audio using Piper
            var audioData = await _piper!.InferAsync(word, AudioOutputType.Wav);
            await File.WriteAllBytesAsync(filePath, audioData);

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
        catch (Exception)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            throw;
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
        GC.SuppressFinalize(this);
    }
}
