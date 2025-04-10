namespace WordWhisperer.Core.Interfaces;

public interface IDictionaryService
{
    Task InitializeAsync();
    Task<string?> GetDefinitionAsync(string word);

    Task<string?> GetPartOfSpeechAsync(string word);

    Task<(string? definition, string? partOfSpeech)> GetWordInfoAsync(string word);
}
