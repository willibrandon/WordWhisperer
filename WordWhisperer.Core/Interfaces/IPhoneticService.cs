namespace WordWhisperer.Core.Interfaces;

public interface IPhoneticService
{
    Task<(string ipa, string simplified)?> GetOrGeneratePhoneticsAsync(string word, string accent);
    Task<bool> HasMultiplePronunciationsAsync(string word);
} 