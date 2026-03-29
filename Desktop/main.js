const { app, BrowserWindow, ipcMain } = require('electron');
const path = require('path');

let mainWindow;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1280,
    height: 800,
    minWidth: 1024,
    minHeight: 640,
    title: 'Q版方块人大乱斗',
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
    },
    autoHideMenuBar: true,
    backgroundColor: '#0a0a1e',
  });

  // 加载启动器
  mainWindow.loadFile(path.join(__dirname, 'launcher.html'));

  mainWindow.on('closed', () => { mainWindow = null; });
}

// Guest模式 - 直接加载游戏（单机版，不需要服务器）
ipcMain.handle('start-guest', async () => {
  // 加载本地game.html（与web版独立的PC版本）
  await mainWindow.loadFile(path.join(__dirname, 'game.html'));
  mainWindow.setFullScreen(true);
  mainWindow.setMenuBarVisibility(false);
  return { success: true };
});

// 登录模式（预留）
ipcMain.handle('start-login', async () => {
  return { success: false, message: '登录功能即将推出，敬请期待！' };
});

// 返回主菜单
ipcMain.handle('back-to-menu', async () => {
  mainWindow.setFullScreen(false);
  mainWindow.loadFile(path.join(__dirname, 'launcher.html'));
  return { success: true };
});

// ESC 退出全屏
ipcMain.handle('toggle-fullscreen', async () => {
  if (mainWindow) {
    mainWindow.setFullScreen(!mainWindow.isFullScreen());
  }
  return { success: true };
});

app.whenReady().then(createWindow);

app.on('window-all-closed', () => { app.quit(); });
app.on('activate', () => {
  if (BrowserWindow.getAllWindows().length === 0) createWindow();
});
