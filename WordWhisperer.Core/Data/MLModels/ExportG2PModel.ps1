#
# ExportG2PModel.ps1
# PowerShell script to download and export a G2P model to ONNX format for WordWhisperer
#

$ErrorActionPreference = "Stop"
$currentDir = $PSScriptRoot
$modelDir = $currentDir

# Create shorter temp directory to avoid Windows 260-character path limit issues
$tempDirBase = "C:\Temp"
if (-not (Test-Path $tempDirBase)) {
    New-Item -ItemType Directory -Path $tempDirBase | Out-Null
}
$tempDir = Join-Path $tempDirBase "WW_G2P"

Write-Host "Starting G2P Model Export Process..." -ForegroundColor Cyan
Write-Host "Working directory: $currentDir" -ForegroundColor Cyan
Write-Host "Temp directory: $tempDir" -ForegroundColor Cyan

# Clean up any existing temp directory
if (Test-Path $tempDir) {
    Remove-Item -Recurse -Force $tempDir
}
New-Item -ItemType Directory -Path $tempDir | Out-Null

# Check if Python is installed
try {
    $pythonVersion = python --version
    Write-Host "Found Python: $pythonVersion" -ForegroundColor Green
}
catch {
    Write-Host "Error: Python is not installed or not in PATH. Please install Python 3.8 or newer and try again." -ForegroundColor Red
    exit 1
}

# Create and activate a virtual environment
Write-Host "Creating Python virtual environment..." -ForegroundColor Cyan
python -m venv "$tempDir\venv"

# Activate the virtual environment
$activateScript = "$tempDir\venv\Scripts\Activate.ps1"
if (Test-Path $activateScript) {
    & $activateScript
    Write-Host "Virtual environment activated" -ForegroundColor Green
}
else {
    Write-Host "Error: Failed to create virtual environment" -ForegroundColor Red
    exit 1
}

# Install dependencies - ensure we have ALL required packages
Write-Host "Installing required Python packages..." -ForegroundColor Cyan
python -m pip install --upgrade pip wheel setuptools
python -m pip install numpy
python -m pip install torch torchvision torchaudio
python -m pip install speechbrain transformers tqdm onnx onnxruntime
python -m pip install huggingface_hub -U

# Create direct download script
$pythonScript = @"
import os
import sys
import torch
import numpy as np
from tqdm import tqdm
import transformers

# Ensure we have transformers
print(f"Using transformers version: {transformers.__version__}")

from speechbrain.pretrained import GraphemeToPhoneme

def export_model():
    output_dir = r'$modelDir'
    model_path = os.path.join(output_dir, 'g2p_model.onnx')
    char_to_input_path = os.path.join(output_dir, 'char_to_input.txt')
    index_to_ipa_path = os.path.join(output_dir, 'index_to_ipa.txt')
    
    print("Loading SpeechBrain G2P model...")
    g2p = GraphemeToPhoneme.from_hparams(source="speechbrain/soundchoice-g2p")
    print("Model loaded successfully.")
    
    # Get token mappings
    grapheme_encoder = g2p.tokenizer.grapheme_encoder
    phoneme_decoder = g2p.tokenizer.phoneme_decoder
    
    # Save character mappings
    print("Saving character to input mapping...")
    with open(char_to_input_path, 'w', encoding='utf-8') as f:
        f.write("# Character to input index mapping for SpeechBrain G2P model\n")
        for char, idx in sorted(grapheme_encoder.items()):
            f.write(f"{char}\t{idx}\n")
    
    print("Saving index to IPA mapping...")
    with open(index_to_ipa_path, 'w', encoding='utf-8') as f:
        f.write("# Output index to IPA mapping for SpeechBrain G2P model\n")
        for idx, phone in sorted(phoneme_decoder.items()):
            f.write(f"{idx}\t{phone}\n")
    
    # Define example input for ONNX export
    print("Preparing model for ONNX export...")
    model = g2p.mods.g2p_model
    model.eval()
    
    # Get a real input sample
    test_word = "hello"
    print(f"Testing model with word: {test_word}")
    result = g2p(test_word)
    print(f"Phonetic output: {result}")
    
    # Create dummy input for ONNX export - ensuring proper shape
    seq_len = 30  # Typical max sequence length
    dummy_input = torch.zeros(1, seq_len, dtype=torch.long)
    
    # Export to ONNX
    print(f"Exporting model to ONNX format at {model_path}...")
    torch.onnx.export(
        model,
        dummy_input,
        model_path,
        input_names=["input"],
        output_names=["output"],
        dynamic_axes={
            "input": {0: "batch_size", 1: "sequence_length"},
            "output": {0: "batch_size", 1: "sequence_length", 2: "vocab_size"}
        },
        opset_version=12,
        verbose=False
    )
    
    # Verify the model
    print("Verifying exported ONNX model...")
    import onnx
    onnx_model = onnx.load(model_path)
    onnx.checker.check_model(onnx_model)
    
    # Test with ONNX Runtime
    print("Testing with ONNX Runtime...")
    import onnxruntime as ort
    sess = ort.InferenceSession(model_path)
    ort_inputs = {"input": dummy_input.numpy()}
    ort_outputs = sess.run(None, ort_inputs)
    
    print(f"ONNX model size: {os.path.getsize(model_path) / 1024 / 1024:.2f} MB")
    print("Export successful!")
    
    # Create sample code for using the model
    print("Creating example code file...")
    with open(os.path.join(output_dir, 'example_usage.cs'), 'w', encoding='utf-8') as f:
        f.write("""// Example C# code for using the G2P ONNX model
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class G2PExample
{
    private InferenceSession _session;
    private Dictionary<string, int> _charToInput;
    private Dictionary<int, string> _indexToIpa;

    public G2PExample(string modelPath, string charMapPath, string ipaMapPath)
    {
        // Load the ONNX model
        _session = new InferenceSession(modelPath);
        
        // Load character mapping
        _charToInput = File.ReadAllLines(charMapPath)
            .Where(line => !line.StartsWith("#") && !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split('\t'))
            .ToDictionary(parts => parts[0], parts => int.Parse(parts[1]));
            
        // Load IPA mapping
        _indexToIpa = File.ReadAllLines(ipaMapPath)
            .Where(line => !line.StartsWith("#") && !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split('\t'))
            .ToDictionary(parts => int.Parse(parts[0]), parts => parts[1]);
    }
    
    public string TranscribeWord(string word)
    {
        // Convert input word to indices
        var inputIndices = new List<long>();
        foreach (char c in word.ToLower())
        {
            string charStr = c.ToString();
            if (_charToInput.TryGetValue(charStr, out int index))
            {
                inputIndices.Add(index);
            }
            else
            {
                // Use a default index for unknown characters
                inputIndices.Add(0);
            }
        }
        
        // Pad to sequence length
        while (inputIndices.Count < 30)
        {
            inputIndices.Add(0);
        }
        
        // Create input tensor
        var inputTensor = new DenseTensor<long>(new[] { 1, inputIndices.Count });
        for (int i = 0; i < inputIndices.Count; i++)
        {
            inputTensor[0, i] = inputIndices[i];
        }
        
        // Run inference
        var inputs = new Dictionary<string, OnnxValue>
        {
            { "input", OnnxValue.CreateFromTensor(inputTensor) }
        };
        
        using var outputs = _session.Run(inputs);
        var output = outputs.First().AsTensor<float>();
        
        // Find most likely phonemes
        var phonemes = new List<string>();
        
        // For each position, get the highest probability phoneme
        for (int pos = 0; pos < word.Length; pos++)
        {
            float maxProb = float.MinValue;
            int bestIndex = 0;
            
            for (int i = 0; i < output.Dimensions[2]; i++)
            {
                float prob = output[0, pos, i];
                if (prob > maxProb)
                {
                    maxProb = prob;
                    bestIndex = i;
                }
            }
            
            if (_indexToIpa.TryGetValue(bestIndex, out string phoneme))
            {
                phonemes.Add(phoneme);
            }
        }
        
        return string.Join("", phonemes);
    }
}
""");
    
    print("ONNX model and mapping files successfully exported!")
    print(f"- ONNX model: {model_path}")
    print(f"- Character map: {char_to_input_path}")
    print(f"- IPA map: {index_to_ipa_path}")
    print(f"- Example usage: {os.path.join(output_dir, 'example_usage.cs')}")
    
    return True

if __name__ == "__main__":
    try:
        success = export_model()
        sys.exit(0 if success else 1)
    except Exception as e:
        print(f"Error: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
"@

# Save the Python script
$scriptPath = Join-Path $tempDir "export_model.py"
$pythonScript | Out-File -FilePath $scriptPath -Encoding utf8

# Run the Python script
Write-Host "Exporting G2P model (this may take a few minutes)..." -ForegroundColor Cyan
try {
    & python $scriptPath
    
    # Check for successful export
    $onnxModelPath = Join-Path $modelDir "g2p_model.onnx"
    $charMapPath = Join-Path $modelDir "char_to_input.txt" 
    $ipaMapPath = Join-Path $modelDir "index_to_ipa.txt"
    
    if ((Test-Path $onnxModelPath) -and (Test-Path $charMapPath) -and (Test-Path $ipaMapPath)) {
        Write-Host "Model export successful!" -ForegroundColor Green
        Write-Host "The following files have been created:" -ForegroundColor Green
        Write-Host "- g2p_model.onnx (ONNX model for inference)" -ForegroundColor Green
        Write-Host "- char_to_input.txt (Character mapping)" -ForegroundColor Green
        Write-Host "- index_to_ipa.txt (IPA mapping)" -ForegroundColor Green
        Write-Host "- example_usage.cs (Example C# code)" -ForegroundColor Green
    }
    else {
        Write-Host "Error: Model export failed. See above for errors." -ForegroundColor Red
    }
}
catch {
    Write-Host "Error running export script: $_" -ForegroundColor Red
}

# Deactivate the virtual environment
if ($env:VIRTUAL_ENV) {
    deactivate
}

# Clean up temporary files
Write-Host "Cleaning up temporary files..." -ForegroundColor Cyan
if (Test-Path $tempDir) {
    Remove-Item -Recurse -Force $tempDir
}

Write-Host "Process complete." -ForegroundColor Cyan
Write-Host ""
Write-Host "The ONNX model is ready to use with your MLPhoneticService." -ForegroundColor Green
Write-Host "1. The G2P model is in $onnxModelPath" -ForegroundColor Yellow
Write-Host "2. Make sure your PhoneticServiceConfig points to these files" -ForegroundColor Yellow
Write-Host "3. Set UseMachineLearning = true in your application configuration" -ForegroundColor Yellow
