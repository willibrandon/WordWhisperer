using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WordWhisperer.Core.Data;
using WordWhisperer.Core.Data.Models;
using WordWhisperer.Core.Interfaces;
using WordWhisperer.Core.Models;

namespace WordWhisperer.Core.Services;

public class PhoneticService(
    DatabaseContext db,
    PhoneticDictionaryService dictionaryService,
    MLPhoneticService mlPhoneticService,
    IOptions<PhoneticServiceConfig> config,
    ILogger<PhoneticService> logger) : IPhoneticService
{
    private readonly PhoneticServiceConfig _config = config.Value;

    public async Task<(string ipa, string simplified)?> GetOrGeneratePhoneticsAsync(string word, string accent)
    {
        // First, check if we have the phonetics in our database
        var wordEntry = await db.Words
            .Include(w => w.Variants)
            .FirstOrDefaultAsync(w => w.WordText.Equals(word, StringComparison.CurrentCultureIgnoreCase));

        if (wordEntry != null)
        {
            if (accent == "american") // Default accent
            {
                if (!string.IsNullOrEmpty(wordEntry.IpaPhonetic) && !string.IsNullOrEmpty(wordEntry.Phonetic))
                {
                    return (wordEntry.IpaPhonetic, wordEntry.Phonetic);
                }
            }
            else
            {
                var variant = wordEntry.Variants.FirstOrDefault(v => v.Variant == accent);
                if (variant != null && !string.IsNullOrEmpty(variant.IpaPhonetic) && !string.IsNullOrEmpty(variant.Phonetic))
                {
                    return (variant.IpaPhonetic, variant.Phonetic);
                }
            }
        }

        // Generate phonetics using ML or rule-based approach
        (string ipa, string simplified)? phonetics = null;

        // Try ML-based transcription first if enabled
        if (_config.UseMachineLearning)
        {
            try
            {
                phonetics = mlPhoneticService.TranscribeWord(word, accent);
                if (phonetics != null)
                {
                    logger.LogDebug("Generated phonetics for '{Word}' using ML model: {Phonetics}", 
                        word, phonetics.Value.ipa);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during ML phonetic transcription for word: {Word}", word);
            }
        }

        // Fall back to rule-based if ML failed or is disabled
        if (phonetics == null)
        {
            phonetics = dictionaryService.GetPhonetics(word, accent);
            if (phonetics != null)
            {
                logger.LogDebug("Generated phonetics for '{Word}' using rule-based method: {Phonetics}", 
                    word, phonetics.Value.ipa);
            }
        }

        if (phonetics == null)
        {
            return null;
        }

        // Store the generated phonetics
        if (wordEntry == null)
        {
            wordEntry = new Word
            {
                WordText = word,
                IpaPhonetic = phonetics.Value.ipa,
                Phonetic = phonetics.Value.simplified,
                IsGenerated = true
            };
            db.Words.Add(wordEntry);
        }
        else if (accent == "american")
        {
            wordEntry.IpaPhonetic = phonetics.Value.ipa;
            wordEntry.Phonetic = phonetics.Value.simplified;
            wordEntry.IsGenerated = true;
        }
        else
        {
            var variant = wordEntry.Variants.FirstOrDefault(v => v.Variant == accent);
            if (variant == null)
            {
                variant = new WordVariant
                {
                    WordId = wordEntry.Id,
                    Variant = accent,
                    IpaPhonetic = phonetics.Value.ipa,
                    Phonetic = phonetics.Value.simplified
                };
                db.WordVariants.Add(variant);
            }
            else
            {
                variant.IpaPhonetic = phonetics.Value.ipa;
                variant.Phonetic = phonetics.Value.simplified;
            }
        }

        await db.SaveChangesAsync();
        return phonetics;
    }

    public async Task<bool> HasMultiplePronunciationsAsync(string word)
    {
        var wordEntry = await db.Words
            .Include(w => w.Variants)
            .FirstOrDefaultAsync(w => w.WordText.Equals(word, StringComparison.CurrentCultureIgnoreCase));

        return wordEntry?.HasMultiplePron ?? false;
    }
}
