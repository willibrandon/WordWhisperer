namespace WordWhisperer.Core.Data.Models;

public class Setting
{
    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }

    public string? Description { get; set; }
}
