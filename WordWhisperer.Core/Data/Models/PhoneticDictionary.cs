namespace WordWhisperer.Core.Data.Models;

public class PhoneticDictionary
{
    public string Version { get; set; } = "";

    public string Source { get; set; } = "";

    public Dictionary<string, PhoneticEntry> Entries { get; set; } = [];

    public PhoneticRules Rules { get; set; } = new();
}
