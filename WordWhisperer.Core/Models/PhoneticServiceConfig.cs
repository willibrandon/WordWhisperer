namespace WordWhisperer.Core.Models;

/// <summary>
/// Configuration options for phonetic transcription services
/// </summary>
public class PhoneticServiceConfig
{
    /// <summary>
    /// Whether to use the ML-based phonetic service as the primary transcription method
    /// </summary>
    public bool UseMachineLearning { get; set; } = true;
    
    /// <summary>
    /// Path to the G2P ONNX model
    /// </summary>
    public string ModelPath { get; set; } = "Data/Models/g2p_model.onnx";
    
    /// <summary>
    /// Path to the CMU dictionary file
    /// </summary>
    public string CmuDictionaryPath { get; set; } = "Data/cmudict.txt";
    
    /// <summary>
    /// Maximum word length for ML processing
    /// </summary>
    public int MaxWordLength { get; set; } = 30;
}
