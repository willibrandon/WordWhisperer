const { app, BrowserWindow, ipcMain } = require('electron');
const path = require('path');
const isDev = require('electron-is-dev');

let mainWindow;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
    minWidth: 800,
    minHeight: 600,
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false,
      preload: path.join(__dirname, 'preload.js')
    },
    icon: path.join(__dirname, 'favicon.ico'),
    title: 'Word Whisperer'
  });

  // Load the app
  mainWindow.loadURL(
    isDev
      ? 'http://localhost:5173' // Dev server URL for Vite
      : `file://${path.join(__dirname, '../dist/index.html')}`
  );

  // Open the DevTools in development mode
  if (isDev) {
    mainWindow.webContents.openDevTools();
  }

  mainWindow.on('closed', () => (mainWindow = null));
}

app.on('ready', createWindow);

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('activate', () => {
  if (mainWindow === null) {
    createWindow();
  }
});

// Setup IPC communication for API calls
ipcMain.handle('play-audio', async (event, audioPath) => {
  // Handle audio playback through Electron's native capabilities
  // This would be implemented later
  console.log('Playing audio:', audioPath);
  return true;
}); 