using Microsoft.EntityFrameworkCore;
using WordWhisperer.Core.Data;
using WordWhisperer.Core.Data.Models;
using WordWhisperer.Core.Interfaces;

namespace WordWhisperer.Core.Services;

public class PhoneticService(DatabaseContext db) : IPhoneticService
{
    public async Task<(string ipa, string simplified)?> GetOrGeneratePhoneticsAsync(string word, string accent)
    {
        // First, check if we have the phonetics in our database
        var wordEntry = await db.Words
            .Include(w => w.Variants)
            .FirstOrDefaultAsync(w => w.WordText.ToLower() == word.ToLower());

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

        // If we don't have the phonetics, generate them
        // For now, we'll use a simple rule-based approach
        var (generatedIpa, generatedSimplified) = GeneratePhonetics(word);

        // Store the generated phonetics
        if (wordEntry == null)
        {
            wordEntry = new Word
            {
                WordText = word,
                IpaPhonetic = generatedIpa,
                Phonetic = generatedSimplified,
                IsGenerated = true
            };
            db.Words.Add(wordEntry);
        }
        else if (accent == "american")
        {
            wordEntry.IpaPhonetic = generatedIpa;
            wordEntry.Phonetic = generatedSimplified;
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
                    IpaPhonetic = generatedIpa,
                    Phonetic = generatedSimplified
                };
                db.WordVariants.Add(variant);
            }
            else
            {
                variant.IpaPhonetic = generatedIpa;
                variant.Phonetic = generatedSimplified;
            }
        }

        await db.SaveChangesAsync();
        return (generatedIpa, generatedSimplified);
    }

    public async Task<bool> HasMultiplePronunciationsAsync(string word)
    {
        var wordEntry = await db.Words
            .Include(w => w.Variants)
            .FirstOrDefaultAsync(w => w.WordText.ToLower() == word.ToLower());

        return wordEntry?.HasMultiplePron ?? false;
    }

    private (string ipa, string simplified) GeneratePhonetics(string word)
    {
        // This is a very basic implementation
        // In a production environment, you would want to use a proper phonetic dictionary
        // or a machine learning model trained on IPA transcriptions

        // For now, we'll just do some basic substitutions
        var simplified = word.ToLower();
        var ipa = word.ToLower();

        // Example substitutions (this is highly simplified)
        var commonSounds = new Dictionary<string, (string ipa, string simplified)>
        {
            {"th", ("θ", "th")},
            {"ch", ("tʃ", "ch")},
            {"sh", ("ʃ", "sh")},
            {"ph", ("f", "f")},
            {"wh", ("w", "w")},
            {"ee", ("iː", "ee")},
            {"oo", ("uː", "oo")},
            {"ay", ("eɪ", "ay")},
            {"igh", ("aɪ", "ie")},
            {"ow", ("aʊ", "ow")}
        };

        foreach (var sound in commonSounds)
        {
            if (word.Contains(sound.Key, StringComparison.OrdinalIgnoreCase))
            {
                ipa = ipa.Replace(sound.Key, sound.Value.ipa, StringComparison.OrdinalIgnoreCase);
                simplified = simplified.Replace(sound.Key, sound.Value.simplified, StringComparison.OrdinalIgnoreCase);
            }
        }

        // Add stress marker to first syllable (simplified approach)
        if (ipa.Length > 0)
        {
            ipa = "ˈ" + ipa;
        }

        return (ipa, simplified);
    }
}
