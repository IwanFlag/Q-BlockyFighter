/**
 * Q版方块人大乱斗 - 大厅 UI 组件 V0.3
 * 
 * 包含：
 * - 主菜单
 * - 房间列表
 * - 匹配界面
 * - 角色选择
 * - 观战列表
 */

class LobbyUI {
  constructor(network) {
    this.network = network;
    this.currentScreen = 'main';     // main | rooms | matchmaking | character | training
    this.selectedCharacter = null;
    this.rooms = [];
    this.players = [];
    this.init();
  }

  init() {
    this.createStyles();
    this.createHTML();
    this.bindEvents();
    this.bindNetworkEvents();
  }

  createStyles() {
    const style = document.createElement('style');
    style.textContent = `
      /* 大厅 UI 样式 */
      #lobby-overlay {
        position: fixed;
        top: 0; left: 0; width: 100%; height: 100%;
        background: linear-gradient(135deg, #0a0a1a 0%, #1a0a2a 50%, #0a1a2a 100%);
        z-index: 1000;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        font-family: 'Microsoft YaHei', sans-serif;
      }

      #lobby-overlay.hidden { display: none; }

      .lobby-title {
        font-size: 48px;
        font-weight: bold;
        color: #ffd700;
        text-shadow: 0 0 20px rgba(255, 215, 0, 0.5),
                     0 0 40px rgba(255, 215, 0, 0.3);
        margin-bottom: 10px;
        letter-spacing: 4px;
      }

      .lobby-subtitle {
        font-size: 16px;
        color: rgba(255, 255, 255, 0.6);
        margin-bottom: 40px;
      }

      .lobby-menu {
        display: flex;
        flex-direction: column;
        gap: 12px;
        width: 320px;
      }

      .lobby-btn {
        padding: 16px 32px;
        font-size: 18px;
        font-weight: bold;
        color: #fff;
        background: linear-gradient(135deg, rgba(255, 215, 0, 0.2), rgba(255, 150, 0, 0.2));
        border: 2px solid rgba(255, 215, 0, 0.4);
        border-radius: 12px;
        cursor: pointer;
        transition: all 0.2s;
        text-align: center;
      }

      .lobby-btn:hover {
        background: linear-gradient(135deg, rgba(255, 215, 0, 0.4), rgba(255, 150, 0, 0.4));
        border-color: #ffd700;
        transform: scale(1.02);
        box-shadow: 0 0 20px rgba(255, 215, 0, 0.3);
      }

      .lobby-btn.primary {
        background: linear-gradient(135deg, #ffd700, #ff8c00);
        color: #000;
        border-color: #ffd700;
      }

      .lobby-btn.primary:hover {
        box-shadow: 0 0 30px rgba(255, 215, 0, 0.5);
      }

      /* 房间列表 */
      .lobby-rooms {
        width: 600px;
        max-height: 400px;
        overflow-y: auto;
        margin: 20px 0;
      }

      .room-item {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 12px 16px;
        background: rgba(255, 255, 255, 0.05);
        border: 1px solid rgba(255, 255, 255, 0.1);
        border-radius: 8px;
        margin-bottom: 8px;
        color: #fff;
      }

      .room-item:hover {
        background: rgba(255, 215, 0, 0.1);
        border-color: rgba(255, 215, 0, 0.3);
      }

      .room-info { display: flex; flex-direction: column; gap: 4px; }
      .room-name { font-weight: bold; color: #ffd700; }
      .room-players { font-size: 12px; color: rgba(255, 255, 255, 0.6); }
      .room-status { font-size: 12px; padding: 4px 8px; border-radius: 4px; }
      .room-status.waiting { background: #2ecc71; color: #000; }
      .room-status.playing { background: #e74c3c; color: #fff; }

      /* 匹配中 */
      .matchmaking {
        text-align: center;
        padding: 40px;
      }

      .matchmaking-spinner {
        width: 60px; height: 60px;
        border: 4px solid rgba(255, 215, 0, 0.2);
        border-top-color: #ffd700;
        border-radius: 50%;
        animation: spin 1s linear infinite;
        margin: 0 auto 20px;
      }

      @keyframes spin { to { transform: rotate(360deg); } }

      .matchmaking-text {
        font-size: 24px;
        color: #ffd700;
        margin-bottom: 10px;
      }

      .matchmaking-time {
        font-size: 14px;
        color: rgba(255, 255, 255, 0.6);
      }

      /* 角色选择 */
      .character-grid {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: 12px;
        width: 600px;
        margin: 20px 0;
      }

      .character-card {
        padding: 16px;
        background: rgba(255, 255, 255, 0.05);
        border: 2px solid rgba(255, 255, 255, 0.1);
        border-radius: 12px;
        text-align: center;
        cursor: pointer;
        transition: all 0.2s;
        color: #fff;
      }

      .character-card:hover {
        background: rgba(255, 215, 0, 0.1);
        border-color: rgba(255, 215, 0, 0.3);
      }

      .character-card.selected {
        background: rgba(255, 215, 0, 0.2);
        border-color: #ffd700;
        box-shadow: 0 0 15px rgba(255, 215, 0, 0.3);
      }

      .character-icon { font-size: 36px; margin-bottom: 8px; }
      .character-name { font-size: 14px; font-weight: bold; color: #ffd700; }
      .character-role { font-size: 11px; color: rgba(255, 255, 255, 0.5); }

      /* 连接状态 */
      .connection-status {
        position: fixed;
        top: 10px; right: 10px;
        padding: 6px 12px;
        border-radius: 20px;
        font-size: 12px;
        font-weight: bold;
        z-index: 1001;
      }

      .connection-status.connected {
        background: rgba(46, 204, 113, 0.2);
        color: #2ecc71;
        border: 1px solid rgba(46, 204, 113, 0.4);
      }

      .connection-status.disconnected {
        background: rgba(231, 76, 60, 0.2);
        color: #e74c3c;
        border: 1px solid rgba(231, 76, 60, 0.4);
      }

      /* 输入框 */
      .lobby-input {
        padding: 12px 16px;
        font-size: 16px;
        background: rgba(0, 0, 0, 0.5);
        border: 2px solid rgba(255, 215, 0, 0.3);
        border-radius: 8px;
        color: #fff;
        outline: none;
        width: 100%;
      }

      .lobby-input:focus {
        border-color: #ffd700;
      }

      .lobby-input::placeholder {
        color: rgba(255, 255, 255, 0.4);
      }

      /* 返回按钮 */
      .back-btn {
        position: absolute;
        top: 20px; left: 20px;
        padding: 8px 16px;
        font-size: 14px;
        color: rgba(255, 255, 255, 0.6);
        background: transparent;
        border: 1px solid rgba(255, 255, 255, 0.2);
        border-radius: 6px;
        cursor: pointer;
      }

      .back-btn:hover {
        color: #fff;
        border-color: rgba(255, 255, 255, 0.5);
      }

      /* HUD 集成 */
      #online-count {
        position: fixed;
        bottom: 10px; right: 10px;
        font-size: 12px;
        color: rgba(255, 255, 255, 0.4);
        z-index: 1001;
      }

      /* 聊天框 */
      .chat-container {
        position: fixed;
        bottom: 30px; left: 30px;
        width: 300px;
        z-index: 999;
      }

      .chat-messages {
        max-height: 200px;
        overflow-y: auto;
        padding: 8px;
        background: rgba(0, 0, 0, 0.5);
        border-radius: 8px;
        margin-bottom: 8px;
      }

      .chat-msg {
        font-size: 12px;
        color: #fff;
        margin-bottom: 4px;
      }

      .chat-msg .name { color: #ffd700; font-weight: bold; }

      .chat-input-container {
        display: flex;
        gap: 8px;
      }

      .chat-input {
        flex: 1;
        padding: 8px 12px;
        font-size: 12px;
        background: rgba(0, 0, 0, 0.7);
        border: 1px solid rgba(255, 255, 255, 0.2);
        border-radius: 6px;
        color: #fff;
        outline: none;
      }

      .chat-send {
        padding: 8px 16px;
        font-size: 12px;
        background: rgba(255, 215, 0, 0.3);
        border: 1px solid rgba(255, 215, 0, 0.5);
        border-radius: 6px;
        color: #ffd700;
        cursor: pointer;
      }
    `;
    document.head.appendChild(style);
  }

  createHTML() {
    const overlay = document.createElement('div');
    overlay.id = 'lobby-overlay';
    overlay.innerHTML = `
      <div id="connection-status" class="connection-status disconnected">未连接</div>
      
      <!-- 主菜单 -->
      <div id="screen-main" class="lobby-screen">
        <div class="lobby-title">⚔️ 裂隙纪元</div>
        <div class="lobby-subtitle">Q版方块人大乱斗 · Web 原型 V0.3</div>
        
        <div style="margin-bottom: 20px;">
          <input type="text" id="player-name" class="lobby-input" 
                 placeholder="输入你的名字" maxlength="12" style="width: 320px;">
        </div>
        
        <div class="lobby-menu">
          <button class="lobby-btn primary" onclick="lobby.startMatchmaking()">⚔️ 快速匹配 (1V1)</button>
          <button class="lobby-btn" onclick="lobby.showScreen('rooms')">🏠 房间列表</button>
          <button class="lobby-btn" onclick="lobby.enterTraining()">🎯 训练场</button>
          <button class="lobby-btn" onclick="lobby.showScreen('character')">👤 选择角色</button>
          <button class="lobby-btn" onclick="lobby.showScreen('spectate')">👁️ 观战</button>
        </div>
        
        <div id="online-count" style="margin-top: 30px; color: rgba(255,255,255,0.4); font-size: 14px;">
          在线: --
        </div>
      </div>

      <!-- 房间列表 -->
      <div id="screen-rooms" class="lobby-screen" style="display: none;">
        <button class="back-btn" onclick="lobby.showScreen('main')">← 返回</button>
        <div class="lobby-title" style="font-size: 32px;">🏠 房间列表</div>
        <div class="lobby-rooms" id="room-list">
          <div style="text-align: center; color: rgba(255,255,255,0.4); padding: 40px;">
            加载中...
          </div>
        </div>
        <div style="display: flex; gap: 12px; width: 600px;">
          <input type="text" id="room-id-input" class="lobby-input" 
                 placeholder="输入房间号加入">
          <button class="lobby-btn" onclick="lobby.joinRoomById()" style="white-space: nowrap;">加入</button>
        </div>
        <button class="lobby-btn primary" onclick="lobby.createRoom()" style="width: 600px; margin-top: 12px;">
          ➕ 创建房间
        </button>
      </div>

      <!-- 匹配中 -->
      <div id="screen-matchmaking" class="lobby-screen" style="display: none;">
        <div class="matchmaking">
          <div class="matchmaking-spinner"></div>
          <div class="matchmaking-text">正在匹配对手...</div>
          <div class="matchmaking-time" id="matchmaking-time">00:00</div>
          <button class="lobby-btn" onclick="lobby.cancelMatchmaking()" style="margin-top: 30px;">
            取消匹配
          </button>
        </div>
      </div>

      <!-- 角色选择 -->
      <div id="screen-character" class="lobby-screen" style="display: none;">
        <button class="back-btn" onclick="lobby.showScreen('main')">← 返回</button>
        <div class="lobby-title" style="font-size: 32px;">👤 选择角色</div>
        <div class="character-grid" id="character-grid"></div>
        <button class="lobby-btn primary" onclick="lobby.confirmCharacter()" style="width: 600px;">
          确认选择
        </button>
      </div>

      <!-- 训练中 -->
      <div id="screen-training" class="lobby-screen" style="display: none;">
        <div class="matchmaking">
          <div class="matchmaking-text">🎯 训练场</div>
          <div style="color: rgba(255,255,255,0.6); margin: 20px 0;">
            连接服务器中...
          </div>
        </div>
      </div>
    `;
    document.body.appendChild(overlay);

    // 在线人数
    const onlineCount = document.createElement('div');
    onlineCount.id = 'online-count';
    onlineCount.textContent = '';
    document.body.appendChild(onlineCount);

    // 填充角色列表
    this.populateCharacters();
  }

  populateCharacters() {
    const characters = [
      { id: 'yinren', name: '影忍·雾隐', role: '刺客·巫术', icon: '🥷' },
      { id: 'jingru', name: '冷月·荆如', role: '刺客·武术', icon: '⚔️' },
      { id: 'zhebie', name: '铁骑·哲别', role: '战士·武术', icon: '🏇' },
      { id: 'guanyun', name: '傲骨·关云', role: '战士·武术', icon: '🐉' },
      { id: 'kali', name: '雷鸣·卡利', role: '战士·魔法', icon: '⚡' },
      { id: 'aolafu', name: '磐石·奥拉夫', role: '坦克·仙术', icon: '🛡️' },
      { id: 'makexi', name: '铁壁·马克西', role: '坦克·武术', icon: '🏛️' },
      { id: 'mulan', name: '追风·花木兰', role: '远程·仙术', icon: '🏹' },
      { id: 'hasang', name: '毒蝎·哈桑', role: '远程·巫术', icon: '🦂' },
      { id: 'lishunchen', name: '破军·李舜臣', role: '远程·魔法', icon: '💣' },
      { id: 'amani', name: '圣歌·阿玛尼', role: '辅助·巫术', icon: '🎵' },
      { id: 'dafenqi', name: '天机·达芬奇', role: '辅助·魔法', icon: '🔧' },
    ];

    const grid = document.getElementById('character-grid');
    grid.innerHTML = characters.map(char => `
      <div class="character-card" data-id="${char.id}" onclick="lobby.selectCharacter('${char.id}')">
        <div class="character-icon">${char.icon}</div>
        <div class="character-name">${char.name}</div>
        <div class="character-role">${char.role}</div>
      </div>
    `).join('');
  }

  // ==================== 界面切换 ====================

  showScreen(screen) {
    document.querySelectorAll('.lobby-screen').forEach(s => s.style.display = 'none');
    const target = document.getElementById(`screen-${screen}`);
    if (target) target.style.display = 'block';
    this.currentScreen = screen;

    // 刷新房间列表
    if (screen === 'rooms') {
      this.refreshRoomList();
    }
  }

  show() {
    document.getElementById('lobby-overlay').classList.remove('hidden');
    this.showScreen('main');
    this.updateOnlineCount();
  }

  hide() {
    document.getElementById('lobby-overlay').classList.add('hidden');
  }

  // ==================== 事件绑定 ====================

  bindEvents() {
    // 玩家名称
    const nameInput = document.getElementById('player-name');
    nameInput.addEventListener('change', () => {
      const name = nameInput.value.trim();
      if (name) {
        this.network.setName(name);
      }
    });

    // 定时刷新在线人数
    setInterval(() => this.updateOnlineCount(), 10000);
  }

  bindNetworkEvents() {
    this.network
      .on('connected', () => {
        this.updateConnectionStatus(true);
        const name = document.getElementById('player-name').value.trim();
        if (name) this.network.setName(name);
      })
      .on('disconnected', () => {
        this.updateConnectionStatus(false);
      })
      .on('matched', (data) => {
        this.hide();
        this.onMatched?.(data);
      })
      .on('game_start', (data) => {
        this.hide();
        this.onGameStart?.(data);
      })
      .on('room_created', (data) => {
        this.showScreen('rooms');
      })
      .on('room_joined', (data) => {
        if (data.room) {
          this.showScreen('rooms');
        }
      })
      .on('error', (data) => {
        alert(data.message || '发生错误');
      });
  }

  updateConnectionStatus(connected) {
    const el = document.getElementById('connection-status');
    el.textContent = connected ? '🟢 已连接' : '🔴 未连接';
    el.className = `connection-status ${connected ? 'connected' : 'disconnected'}`;
  }

  async updateOnlineCount() {
    try {
      const status = await this.network.getServerStatus();
      if (status) {
        document.getElementById('online-count').textContent = `在线: ${status.players} | 房间: ${status.rooms}`;
      }
    } catch (err) {
      document.getElementById('online-count').textContent = '服务器未连接';
    }
  }

  // ==================== 游戏操作 ====================

  startMatchmaking() {
    this.network.matchmake();
    this.showScreen('matchmaking');
    this.matchmakingStartTime = Date.now();
    this.matchmakingTimer = setInterval(() => {
      const elapsed = Math.floor((Date.now() - this.matchmakingStartTime) / 1000);
      const min = Math.floor(elapsed / 60).toString().padStart(2, '0');
      const sec = (elapsed % 60).toString().padStart(2, '0');
      document.getElementById('matchmaking-time').textContent = `${min}:${sec}`;
    }, 1000);
  }

  cancelMatchmaking() {
    clearInterval(this.matchmakingTimer);
    this.network.leaveRoom();
    this.showScreen('main');
  }

  enterTraining() {
    this.network.enterTraining();
    this.showScreen('training');
  }

  createRoom() {
    this.network.createRoom('1v1');
  }

  joinRoomById() {
    const input = document.getElementById('room-id-input');
    const roomId = input.value.trim();
    if (roomId) {
      this.network.joinRoom(roomId);
    }
  }

  async refreshRoomList() {
    try {
      const data = await this.network.getRoomList();
      this.rooms = data.rooms || [];
      this.renderRoomList();
    } catch (err) {
      console.error('刷新房间列表失败:', err);
    }
  }

  renderRoomList() {
    const list = document.getElementById('room-list');
    if (this.rooms.length === 0) {
      list.innerHTML = `
        <div style="text-align: center; color: rgba(255,255,255,0.4); padding: 40px;">
          暂无房间，创建一个吧！
        </div>
      `;
      return;
    }

    list.innerHTML = this.rooms.map(room => `
      <div class="room-item" onclick="lobby.network.joinRoom(${room.id})">
        <div class="room-info">
          <div class="room-name">房间 #${room.id}</div>
          <div class="room-players">
            ${room.players.map(p => p.name).join(' vs ')} 
            (${room.players.length}/2)
          </div>
        </div>
        <span class="room-status ${room.state}">${room.state === 'waiting' ? '等待中' : '游戏中'}</span>
      </div>
    `).join('');
  }

  selectCharacter(id) {
    this.selectedCharacter = id;
    document.querySelectorAll('.character-card').forEach(card => {
      card.classList.toggle('selected', card.dataset.id === id);
    });
  }

  confirmCharacter() {
    if (this.selectedCharacter) {
      this.network.setCharacter(this.selectedCharacter);
      this.showScreen('main');
    }
  }

  // ==================== 回调设置 ====================

  set onMatched(callback) { this._onMatched = callback; }
  set onGameStart(callback) { this._onGameStart = callback; }
}

// 导出
if (typeof module !== 'undefined' && module.exports) {
  module.exports = LobbyUI;
}
