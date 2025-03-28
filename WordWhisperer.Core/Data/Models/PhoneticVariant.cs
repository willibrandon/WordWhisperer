namespace WordWhisperer.Core.Data.Models;

public class PhoneticVariant
{
    public string Ipa { get; set; } = "";
    public string Simplified { get; set; } = "";
    public List<string> Syllables { get; set; } = [];
    public List<int> Stress { get; set; } = [];
} 