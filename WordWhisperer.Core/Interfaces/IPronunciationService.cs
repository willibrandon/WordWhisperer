namespace WordWhisperer.Core.Interfaces;

public interface IPronunciationService
{
    Task<string?> GetOrGenerateAudioAsync(string word, string accent, bool slow);

    Task<bool> AudioExistsAsync(string word, string accent, bool slow);

    Task<string?> GetAudioPathAsync(string word, string accent, bool slow);
}
