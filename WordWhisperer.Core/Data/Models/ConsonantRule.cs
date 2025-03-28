namespace WordWhisperer.Core.Data.Models;

public class ConsonantRule
{
    public string Default { get; set; } = "";
    public string? Voiced { get; set; }
    public string? Unvoiced { get; set; }
    public string Simplified { get; set; } = "";
} 