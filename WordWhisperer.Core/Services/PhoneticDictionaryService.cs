using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WordWhisperer.Core.Data.Models;

namespace WordWhisperer.Core.Services;

public class PhoneticDictionaryService(ILogger<PhoneticDictionaryService> logger)
{
    private Dictionary<string, PhoneticEntry>? _entries;
    private PhoneticRules? _rules;

    public async Task InitializeAsync()
    {
        try
        {
            var dictionaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "PhoneticDictionary.json");
            var jsonContent = await File.ReadAllTextAsync(dictionaryPath);
            var dictionary = JsonSerializer.Deserialize<PhoneticDictionary>(jsonContent);

            _entries = dictionary?.Entries ?? [];
            _rules = dictionary?.Rules;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load phonetic dictionary");
            _entries = [];
            _rules = new PhoneticRules();
        }
    }

    public (string ipa, string simplified)? GetPhonetics(string word, string accent = "american")
    {
        if (_entries == null)
        {
            logger.LogWarning("Phonetic dictionary not initialized");
            return null;
        }

        word = word.ToLower();
        if (_entries.TryGetValue(word, out var entry))
        {
            if (accent != "american" && entry.Variants?.TryGetValue(accent, out var variant) == true)
            {
                return (variant.Ipa, variant.Simplified);
            }
            return (entry.Ipa, entry.Simplified);
        }

        return GeneratePhonetics(word);
    }

    private (string ipa, string simplified)? GeneratePhonetics(string word)
    {
        if (_rules == null)
        {
            logger.LogWarning("Phonetic rules not initialized");
            return null;
        }

        try
        {
            var (syllables, stressPattern) = AnalyzeSyllables(word);
            var ipaBuilder = new List<string>();
            var simplifiedBuilder = new List<string>();

            for (var i = 0; i < syllables.Count; i++)
            {
                var syllable = syllables[i];
                var (ipaPhonemes, simplifiedPhonemes) = TranscribeSyllable(syllable);
                
                if (stressPattern[i] == 1)
                {
                    ipaBuilder.Add("ˈ" + ipaPhonemes);
                    simplifiedBuilder.Add(simplifiedPhonemes.ToUpper());
                }
                else
                {
                    ipaBuilder.Add(ipaPhonemes);
                    simplifiedBuilder.Add(simplifiedPhonemes.ToLower());
                }
            }

            return (
                string.Join("", ipaBuilder),
                string.Join("-", simplifiedBuilder)
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate phonetics for word: {Word}", word);
            return null;
        }
    }

    private (List<string> syllables, List<int> stressPattern) AnalyzeSyllables(string word)
    {
        // This is a simplified syllable analysis
        // In a production environment, you would want to use a more sophisticated algorithm
        var syllables = new List<string>();
        var currentSyllable = "";
        var vowelCount = 0;

        for (var i = 0; i < word.Length; i++)
        {
            var c = word[i];
            currentSyllable += c;

            if (IsVowel(c))
            {
                vowelCount++;
                if (i < word.Length - 1 && !IsVowel(word[i + 1]))
                {
                    if (currentSyllable.Length > 0)
                    {
                        syllables.Add(currentSyllable);
                        currentSyllable = "";
                    }
                }
            }
        }

        if (currentSyllable.Length > 0)
        {
            syllables.Add(currentSyllable);
        }

        // Apply stress pattern based on syllable count
        var stressPattern = syllables.Count switch
        {
            1 => [1],
            2 => _rules?.StressPatterns.NounTwoSyllable.ToList() ?? [1, 0],
            3 => _rules?.StressPatterns.ThreeSyllable.ToList() ?? [1, 0, 0],
            _ => [.. Enumerable.Repeat(0, syllables.Count)]
        };
        stressPattern[0] = 1; // Default stress on first syllable for unknown patterns

        return (syllables, stressPattern);
    }

    private (string ipa, string simplified) TranscribeSyllable(string syllable)
    {
        var ipaBuilder = new StringBuilder();
        var simplifiedBuilder = new StringBuilder();
        var i = 0;

        while (i < syllable.Length)
        {
            var success = false;

            // Try multi-character consonants first
            if (i < syllable.Length - 1)
            {
                var digraph = syllable.Substring(i, 2);
                if (_rules?.Consonants.TryGetValue(digraph, out var consonantRule) == true)
                {
                    ipaBuilder.Append(consonantRule.Default);
                    simplifiedBuilder.Append(consonantRule.Simplified);
                    i += 2;
                    success = true;
                }
            }

            if (!success)
            {
                var c = syllable[i].ToString();

                if (_rules?.Vowels.TryGetValue(c, out var vowelRule) == true)
                {
                    // Check for special vowel contexts
                    var context = GetVowelContext(syllable, i);
                    if (context != null && vowelRule.Contexts.TryGetValue(context, out var contextualSound))
                    {
                        ipaBuilder.Append(contextualSound);
                    }
                    else
                    {
                        ipaBuilder.Append(vowelRule.Default);
                    }

                    simplifiedBuilder.Append(vowelRule.Simplified);
                }
                else
                {
                    // Default to the character itself for unknown sounds
                    ipaBuilder.Append(c);
                    simplifiedBuilder.Append(c);
                }

                i++;
            }
        }

        return (ipaBuilder.ToString(), simplifiedBuilder.ToString());
    }

    private static string? GetVowelContext(string syllable, int position)
    {
        if (position == syllable.Length - 2 && syllable[position + 1] == 'e')
            return "silent_e";
        if (position < syllable.Length - 1 && syllable[position + 1] == 'r')
            return "before_r";
        if (position < syllable.Length - 1 && syllable[position] == syllable[position + 1])
            return syllable[position] + syllable[position].ToString();
        return null;
    }

    private static bool IsVowel(char c) => "aeiouAEIOU".Contains(c);
}
