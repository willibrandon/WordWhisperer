# Machine Learning Models for Phonetic Transcription

This directory contains the machine learning models and data files for the phonetic transcription service in Word Whisperer.

## Required Files

1. `g2p_model.onnx` - ONNX model for Grapheme-to-Phoneme conversion
2. `cmudict.txt` - CMU Pronouncing Dictionary
3. `char_to_input.txt` - Character mapping for model input
4. `index_to_ipa.txt` - Index to IPA mapping for model output

## Setting Up

### CMU Dictionary

You can download the CMU Pronouncing Dictionary from:
https://github.com/cmusphinx/cmudict/blob/master/cmudict.dict

After downloading, place it in this directory as `cmudict.txt`.

### Grapheme-to-Phoneme (G2P) Model

Several options are available for G2P models:

1. SpeechBrain's SoundChoice G2P (Apache 2.0 license):
   - https://huggingface.co/speechbrain/soundchoice-g2p

2. NVIDIA NeMo G2P models:
   - Conformer-CTC (smaller, faster): https://huggingface.co/nvidia/tts_en_conformer_g2p

To use these models, you'll need to export them to ONNX format. The export process depends on the specific model framework:

```python
# Example script to export SpeechBrain model to ONNX
import torch
from speechbrain.pretrained import GraphemeToPhoneme

# Load the model
g2p = GraphemeToPhoneme.from_hparams("speechbrain/soundchoice-g2p")

# Define example input
dummy_input = torch.zeros(1, 30)  # Batch size 1, sequence length 30

# Export to ONNX
torch.onnx.export(
    g2p.mods.g2p_model,
    dummy_input,
    "g2p_model.onnx",
    input_names=["input"],
    output_names=["output"],
    dynamic_axes={
        "input": {0: "batch_size", 1: "sequence_length"},
        "output": {0: "batch_size", 1: "sequence_length"}
    },
    opset_version=12
)
```

### Character Mapping Files

The `char_to_input.txt` file maps input characters to model indices, and `index_to_ipa.txt` maps output indices to IPA characters. These mappings depend on the specific model you're using.

## Usage

Once all files are in place, the ML-based phonetic service will automatically use them when the application starts. The service will fall back to the rule-based approach if any files are missing or if there's an error with the ML model.
