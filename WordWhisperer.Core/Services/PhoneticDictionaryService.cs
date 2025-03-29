using System.Text;
using Microsoft.Extensions.Logging;

namespace WordWhisperer.Core.Services;

public class PhoneticDictionaryService(ILogger<PhoneticDictionaryService> logger)
{
    private Dictionary<string, string>? _entries;

    public async Task InitializeAsync()
    {
        try
        {
            var dictionaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "MLModels", "cmudict.txt");
            var charToInputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "MLModels", "char_to_input.txt");
            var indexToIpaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "MLModels", "index_to_ipa.txt");

            logger.LogInformation("Loading CMU dictionary from {Path}", dictionaryPath);
            _entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Read CMU dictionary file
            var lines = await File.ReadAllLinesAsync(dictionaryPath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";;;"))
                    continue;

                // Split on any number of spaces and remove empty entries
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)  // Need at least a word and one phoneme
                    continue;

                var word = parts[0].ToLowerInvariant();
                // Remove the variant number if present (e.g., "WORD(2)" becomes "WORD")
                word = word.Split('(')[0];
                
                // Join all remaining parts as the pronunciation
                var phonemes = string.Join(" ", parts.Skip(1));

                _entries[word] = phonemes;
            }

            logger.LogInformation("Loaded {Count} words from CMU dictionary", _entries.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load CMU dictionary");
            _entries = [];
        }
    }

    public (string ipa, string simplified)? GetPhonetics(string word, string accent = "american")
    {
        if (_entries == null)
        {
            logger.LogWarning("CMU dictionary not initialized");
            return null;
        }

        word = word.ToLower();
        if (_entries.TryGetValue(word, out var cmuPhonemes))
        {
            // Convert CMU phonemes to IPA and simplified formats
            return ConvertCmuToPhonetics(cmuPhonemes);
        }

        logger.LogWarning("Word '{Word}' not found in CMU dictionary", word);
        return null;
    }

    private static (string ipa, string simplified) ConvertCmuToPhonetics(string cmuPhonemes)
    {
        var ipaBuilder = new StringBuilder();
        var simplifiedBuilder = new StringBuilder();
        var phonemes = cmuPhonemes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var isFirstSyllable = true;

        foreach (var phoneme in phonemes)
        {
            // Check for stress marker
            if (phoneme.EndsWith("1"))
            {
                if (!isFirstSyllable)
                {
                    ipaBuilder.Append('ˈ');
                    simplifiedBuilder.Append('-');
                }
                isFirstSyllable = false;
            }
            else if (phoneme.EndsWith("2"))
            {
                if (!isFirstSyllable)
                {
                    ipaBuilder.Append('ˌ');
                    simplifiedBuilder.Append('-');
                }
                isFirstSyllable = false;
            }
            else if (!isFirstSyllable)
            {
                simplifiedBuilder.Append('-');
            }

            // Convert CMU phoneme to IPA
            var basePhoneme = phoneme.TrimEnd('0', '1', '2');
            var (ipa, simplified) = ConvertPhoneme(basePhoneme);
            ipaBuilder.Append(ipa);
            simplifiedBuilder.Append(simplified);
            isFirstSyllable = false;
        }

        return (ipaBuilder.ToString(), simplifiedBuilder.ToString());
    }

    private static (string ipa, string simplified) ConvertPhoneme(string cmuPhoneme) => cmuPhoneme switch
    {
        "AA" => ("ɑ", "ah"),
        "AE" => ("æ", "a"),
        "AH" => ("ʌ", "uh"),
        "AO" => ("ɔ", "aw"),
        "AW" => ("aʊ", "ow"),
        "AY" => ("aɪ", "ai"),
        "B" => ("b", "b"),
        "CH" => ("tʃ", "ch"),
        "D" => ("d", "d"),
        "DH" => ("ð", "th"),
        "EH" => ("ɛ", "eh"),
        "ER" => ("ɝ", "er"),
        "EY" => ("eɪ", "ay"),
        "F" => ("f", "f"),
        "G" => ("ɡ", "g"),
        "HH" => ("h", "h"),
        "IH" => ("ɪ", "ih"),
        "IY" => ("i", "ee"),
        "JH" => ("dʒ", "j"),
        "K" => ("k", "k"),
        "L" => ("l", "l"),
        "M" => ("m", "m"),
        "N" => ("n", "n"),
        "NG" => ("ŋ", "ng"),
        "OW" => ("oʊ", "oh"),
        "OY" => ("ɔɪ", "oy"),
        "P" => ("p", "p"),
        "R" => ("ɹ", "r"),
        "S" => ("s", "s"),
        "SH" => ("ʃ", "sh"),
        "T" => ("t", "t"),
        "TH" => ("θ", "th"),
        "UH" => ("ʊ", "oo"),
        "UW" => ("u", "oo"),
        "V" => ("v", "v"),
        "W" => ("w", "w"),
        "Y" => ("j", "y"),
        "Z" => ("z", "z"),
        "ZH" => ("ʒ", "zh"),
        _ => (cmuPhoneme.ToLowerInvariant(), cmuPhoneme.ToLowerInvariant())
    };
}
