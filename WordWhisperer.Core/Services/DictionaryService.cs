using Microsoft.EntityFrameworkCore;
using WordWhisperer.Core.Data;
using WordWhisperer.Core.Interfaces;

namespace WordWhisperer.Core.Services;

public class DictionaryService(DatabaseContext db) : IDictionaryService
{
    public async Task<string?> GetDefinitionAsync(string word)
    {
        var wordEntry = await db.Words
            .FirstOrDefaultAsync(w => w.WordText.Equals(word, StringComparison.CurrentCultureIgnoreCase));

        return wordEntry?.Definition;
    }

    public async Task<string?> GetPartOfSpeechAsync(string word)
    {
        var wordEntry = await db.Words
            .FirstOrDefaultAsync(w => w.WordText.Equals(word, StringComparison.CurrentCultureIgnoreCase));

        return wordEntry?.PartOfSpeech;
    }

    public async Task<(string? definition, string? partOfSpeech)> GetWordInfoAsync(string word)
    {
        var wordEntry = await db.Words
            .FirstOrDefaultAsync(w => w.WordText.Equals(word, StringComparison.CurrentCultureIgnoreCase));

        return (wordEntry?.Definition, wordEntry?.PartOfSpeech);
    }
}
