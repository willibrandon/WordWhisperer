using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PiperSharp;
using PiperSharp.Models;
using System.Runtime.InteropServices;
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
    private static readonly SemaphoreSlim _modelLock = new(1, 1);
    private static readonly Dictionary<string, bool> _modelDownloaded = new();

    public PronunciationService(DatabaseContext db, IConfiguration configuration)
    {
        _db = db;
        // Get the base directory from configuration or use the current directory
        var baseDir = configuration["BaseDirectory"] ?? AppDomain.CurrentDomain.BaseDirectory;
        _audioCachePath = Path.GetFullPath(Path.Combine(baseDir, configuration["AudioCachePath"] ?? "AudioCache"));
        _piperPath = _audioCachePath; // Don't create an extra piper subdirectory
        Directory.CreateDirectory(_audioCachePath);
    }

    private async Task EnsureInitializedAsync(string accent)
    {
        if (_piper == null)
        {
            // Get the correct executable name based on platform
            string exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "piper.exe" : "piper";
            string piperExePath = Path.Combine(_piperPath, "piper", exeName);

            // Only download and extract if Piper isn't already installed
            if (!File.Exists(piperExePath))
            {
                // Download and extract Piper if needed
                var piperZip = PiperDownloader.DownloadPiper();
                await piperZip.ExtractPiper(_piperPath);
            }
            
            // Create provider with paths that account for PiperSharp's piper subdirectory
            _piper = new PiperProvider(new PiperConfiguration
            {
                ExecutableLocation = piperExePath,
                WorkingDirectory = Path.Combine(_piperPath, "piper")
            });
        }

        // Get the model key for the accent
        if (!_accentToModelKey.TryGetValue(accent, out var modelKey))
        {
            throw new ArgumentException($"Unsupported accent: {accent}");
        }

        // Download and load the model if needed, with synchronization
        if (_currentModel?.Key != modelKey)
        {
            await _modelLock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_currentModel?.Key != modelKey)
                {
                    // Ensure models directory exists
                    Directory.CreateDirectory(Path.Combine(_piperPath, "models"));

                    // Check if we've already downloaded this model in another instance
                    if (!_modelDownloaded.TryGetValue(modelKey, out var downloaded) || !downloaded)
                    {
                        const int maxRetries = 3;
                        int retryCount = 0;
                        bool success = false;

                        while (!success && retryCount < maxRetries)
                        {
                            try
                            {
                                _currentModel = await PiperDownloader.DownloadModelByKey(modelKey);
                                success = true;
                                _modelDownloaded[modelKey] = true;
                            }
                            catch (IOException ex) when (ex.HResult == unchecked((int)0x80070020)) // File in use
                            {
                                retryCount++;
                                if (retryCount < maxRetries)
                                {
                                    await Task.Delay(1000 * retryCount); // Exponential backoff
                                }
                                else throw; // Re-throw if we've exhausted retries
                            }
                        }
                    }
                    else
                    {
                        // If we've already downloaded the model, create a new model instance
                        _currentModel = await PiperDownloader.GetModelByKey(modelKey);
                        if (_currentModel == null)
                        {
                            throw new InvalidOperationException($"Failed to load voice model for accent: {accent}");
                        }
                    }

                    // Ensure the model is set in the configuration
                    if (_currentModel != null)
                    {
                        _piper!.Configuration.Model = _currentModel;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Failed to load voice model for accent: {accent}");
                    }
                }
            }
            finally
            {
                _modelLock.Release();
            }
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
        var filePath = Path.GetFullPath(Path.Combine(_audioCachePath, fileName));

        try
        {
            // Create directory if it doesn't exist
            Directory.CreateDirectory(_audioCachePath);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Use macOS say command
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "say",
                        Arguments = $"{(slow ? "-r 100 " : "")}-o \"{filePath}\" \"{word}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                await process.WaitForExitAsync();
            }
            else
            {
                // Initialize Piper with the correct model
                await EnsureInitializedAsync(accent);

                // Generate audio using Piper
                var audioData = await _piper!.InferAsync(word, AudioOutputType.Wav);
                await File.WriteAllBytesAsync(filePath, audioData);
            }

            // Update database with new audio path
            var normalizedWord = word.ToLower();
            var wordEntry = await _db.Words
                .FirstOrDefaultAsync(w => w.WordText.ToLower() == normalizedWord);

            if (wordEntry == null)
            {
                wordEntry = new Word
                {
                    WordText = word,
                    AudioPath = accent == "american" ? filePath : null,
                    CreatedAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow
                };
                _db.Words.Add(wordEntry);
                await _db.SaveChangesAsync();
            }

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
        var normalizedWord = word.ToLower();
        var wordEntry = await _db.Words
            .Include(w => w.Variants)
            .FirstOrDefaultAsync(w => w.WordText.ToLower() == normalizedWord);

        if (wordEntry == null)
            return null;

        // Get the stored path
        var storedPath = accent == "american" 
            ? wordEntry.AudioPath 
            : wordEntry.Variants.FirstOrDefault(v => v.Variant == accent)?.AudioPath;
        
        return storedPath;
    }

    private static string GetAudioFileName(string word, string accent, bool slow)
    {
        // If the word is too long, use a hash instead
        string baseFileName = word.Length > 30 ? GetWordHash(word) : word;
        var speedSuffix = slow ? "_slow" : "";
        return $"{baseFileName}_{accent}{speedSuffix}{(RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ".aiff" : ".wav")}";
    }

    private static string GetWordHash(string word)
    {
        // Create a deterministic hash of the word
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(word));
        // Use first 8 bytes as hex string
        return BitConverter.ToString(hashBytes, 0, 8).Replace("-", "").ToLowerInvariant();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
