namespace WordWhisperer.Core.Data.Models;

public class WordVariant
{
    public int Id { get; set; }
    public int WordId { get; set; }
    public Word? Word { get; set; }
    public string Variant { get; set; } = string.Empty;
    public string? Phonetic { get; set; }
    public string? IpaPhonetic { get; set; }
    public string? AudioPath { get; set; }
} 