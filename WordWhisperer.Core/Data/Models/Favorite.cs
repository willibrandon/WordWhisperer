namespace WordWhisperer.Core.Data.Models;

public class Favorite
{
    public int Id { get; set; }
    public int WordId { get; set; }
    public string? Notes { get; set; }
    public string? Tags { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Word? Word { get; set; }
} 