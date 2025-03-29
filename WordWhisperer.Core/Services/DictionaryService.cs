using Microsoft.EntityFrameworkCore;
using WordWhisperer.Core.Data;
using WordWhisperer.Core.Interfaces;

namespace WordWhisperer.Core.Services;

public class DictionaryService(DatabaseContext db) : IDictionaryService
{
    public async Task<string?> GetDefinitionAsync(string word)
    {
        var normalizedWord = word.ToLower();
        var wordEntry = await db.Words
            .FirstOrDefaultAsync(w => w.WordText.ToLower() == normalizedWord);

        return wordEntry?.Definition;
    }

    public async Task<string?> GetPartOfSpeechAsync(string word)
    {
        var normalizedWord = word.ToLower();
        var wordEntry = await db.Words
            .FirstOrDefaultAsync(w => w.WordText.ToLower() == normalizedWord);

        return wordEntry?.PartOfSpeech;
    }

    public async Task<(string? definition, string? partOfSpeech)> GetWordInfoAsync(string word)
    {
        var normalizedWord = word.ToLower();
        var wordEntry = await db.Words
            .FirstOrDefaultAsync(w => w.WordText.ToLower() == normalizedWord);

        return (wordEntry?.Definition, wordEntry?.PartOfSpeech);
    }
}
