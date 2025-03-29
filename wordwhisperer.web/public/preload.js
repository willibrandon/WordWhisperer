// Preload script for Electron
const { contextBridge, ipcRenderer } = require('electron');

// Expose protected methods that allow the renderer process to use
// the ipcRenderer without exposing the entire object
contextBridge.exposeInMainWorld(
  'api', {
    playAudio: (audioPath) => ipcRenderer.invoke('play-audio', audioPath),
    // Add more API methods as needed
  }
); 