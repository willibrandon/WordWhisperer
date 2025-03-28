namespace WordWhisperer.Core.Data.Models;

public class PhoneticRules
{
    public Dictionary<string, ConsonantRule> Consonants { get; set; } = [];
    public Dictionary<string, VowelRule> Vowels { get; set; } = [];
    public Dictionary<string, SyllablePattern> SyllablePatterns { get; set; } = [];
    public StressPatterns StressPatterns { get; set; } = new();
} 