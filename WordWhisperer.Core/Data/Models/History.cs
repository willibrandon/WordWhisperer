namespace WordWhisperer.Core.Data.Models;

public class History
{
    public int Id { get; set; }
    public int WordId { get; set; }
    public Word? Word { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? AccentUsed { get; set; }
} 