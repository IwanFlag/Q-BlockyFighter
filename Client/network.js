/**
 * Q版方块人大乱斗 - 客户端网络模块 V0.3
 * 
 * 功能：
 * - WebSocket 连接管理
 * - 帧同步接收与处理
 * - 房间管理
 * - 匹配系统
 * - 输入发送
 */

class GameNetwork {
  constructor() {
    this.ws = null;
    this.connected = false;
    this.playerId = null;
    this.playerName = null;
    this.roomId = null;
    this.isSpectator = false;
    this.frameBuffer = [];           // 帧数据缓冲
    this.inputQueue = [];            // 输入队列
    this.callbacks = {};             // 事件回调
    this.reconnectAttempts = 0;
    this.maxReconnectAttempts = 5;
    this.reconnectDelay = 2000;
    this.serverUrl = 'ws://' + (location.hostname || 'localhost') + ':3000';
    this.tickRate = 20;
    this.lastFrameTime = 0;
    this.latency = 0;
    this.inputSequence = 0;
  }

  // ==================== 连接管理 ====================

  connect(url = null) {
    if (url) this.serverUrl = url;
    
    return new Promise((resolve, reject) => {
      try {
        this.ws = new WebSocket(this.serverUrl);

        this.ws.onopen = () => {
          this.connected = true;
          this.reconnectAttempts = 0;
          console.log('[网络] 已连接到服务器');
          this.emit('connected');
          resolve();
        };

        this.ws.onmessage = (event) => {
          try {
            const msg = JSON.parse(event.data);
            this.handleMessage(msg);
          } catch (err) {
            console.error('[网络] 消息解析错误:', err);
          }
        };

        this.ws.onclose = (event) => {
          this.connected = false;
          console.log('[网络] 连接已关闭:', event.code, event.reason);
          this.emit('disconnected', { code: event.code, reason: event.reason });
          
          // 自动重连
          if (this.reconnectAttempts < this.maxReconnectAttempts) {
            this.reconnectAttempts++;
            console.log(`[网络] 尝试重连 (${this.reconnectAttempts}/${this.maxReconnectAttempts})...`);
            setTimeout(() => this.connect(), this.reconnectDelay);
          }
        };

        this.ws.onerror = (error) => {
          console.error('[网络] 连接错误:', error);
          this.emit('error', error);
          reject(error);
        };
      } catch (err) {
        reject(err);
      }
    });
  }

  disconnect() {
    this.reconnectAttempts = this.maxReconnectAttempts; // 阻止自动重连
    if (this.ws) {
      this.ws.close(1000, '客户端主动断开');
      this.ws = null;
    }
    this.connected = false;
    this.roomId = null;
  }

  // ==================== 消息发送 ====================

  send(type, data = {}) {
    if (!this.connected || !this.ws) {
      console.warn('[网络] 未连接，无法发送:', type);
      return false;
    }
    
    const message = JSON.stringify({ type, ...data });
    this.ws.send(message);
    return true;
  }

  // ==================== 消息处理 ====================

  handleMessage(msg) {
    switch (msg.type) {
      case 'welcome':
        this.playerId = msg.playerId;
        this.playerName = msg.playerName;
        this.tickRate = msg.tickRate;
        console.log(`[网络] 欢迎 ${this.playerName} (ID: ${this.playerId})`);
        this.emit('welcome', msg);
        break;

      case 'room_created':
        this.roomId = msg.room?.id;
        console.log(`[网络] 房间已创建: ${this.roomId}`);
        this.emit('room_created', msg);
        break;

      case 'room_joined':
        this.roomId = msg.room?.id;
        console.log(`[网络] 已加入房间: ${this.roomId}`);
        this.emit('room_joined', msg);
        break;

      case 'room_left':
        this.roomId = null;
        this.isSpectator = false;
        console.log(`[网络] 已离开房间`);
        this.emit('room_left', msg);
        break;

      case 'player_joined':
        console.log(`[网络] 玩家加入: ${msg.player?.name}`);
        this.emit('player_joined', msg);
        break;

      case 'player_left':
        console.log(`[网络] 玩家离开: ${msg.playerName}`);
        this.emit('player_left', msg);
        break;

      case 'game_start':
        console.log(`[网络] 游戏开始！`);
        this.emit('game_start', msg);
        break;

      case 'frame':
        // 帧数据处理
        this.handleFrame(msg.frame);
        this.emit('frame', msg);
        break;

      case 'matched':
        this.roomId = msg.room?.id;
        console.log(`[网络] 匹配成功！对手: ${msg.opponent?.name}`);
        this.emit('matched', msg);
        break;

      case 'chat':
        this.emit('chat', msg);
        break;

      case 'spectate':
        this.isSpectator = true;
        this.roomId = msg.room?.id;
        console.log(`[网络] 进入观战模式`);
        this.emit('spectate', msg);
        break;

      case 'error':
        console.error('[网络] 服务器错误:', msg.message);
        this.emit('error', msg);
        break;

      default:
        console.log('[网络] 未知消息:', msg.type);
    }
  }

  handleFrame(frameData) {
    if (!frameData) return;
    
    this.frameBuffer.push(frameData);
    
    // 限制缓冲区大小
    if (this.frameBuffer.length > 60) {
      this.frameBuffer.shift();
    }

    // 计算延迟
    const now = Date.now();
    if (frameData.timestamp) {
      this.latency = now - frameData.timestamp;
    }
    this.lastFrameTime = now;
  }

  // ==================== 游戏操作 ====================

  // 设置玩家名称
  setName(name) {
    this.playerName = name;
    this.send('join', { name });
  }

  // 设置角色
  setCharacter(character) {
    this.send('join', { character });
  }

  // 创建房间
  createRoom(mode = '1v1') {
    this.send('create_room', { mode });
  }

  // 加入房间
  joinRoom(roomId) {
    this.send('join_room', { roomId: parseInt(roomId) });
  }

  // 离开房间
  leaveRoom() {
    this.send('leave_room');
  }

  // 准备就绪
  setReady(ready = true, character = null, weaponIndex = 0) {
    this.send('ready', { ready, character, weaponIndex });
  }

  // 发送输入
  sendInput(keys, mouse) {
    this.inputSequence++;
    this.send('input', {
      keys,
      mouse,
      sequence: this.inputSequence,
    });
  }

  // 发送聊天
  sendChat(message) {
    this.send('chat', { message });
  }

  // 观战
  spectate(roomId) {
    this.send('spectate', { roomId: parseInt(roomId) });
  }

  // 请求匹配
  matchmake() {
    this.send('matchmake');
  }

  // 进入训练场
  enterTraining() {
    this.send('training');
  }

  // ==================== 事件系统 ====================

  on(event, callback) {
    if (!this.callbacks[event]) {
      this.callbacks[event] = [];
    }
    this.callbacks[event].push(callback);
    return this;
  }

  off(event, callback) {
    if (!this.callbacks[event]) return;
    if (callback) {
      this.callbacks[event] = this.callbacks[event].filter(cb => cb !== callback);
    } else {
      delete this.callbacks[event];
    }
    return this;
  }

  emit(event, data = {}) {
    if (!this.callbacks[event]) return;
    for (const cb of this.callbacks[event]) {
      try {
        cb(data);
      } catch (err) {
        console.error(`[事件] ${event} 回调错误:`, err);
      }
    }
  }

  // ==================== 工具方法 ====================

  // 获取当前帧号
  getCurrentFrame() {
    return this.frameBuffer.length > 0 
      ? this.frameBuffer[this.frameBuffer.length - 1].frame 
      : 0;
  }

  // 获取指定帧的数据
  getFrame(frameNumber) {
    return this.frameBuffer.find(f => f.frame === frameNumber);
  }

  // 获取最近N帧
  getRecentFrames(count = 10) {
    return this.frameBuffer.slice(-count);
  }

  // 获取连接状态
  getStatus() {
    return {
      connected: this.connected,
      playerId: this.playerId,
      playerName: this.playerName,
      roomId: this.roomId,
      isSpectator: this.isSpectator,
      latency: this.latency,
      currentFrame: this.getCurrentFrame(),
      bufferSize: this.frameBuffer.length,
    };
  }

  // 获取房间列表
  async getRoomList() {
    try {
      const response = await fetch('http://' + (location.hostname || 'localhost') + ':3000/api/rooms');
      return await response.json();
    } catch (err) {
      console.error('[网络] 获取房间列表失败:', err);
      return { rooms: [], online: 0 };
    }
  }

  // 获取服务器状态
  async getServerStatus() {
    try {
      const response = await fetch('http://' + (location.hostname || 'localhost') + ':3000/api/status');
      return await response.json();
    } catch (err) {
      console.error('[网络] 获取服务器状态失败:', err);
      return null;
    }
  }
}

// 导出
if (typeof module !== 'undefined' && module.exports) {
  module.exports = GameNetwork;
}
