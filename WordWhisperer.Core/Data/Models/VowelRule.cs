namespace WordWhisperer.Core.Data.Models;

public class VowelRule
{
    public string Default { get; set; } = "";
    public string Simplified { get; set; } = "";
    public Dictionary<string, string> Contexts { get; set; } = [];
} 