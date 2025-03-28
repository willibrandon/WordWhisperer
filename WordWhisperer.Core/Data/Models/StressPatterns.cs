namespace WordWhisperer.Core.Data.Models;

public class StressPatterns
{
    public int[] NounTwoSyllable { get; set; } = [1, 0];

    public int[] VerbTwoSyllable { get; set; } = [0, 1];

    public int[] ThreeSyllable { get; set; } = [1, 0, 0];
}
