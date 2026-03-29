const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
  startGuest: () => ipcRenderer.invoke('start-guest'),
  startLogin: () => ipcRenderer.invoke('start-login'),
  backToMenu: () => ipcRenderer.invoke('back-to-menu'),
  toggleFullscreen: () => ipcRenderer.invoke('toggle-fullscreen'),
  platform: process.platform,
});
