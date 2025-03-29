using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using WordWhisperer.Core.Models;

namespace WordWhisperer.Core.Services;

/// <summary>
/// Machine learning based phonetic transcription service using ONNX models
/// </summary>
public class MLPhoneticService : IDisposable
{
    private readonly ILogger<MLPhoneticService> _logger;
    private readonly Dictionary<string, char> _indexToIpaMap;
    private readonly Dictionary<char, long> _charToInputMap;
    private InferenceSession? _inferenceSession;
    private readonly string _modelPath;
    private bool _initialized;
    private readonly Dictionary<string, (string ipa, string simplified)> _cmuDictionary;

    public MLPhoneticService(ILogger<MLPhoneticService> logger)
    {
        _logger = logger;
        _modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "MLModels", "g2p_model.onnx");
        _indexToIpaMap = new Dictionary<string, char>();
        _charToInputMap = new Dictionary<char, long>();
        _cmuDictionary = new Dictionary<string, (string ipa, string simplified)>(StringComparer.OrdinalIgnoreCase);
        _initialized = false;
    }

    /// <summary>
    /// Initialize the ML model and dictionaries
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // Load the CMU dictionary for exact lookups
            await LoadCmuDictionaryAsync();

            // Load character mappings
            await LoadCharacterMappingsAsync();

            // Create ONNX inference session
            if (File.Exists(_modelPath))
            {
                var sessionOptions = new SessionOptions
                {
                    ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
                };
                _inferenceSession = new InferenceSession(_modelPath, sessionOptions);
                _initialized = true;
                _logger.LogInformation("ML Phonetic Service successfully initialized with model: {ModelPath}", _modelPath);
            }
            else
            {
                _logger.LogWarning("G2P model not found at path: {ModelPath}", _modelPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ML Phonetic Service");
        }
    }

    /// <summary>
    /// Transcribe a word to phonetic notation using the ML model
    /// </summary>
    public (string ipa, string simplified)? TranscribeWord(string word, string accent = "american")
    {
        // First check the CMU dictionary for exact matches
        if (_cmuDictionary.TryGetValue(word, out var phonetics))
        {
            // Apply accent-specific modifications if needed
            return ApplyAccentSpecificModifications(phonetics, accent);
        }

        // If word is not in dictionary, use ML model
        if (!_initialized || _inferenceSession == null)
        {
            _logger.LogWarning("ML Phonetic Service not initialized, cannot transcribe word: {Word}", word);
            return null;
        }

        try
        {
            // Preprocess input word - lowercase and remove non-alphanumeric
            word = word.ToLowerInvariant().Trim();
            
            // Convert word to input tensor
            var inputTensor = CreateInputTensor(word);
            
            // Get model outputs
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input", inputTensor) };
            using var outputs = _inferenceSession.Run(inputs);
            
            // Process outputs to get IPA
            var outputTensor = outputs.First().AsTensor<float>();
            var ipaOutput = ProcessOutputTensor(outputTensor, word.Length);
            
            // Generate simplified phonetics from IPA
            var simplified = SimplifyIpa(ipaOutput);
            
            // Apply accent-specific modifications to the generated phonetics
            return ApplyAccentSpecificModifications((ipaOutput, simplified), accent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing word with ML model: {Word}", word);
            return null;
        }
    }

    /// <summary>
    /// Convert word to input tensor for the ML model
    /// </summary>
    private DenseTensor<float> CreateInputTensor(string word)
    {
        // Setup input tensor with shape [1, max_length]
        var maxLength = 30; // Maximum word length the model can handle
        var inputTensor = new DenseTensor<float>(new[] { 1, maxLength });
        
        // Convert each character to its corresponding index in the vocabulary
        for (int i = 0; i < Math.Min(word.Length, maxLength); i++)
        {
            if (_charToInputMap.TryGetValue(word[i], out var index))
            {
                inputTensor[0, i] = index;
            }
            else
            {
                // Use unknown character token
                inputTensor[0, i] = 0;
            }
        }
        
        return inputTensor;
    }

    /// <summary>
    /// Process model output tensor to get IPA string
    /// </summary>
    private string ProcessOutputTensor(Tensor<float> outputTensor, int inputLength)
    {
        var ipaBuilder = new StringBuilder();
        var outputLength = Math.Min(inputLength * 2, outputTensor.Dimensions[1]); // Phonemes typically longer than chars
        
        for (int i = 0; i < outputLength; i++)
        {
            // Get the index of the max value
            var maxValue = float.MinValue;
            var maxIndex = -1;
            
            for (int j = 0; j < outputTensor.Dimensions[2]; j++)
            {
                var value = outputTensor[0, i, j];
                if (value > maxValue)
                {
                    maxValue = value;
                    maxIndex = j;
                }
            }
            
            // Skip if it's a padding token or end of sequence
            if (maxIndex == 0 || maxIndex == -1)
            {
                continue;
            }
            
            // Convert index to IPA character and append to result
            if (_indexToIpaMap.TryGetValue(maxIndex.ToString(), out var ipaChar))
            {
                ipaBuilder.Append(ipaChar);
            }
        }
        
        return ipaBuilder.ToString();
    }

    /// <summary>
    /// Generate simplified phonetic notation from IPA
    /// </summary>
    private string SimplifyIpa(string ipa)
    {
        var simplified = new StringBuilder();
        var syllables = SplitIntoSyllables(ipa);
        
        for (int i = 0; i < syllables.Count; i++)
        {
            var syllable = syllables[i];
            var isStressed = syllable.Contains('ˈ');
            
            // Remove stress mark
            syllable = syllable.Replace("ˈ", "").Replace("ˌ", "");
            
            // Apply simplified representation rules
            var simplifiedSyllable = ConvertIpaToSimplified(syllable);
            
            // Apply stress (uppercase for primary stress)
            if (isStressed)
            {
                simplified.Append(simplifiedSyllable.ToUpperInvariant());
            }
            else
            {
                simplified.Append(simplifiedSyllable.ToLowerInvariant());
            }
            
            // Add syllable separator
            if (i < syllables.Count - 1)
            {
                simplified.Append('-');
            }
        }
        
        return simplified.ToString();
    }

    /// <summary>
    /// Convert IPA characters to simplified phonetic notation
    /// </summary>
    private string ConvertIpaToSimplified(string ipa)
    {
        // Common IPA to simplified mapping
        var ipaMap = new Dictionary<string, string>
        {
            { "ɑ", "ah" }, { "æ", "ae" }, { "ə", "uh" }, { "ɔ", "aw" },
            { "e", "ey" }, { "ɛ", "eh" }, { "i", "ee" }, { "ɪ", "ih" },
            { "o", "oh" }, { "u", "oo" }, { "ʊ", "uu" }, { "ʌ", "uh" },
            { "aɪ", "ai" }, { "aʊ", "au" }, { "ɔɪ", "oi" }, { "eɪ", "ei" },
            { "oʊ", "ou" }, { "tʃ", "ch" }, { "dʒ", "j" }, { "ʃ", "sh" },
            { "ʒ", "zh" }, { "θ", "th" }, { "ð", "th" }, { "ŋ", "ng" }
        };
        
        var result = ipa;
        foreach (var mapping in ipaMap)
        {
            result = result.Replace(mapping.Key, mapping.Value);
        }
        
        return result;
    }

    /// <summary>
    /// Split IPA string into syllables (basic implementation)
    /// </summary>
    private List<string> SplitIntoSyllables(string ipa)
    {
        // A very basic syllable splitting algorithm
        // In a real implementation, this would be more sophisticated
        var syllables = new List<string>();
        var currentSyllable = new StringBuilder();
        
        for (int i = 0; i < ipa.Length; i++)
        {
            // Add stress marks to the next syllable
            if (ipa[i] == 'ˈ' || ipa[i] == 'ˌ')
            {
                currentSyllable.Append(ipa[i]);
                continue;
            }
            
            currentSyllable.Append(ipa[i]);
            
            // Look for syllable boundaries
            if (i < ipa.Length - 2 && IsVowel(ipa[i]) && !IsVowel(ipa[i+1]) && IsVowel(ipa[i+2]))
            {
                // Split after consonant between vowels (CV.CV)
                currentSyllable.Append(ipa[i+1]);
                syllables.Add(currentSyllable.ToString());
                currentSyllable.Clear();
                i++;
            }
            else if (i == ipa.Length - 1)
            {
                syllables.Add(currentSyllable.ToString());
            }
        }
        
        // If we didn't split into syllables, treat the whole word as one syllable
        if (syllables.Count == 0 && currentSyllable.Length > 0)
        {
            syllables.Add(currentSyllable.ToString());
        }
        
        return syllables;
    }

    /// <summary>
    /// Check if a character is an IPA vowel
    /// </summary>
    private bool IsVowel(char c)
    {
        return "ɑæəɔeɛiɪoɒuʊʌaɪaʊɔɪeɪoʊ".Contains(c);
    }

    /// <summary>
    /// Load CMU dictionary for fast lookups
    /// </summary>
    private async Task LoadCmuDictionaryAsync()
    {
        try
        {
            var dictionaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "MLModels", "cmudict.txt");
            if (!File.Exists(dictionaryPath))
            {
                _logger.LogWarning("CMU Dictionary not found at path: {DictionaryPath}", dictionaryPath);
                return;
            }

            var lines = await File.ReadAllLinesAsync(dictionaryPath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";;;"))
                    continue;

                var parts = line.Split("  ", 2);
                if (parts.Length != 2)
                    continue;

                var word = parts[0].ToLowerInvariant();
                var arpabetPhonemes = parts[1].Trim();
                
                // Convert ARPABET to IPA and simplified
                var ipa = ConvertArpabetToIpa(arpabetPhonemes);
                var simplified = SimplifyIpa(ipa);
                
                // Add to dictionary (ignore words with () for variants)
                if (!word.Contains('('))
                {
                    _cmuDictionary[word] = (ipa, simplified);
                }
            }
            
            _logger.LogInformation("Loaded {Count} words from CMU Dictionary", _cmuDictionary.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load CMU Dictionary");
        }
    }

    /// <summary>
    /// Convert ARPABET phoneme notation to IPA
    /// </summary>
    private string ConvertArpabetToIpa(string arpabet)
    {
        var arpabetToIpa = new Dictionary<string, string>
        {
            { "AA0", "ɑ" }, { "AA1", "ˈɑ" }, { "AA2", "ˌɑ" },
            { "AE0", "æ" }, { "AE1", "ˈæ" }, { "AE2", "ˌæ" },
            { "AH0", "ə" }, { "AH1", "ˈʌ" }, { "AH2", "ˌʌ" },
            { "AO0", "ɔ" }, { "AO1", "ˈɔ" }, { "AO2", "ˌɔ" },
            { "AW0", "aʊ" }, { "AW1", "ˈaʊ" }, { "AW2", "ˌaʊ" },
            { "AY0", "aɪ" }, { "AY1", "ˈaɪ" }, { "AY2", "ˌaɪ" },
            { "B", "b" }, { "CH", "tʃ" }, { "D", "d" }, { "DH", "ð" },
            { "EH0", "ɛ" }, { "EH1", "ˈɛ" }, { "EH2", "ˌɛ" },
            { "ER0", "ɚ" }, { "ER1", "ˈɝ" }, { "ER2", "ˌɝ" },
            { "EY0", "eɪ" }, { "EY1", "ˈeɪ" }, { "EY2", "ˌeɪ" },
            { "F", "f" }, { "G", "ɡ" }, { "HH", "h" },
            { "IH0", "ɪ" }, { "IH1", "ˈɪ" }, { "IH2", "ˌɪ" },
            { "IY0", "i" }, { "IY1", "ˈi" }, { "IY2", "ˌi" },
            { "JH", "dʒ" }, { "K", "k" }, { "L", "l" }, { "M", "m" },
            { "N", "n" }, { "NG", "ŋ" }, { "OW0", "oʊ" }, { "OW1", "ˈoʊ" },
            { "OW2", "ˌoʊ" }, { "OY0", "ɔɪ" }, { "OY1", "ˈɔɪ" }, { "OY2", "ˌɔɪ" },
            { "P", "p" }, { "R", "ɹ" }, { "S", "s" }, { "SH", "ʃ" },
            { "T", "t" }, { "TH", "θ" }, { "UH0", "ʊ" }, { "UH1", "ˈʊ" },
            { "UH2", "ˌʊ" }, { "UW0", "u" }, { "UW1", "ˈu" }, { "UW2", "ˌu" },
            { "V", "v" }, { "W", "w" }, { "Y", "j" }, { "Z", "z" }, { "ZH", "ʒ" }
        };

        var arpabetPhonemes = arpabet.Split(' ');
        var ipaBuilder = new StringBuilder();
        
        foreach (var phoneme in arpabetPhonemes)
        {
            if (arpabetToIpa.TryGetValue(phoneme, out var ipaPhoneme))
            {
                ipaBuilder.Append(ipaPhoneme);
            }
            else
            {
                // Handle unknown phoneme by passing it through
                ipaBuilder.Append(phoneme);
            }
        }
        
        return ipaBuilder.ToString();
    }

    /// <summary>
    /// Load character mappings for model input and output
    /// </summary>
    private async Task LoadCharacterMappingsAsync()
    {
        try
        {
            var inputMapPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "MLModels", "char_to_input.txt");
            var outputMapPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "MLModels", "index_to_ipa.txt");
            
            if (!File.Exists(inputMapPath) || !File.Exists(outputMapPath))
            {
                _logger.LogWarning("Character mapping files not found");
                return;
            }
            
            // Load input character mapping
            var inputLines = await File.ReadAllLinesAsync(inputMapPath);
            foreach (var line in inputLines)
            {
                var parts = line.Split('\t');
                if (parts.Length == 2 && parts[0].Length == 1 && long.TryParse(parts[1], out var index))
                {
                    _charToInputMap[parts[0][0]] = index;
                }
            }
            
            // Load output index to IPA mapping
            var outputLines = await File.ReadAllLinesAsync(outputMapPath);
            foreach (var line in outputLines)
            {
                var parts = line.Split('\t');
                if (parts.Length == 2 && parts[1].Length >= 1)
                {
                    _indexToIpaMap[parts[0]] = parts[1][0];
                }
            }
            
            _logger.LogInformation("Loaded character mappings: {InputCount} input chars, {OutputCount} output chars", 
                _charToInputMap.Count, _indexToIpaMap.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load character mappings");
        }
    }

    /// <summary>
    /// Apply accent-specific modifications to the phonetic output
    /// </summary>
    private (string ipa, string simplified) ApplyAccentSpecificModifications((string ipa, string simplified) phonetics, string accent)
    {
        string ipa = phonetics.ipa;
        string simplified = phonetics.simplified;

        switch (accent.ToLowerInvariant())
        {
            case "british":
                // Convert rhotic "r" (post-vocalic) - common in American English
                // but typically dropped in non-rhotic British accents
                ipa = ipa.Replace("ɚ", "ə").Replace("ɝ", "ɜː");
                
                // Convert American diphthongs to British ones
                ipa = ipa.Replace("oʊ", "əʊ");
                
                // Add yod before "u" in certain contexts (like "new")
                ipa = AddYodBeforeU(ipa);
                
                // Adjust simplified notation
                simplified = AccentAdjustSimplified(simplified, "british");
                break;

            case "australian":
                // Australian specific changes 
                // (subset of British with some unique characteristics)
                ipa = ipa.Replace("oʊ", "əʊ");
                
                // Australian vowel shifts
                ipa = ipa.Replace("æ", "æː");
                
                // Adjust simplified notation
                simplified = AccentAdjustSimplified(simplified, "australian");
                break;
                
            // Default is American - no specific changes needed
        }

        return (ipa, simplified);
    }

    /// <summary>
    /// Add yod (j) sound before long u in British pronunciations
    /// </summary>
    private string AddYodBeforeU(string ipa)
    {
        // This is a simplified approach - would need more sophisticated
        // phonological rules for a complete solution
        return ipa
            .Replace("ˈnu", "ˈnju")
            .Replace("ˈtu", "ˈtju")
            .Replace("ˈdu", "ˈdju");
    }

    /// <summary>
    /// Adjust simplified phonetic notation for different accents
    /// </summary>
    private string AccentAdjustSimplified(string simplified, string accent)
    {
        switch (accent)
        {
            case "british":
                // Adjust ou/ow sounds
                simplified = simplified.Replace("-OW-", "-OH-");
                simplified = simplified.Replace("-ow-", "-oh-");
                return simplified;
                
            case "australian":
                // Specific Australian adjustments
                return simplified;
                
            default:
                return simplified;
        }
    }

    public void Dispose()
    {
        _inferenceSession?.Dispose();
    }
}
