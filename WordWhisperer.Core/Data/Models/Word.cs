namespace WordWhisperer.Core.Data.Models;

public class Word
{
    public int Id { get; set; }

    public string WordText { get; set; } = string.Empty;

    public string? Phonetic { get; set; }

    public string? IpaPhonetic { get; set; }

    public string? AudioPath { get; set; }

    public string? Definition { get; set; }

    public string? PartOfSpeech { get; set; }

    public string? Source { get; set; }

    public bool IsGenerated { get; set; }

    public bool HasMultiplePron { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

    public int AccessCount { get; set; }

    // Navigation properties
    public ICollection<WordVariant> Variants { get; set; } = new List<WordVariant>();

    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public ICollection<History> History { get; set; } = new List<History>();
}
