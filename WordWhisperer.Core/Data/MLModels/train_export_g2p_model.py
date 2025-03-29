#!/usr/bin/env python3
"""
Train and export a Grapheme-to-Phoneme model to ONNX format for WordWhisperer

This script provides options to:
1. Train a simple sequence-to-sequence model using CMU dictionary data
2. Export an existing pre-trained model to ONNX format
3. Generate character mapping files needed by the C# application

Requirements:
- torch
- torchaudio (for speechbrain models)
- speechbrain (optional, for using pre-trained models)
- numpy
- onnx
- onnxruntime (for testing)
"""

import os
import argparse
import torch
import torch.nn as nn
import torch.nn.functional as F
import numpy as np
import onnx
import onnxruntime as ort
from torch.utils.data import Dataset, DataLoader
from typing import Dict, List, Tuple, Optional
import logging

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

# Character sets and mappings
class CharMapper:
    def __init__(self):
        # Input character set (graphemes)
        self.chars = ['<pad>', '<unk>'] + list("abcdefghijklmnopqrstuvwxyz'-.")
        self.char_to_idx = {c: i for i, c in enumerate(self.chars)}
        
        # Output character set (phonemes in IPA)
        self.ipa_chars = ['<pad>', '<unk>'] + list("əɑæɛɪioʊuʌɝɚɒaɪaʊɔɪˈˌbdfghdʒklmnŋprsʃttʃθðvwjzʒ")
        self.ipa_to_idx = {c: i for i, c in enumerate(self.ipa_chars)}
        
    def save_mappings(self, output_dir: str):
        """Save character mappings to files"""
        # Save input character mapping
        with open(os.path.join(output_dir, 'char_to_input.txt'), 'w', encoding='utf-8') as f:
            for char, idx in self.char_to_idx.items():
                f.write(f"{char}\t{idx}\n")
        
        # Save output character mapping
        with open(os.path.join(output_dir, 'index_to_ipa.txt'), 'w', encoding='utf-8') as f:
            for idx, char in enumerate(self.ipa_chars):
                f.write(f"{idx}\t{char}\n")
        
        logger.info(f"Character mappings saved to {output_dir}")

# Dataset for G2P training
class CMUDictDataset(Dataset):
    def __init__(self, cmu_dict_path: str, char_mapper: CharMapper, max_word_len: int = 30, max_phoneme_len: int = 40):
        self.data = []
        self.char_mapper = char_mapper
        self.max_word_len = max_word_len
        self.max_phoneme_len = max_phoneme_len
        
        # Load and process CMU dictionary
        logger.info(f"Loading CMU dictionary from {cmu_dict_path}")
        with open(cmu_dict_path, 'r', encoding='utf-8') as f:
            for line in f:
                if line.startswith(';;;') or not line.strip():
                    continue
                    
                parts = line.strip().split('  ', 1)
                if len(parts) != 2:
                    continue
                
                word, arpabet = parts
                word = word.lower()
                
                # Skip variants (words with numbers in parentheses)
                if '(' in word:
                    continue
                
                # Convert ARPABET to IPA
                ipa = self.convert_arpabet_to_ipa(arpabet)
                
                self.data.append((word, ipa))
        
        logger.info(f"Loaded {len(self.data)} words from CMU dictionary")
        
    def __len__(self):
        return len(self.data)
    
    def __getitem__(self, idx):
        word, ipa = self.data[idx]
        
        # Convert word to tensor
        word_tensor = torch.zeros(self.max_word_len, dtype=torch.long)
        for i, char in enumerate(word[:self.max_word_len]):
            word_tensor[i] = self.char_mapper.char_to_idx.get(char, 1)  # 1 is <unk>
            
        # Convert IPA to tensor
        ipa_tensor = torch.zeros(self.max_phoneme_len, dtype=torch.long)
        for i, char in enumerate(ipa[:self.max_phoneme_len]):
            if char in self.char_mapper.ipa_to_idx:
                ipa_tensor[i] = self.char_mapper.ipa_to_idx[char]
            else:
                ipa_tensor[i] = 1  # <unk>
        
        return word_tensor, ipa_tensor
        
    def convert_arpabet_to_ipa(self, arpabet: str) -> str:
        """Convert ARPABET phonemes to IPA notation"""
        # This is a simplified mapping, a real implementation would be more complex
        arpabet_to_ipa = {
            'AA0': 'ɑ', 'AA1': 'ˈɑ', 'AA2': 'ˌɑ',
            'AE0': 'æ', 'AE1': 'ˈæ', 'AE2': 'ˌæ',
            'AH0': 'ə', 'AH1': 'ˈʌ', 'AH2': 'ˌʌ',
            'AO0': 'ɔ', 'AO1': 'ˈɔ', 'AO2': 'ˌɔ',
            'AW0': 'aʊ', 'AW1': 'ˈaʊ', 'AW2': 'ˌaʊ',
            'AY0': 'aɪ', 'AY1': 'ˈaɪ', 'AY2': 'ˌaɪ',
            'B': 'b', 'CH': 'tʃ', 'D': 'd', 'DH': 'ð',
            'EH0': 'ɛ', 'EH1': 'ˈɛ', 'EH2': 'ˌɛ',
            'ER0': 'ɚ', 'ER1': 'ˈɝ', 'ER2': 'ˌɝ',
            'EY0': 'eɪ', 'EY1': 'ˈeɪ', 'EY2': 'ˌeɪ',
            'F': 'f', 'G': 'g', 'HH': 'h',
            'IH0': 'ɪ', 'IH1': 'ˈɪ', 'IH2': 'ˌɪ',
            'IY0': 'i', 'IY1': 'ˈi', 'IY2': 'ˌi',
            'JH': 'dʒ', 'K': 'k', 'L': 'l', 'M': 'm',
            'N': 'n', 'NG': 'ŋ', 'OW0': 'oʊ', 'OW1': 'ˈoʊ',
            'OW2': 'ˌoʊ', 'OY0': 'ɔɪ', 'OY1': 'ˈɔɪ', 'OY2': 'ˌɔɪ',
            'P': 'p', 'R': 'r', 'S': 's', 'SH': 'ʃ',
            'T': 't', 'TH': 'θ', 'UH0': 'ʊ', 'UH1': 'ˈʊ',
            'UH2': 'ˌʊ', 'UW0': 'u', 'UW1': 'ˈu', 'UW2': 'ˌu',
            'V': 'v', 'W': 'w', 'Y': 'j', 'Z': 'z', 'ZH': 'ʒ'
        }
        
        ipa = []
        for phone in arpabet.split():
            if phone in arpabet_to_ipa:
                ipa.append(arpabet_to_ipa[phone])
            else:
                logger.warning(f"Unknown ARPABET phoneme: {phone}")
        
        return ''.join(ipa)

# Simple Encoder-Decoder for G2P
class G2PModel(nn.Module):
    def __init__(self, input_size: int, hidden_size: int, output_size: int, 
                 max_length: int = 30):
        super(G2PModel, self).__init__()
        self.hidden_size = hidden_size
        self.max_length = max_length
        
        # Encoder
        self.embedding = nn.Embedding(input_size, hidden_size)
        self.gru = nn.GRU(hidden_size, hidden_size, batch_first=True, bidirectional=True)
        
        # Decoder
        self.output_size = output_size
        self.out = nn.Linear(hidden_size * 2, output_size)
    
    def forward(self, input_tensor: torch.Tensor):
        # input_tensor: [batch_size, seq_len]
        batch_size = input_tensor.size(0)
        
        # Encoder
        embedded = self.embedding(input_tensor)  # [batch_size, seq_len, hidden_size]
        encoder_outputs, encoder_hidden = self.gru(embedded)
        
        # Decoder (simplified for ONNX export - single forward pass)
        output_logits = self.out(encoder_outputs)  # [batch_size, seq_len, output_size]
        
        return output_logits

def train_model(args):
    """Train a new G2P model"""
    char_mapper = CharMapper()
    
    # Create dataset and dataloader
    dataset = CMUDictDataset(args.cmu_dict, char_mapper, args.max_word_len, args.max_phoneme_len)
    train_size = int(0.9 * len(dataset))
    val_size = len(dataset) - train_size
    train_dataset, val_dataset = torch.utils.data.random_split(dataset, [train_size, val_size])
    
    train_loader = DataLoader(train_dataset, batch_size=args.batch_size, shuffle=True)
    val_loader = DataLoader(val_dataset, batch_size=args.batch_size)
    
    # Create model
    model = G2PModel(
        input_size=len(char_mapper.chars),
        hidden_size=args.hidden_size,
        output_size=len(char_mapper.ipa_chars),
        max_length=args.max_word_len
    )
    
    # Training setup
    criterion = nn.CrossEntropyLoss(ignore_index=0)  # ignore padding
    optimizer = torch.optim.Adam(model.parameters(), lr=args.learning_rate)
    
    # Training loop
    logger.info("Starting training...")
    for epoch in range(args.epochs):
        model.train()
        train_loss = 0
        
        for word_batch, ipa_batch in train_loader:
            optimizer.zero_grad()
            
            # Forward pass
            output = model(word_batch)  # [batch_size, seq_len, output_size]
            
            # Reshape for loss calculation
            output = output.view(-1, output.size(2))  # [batch_size*seq_len, output_size]
            target = ipa_batch.view(-1)  # [batch_size*seq_len]
            
            # Calculate loss
            loss = criterion(output, target)
            train_loss += loss.item()
            
            # Backward pass
            loss.backward()
            optimizer.step()
        
        # Validation
        model.eval()
        val_loss = 0
        with torch.no_grad():
            for word_batch, ipa_batch in val_loader:
                output = model(word_batch)
                output = output.view(-1, output.size(2))
                target = ipa_batch.view(-1)
                loss = criterion(output, target)
                val_loss += loss.item()
        
        logger.info(f"Epoch {epoch+1}/{args.epochs}, "
                   f"Train Loss: {train_loss/len(train_loader):.4f}, "
                   f"Val Loss: {val_loss/len(val_loader):.4f}")
    
    # Save the model
    torch.save(model.state_dict(), args.output_model)
    logger.info(f"Model saved to {args.output_model}")
    
    # Save character mappings
    char_mapper.save_mappings(os.path.dirname(args.output_model))
    
    # Export to ONNX
    export_to_onnx(model, args.output_model.replace('.pt', '.onnx'), char_mapper, args.max_word_len)

def export_to_onnx(model, onnx_path, char_mapper=None, max_word_len=30):
    """Export PyTorch model to ONNX format"""
    model.eval()
    
    # Create dummy input tensor
    dummy_input = torch.zeros((1, max_word_len), dtype=torch.long)
    
    # Export the model
    torch.onnx.export(
        model,
        dummy_input,
        onnx_path,
        export_params=True,
        opset_version=12,
        do_constant_folding=True,
        input_names=['input'],
        output_names=['output'],
        dynamic_axes={
            'input': {0: 'batch_size'},
            'output': {0: 'batch_size'}
        }
    )
    
    logger.info(f"Model exported to ONNX format: {onnx_path}")
    
    # Verify the model
    try:
        onnx_model = onnx.load(onnx_path)
        onnx.checker.check_model(onnx_model)
        logger.info("ONNX model checked successfully")
        
        # Test inference with ONNX Runtime
        ort_session = ort.InferenceSession(onnx_path)
        ort_inputs = {'input': dummy_input.numpy()}
        ort_outputs = ort_session.run(None, ort_inputs)
        logger.info("ONNX Runtime inference test successful")
        
        return True
    except Exception as e:
        logger.error(f"Error verifying ONNX model: {e}")
        return False

def export_speechbrain_model(args):
    """Export a pre-trained SpeechBrain G2P model to ONNX format"""
    try:
        from speechbrain.pretrained import GraphemeToPhoneme
        
        # Load the model
        logger.info("Loading SpeechBrain G2P model...")
        g2p = GraphemeToPhoneme.from_hparams(source=args.pretrained_model)
        
        # Create char mapper and save mappings
        char_mapper = CharMapper()
        char_mapper.save_mappings(os.path.dirname(args.output_model))
        
        # Export the model
        logger.info("Exporting model to ONNX...")
        dummy_input = torch.zeros(1, args.max_word_len)
        
        torch.onnx.export(
            g2p.mods.g2p_model,
            dummy_input,
            args.output_model,
            export_params=True,
            opset_version=12,
            do_constant_folding=True,
            input_names=['input'],
            output_names=['output'],
            dynamic_axes={
                'input': {0: 'batch_size', 1: 'sequence_length'},
                'output': {0: 'batch_size', 1: 'sequence_length'}
            }
        )
        
        logger.info(f"Model exported to {args.output_model}")
        return True
    except ImportError:
        logger.error("SpeechBrain not installed. Please install with: pip install speechbrain")
        return False
    except Exception as e:
        logger.error(f"Error exporting SpeechBrain model: {e}")
        return False

def main():
    parser = argparse.ArgumentParser(description="Train and export G2P models for WordWhisperer")
    subparsers = parser.add_subparsers(dest='command')
    
    # Train command
    train_parser = subparsers.add_parser('train', help='Train a new G2P model')
    train_parser.add_argument('--cmu-dict', required=True, help='Path to CMU dictionary file')
    train_parser.add_argument('--output-model', required=True, help='Path to save the trained model')
    train_parser.add_argument('--epochs', type=int, default=10, help='Number of training epochs')
    train_parser.add_argument('--batch-size', type=int, default=64, help='Batch size for training')
    train_parser.add_argument('--hidden-size', type=int, default=256, help='Hidden size for the model')
    train_parser.add_argument('--learning-rate', type=float, default=0.001, help='Learning rate')
    train_parser.add_argument('--max-word-len', type=int, default=30, help='Maximum word length')
    train_parser.add_argument('--max-phoneme-len', type=int, default=40, help='Maximum phoneme sequence length')
    
    # Export command
    export_parser = subparsers.add_parser('export', help='Export pre-trained SpeechBrain model to ONNX')
    export_parser.add_argument('--pretrained-model', required=True, 
                               help='Path or HuggingFace identifier for pre-trained model')
    export_parser.add_argument('--output-model', required=True, help='Path to save the ONNX model')
    export_parser.add_argument('--max-word-len', type=int, default=30, help='Maximum word length')
    
    # Generate mappings command
    mapping_parser = subparsers.add_parser('mappings', help='Generate character mapping files')
    mapping_parser.add_argument('--output-dir', required=True, help='Directory to save mapping files')
    
    args = parser.parse_args()
    
    if args.command == 'train':
        train_model(args)
    elif args.command == 'export':
        export_speechbrain_model(args)
    elif args.command == 'mappings':
        char_mapper = CharMapper()
        char_mapper.save_mappings(args.output_dir)
    else:
        parser.print_help()

if __name__ == "__main__":
    main()
