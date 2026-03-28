/**
  // 静态文件
  serveStatic(req, res);
  return;
 * Q版方块人大乱斗 - 帧同步服务器 V0.3
 * 
 * 功能：
 * - WebSocket 实时通信
 * - 房间管理（创建/加入/离开）
 * - 帧同步（确定性锁步）
 * - 1V1 匹配系统
 * - 训练场模式
 * - 观战系统
 */

const { WebSocketServer } = require('ws');
const http = require('http');
const crypto = require('crypto');

// ==================== 配置 ====================
const CONFIG = {
  PORT: 3000,
  TICK_RATE: 20,                    // 服务器帧率（每秒帧数）
  FRAME_INTERVAL: 1000 / 20,        // 帧间隔（ms）
  MAX_PLAYERS_PER_ROOM: 2,          // 1V1 最大玩家数
  MAX_SPECTATORS: 10,               // 最大观战人数
  MATCHMAKING_INTERVAL: 1000,       // 匹配检查间隔（ms）
  ROOM_CLEANUP_INTERVAL: 30000,     // 房间清理间隔（ms）
  IDLE_TIMEOUT: 60000,              // 空闲超时（ms）
};

// ==================== 全局状态 ====================
const rooms = new Map();            // roomId -> Room
const players = new Map();          // ws -> Player
const matchmakingQueue = [];        // 等待匹配的玩家
let nextRoomId = 1;
let nextPlayerId = 1;

// ==================== 消息类型 ====================
const MSG = {
  // 客户端 -> 服务器
  C_JOIN: 'join',                   // 加入服务器
  C_CREATE_ROOM: 'create_room',     // 创建房间
  C_JOIN_ROOM: 'join_room',         // 加入房间
  C_LEAVE_ROOM: 'leave_room',       // 离开房间
  C_READY: 'ready',                 // 准备就绪
  C_INPUT: 'input',                 // 输入帧
  C_CHAT: 'chat',                   // 聊天
  C_SPECTATE: 'spectate',           // 观战
  C_MATCHMAKE: 'matchmake',         // 请求匹配
  C_TRAINING: 'training',           // 训练场模式
  
  // 服务器 -> 客户端
  S_WELCOME: 'welcome',             // 欢迎消息
  S_ROOM_CREATED: 'room_created',   // 房间已创建
  S_ROOM_JOINED: 'room_joined',     // 已加入房间
  S_ROOM_LEFT: 'room_left',         // 已离开房间
  S_PLAYER_JOINED: 'player_joined', // 有玩家加入
  S_PLAYER_LEFT: 'player_left',     // 有玩家离开
  S_GAME_START: 'game_start',       // 游戏开始
  S_FRAME: 'frame',                 // 帧数据
  S_GAME_STATE: 'game_state',       // 游戏状态
  S_CHAT: 'chat',                   // 聊天消息
  S_MATCHED: 'matched',             // 匹配成功
  S_ERROR: 'error',                 // 错误
  S_ROOM_LIST: 'room_list',         // 房间列表
  S_SPECTATE: 'spectate',           // 观战数据
};

// ==================== 玩家类 ====================
class Player {
  constructor(ws, id) {
    this.ws = ws;
    this.id = id;
    this.name = `玩家${id}`;
    this.roomId = null;
    this.isReady = false;
    this.isSpectator = false;
    this.character = null;          // 选择的角色
    this.weaponIndex = 0;           // 当前武器索引
    this.lastInput = null;
    this.lastActivity = Date.now();
  }

  send(type, data = {}) {
    if (this.ws.readyState === 1) {
      this.ws.send(JSON.stringify({ type, ...data }));
    }
  }

  toJSON() {
    return {
      id: this.id,
      name: this.name,
      isReady: this.isReady,
      character: this.character,
    };
  }
}

// ==================== 房间类 ====================
class Room {
  constructor(id, host, mode = '1v1') {
    this.id = id;
    this.host = host.id;
    this.mode = mode;               // '1v1' | 'training'
    this.players = new Map();       // playerId -> Player
    this.spectators = new Map();    // playerId -> Player
    this.state = 'waiting';         // 'waiting' | 'playing' | 'finished'
    this.frameNumber = 0;
    this.frameInputs = [];          // 存储所有帧的输入
    this.gameStartTime = null;
    this.createdAt = Date.now();
    
    // 添加房主
    this.addPlayer(host);
  }

  addPlayer(player) {
    if (this.mode === '1v1' && this.players.size >= CONFIG.MAX_PLAYERS_PER_ROOM) {
      return false;
    }
    this.players.set(player.id, player);
    player.roomId = this.id;
    player.isSpectator = false;
    return true;
  }

  addSpectator(player) {
    if (this.spectators.size >= CONFIG.MAX_SPECTATORS) {
      return false;
    }
    this.spectators.set(player.id, player);
    player.roomId = this.id;
    player.isSpectator = true;
    return true;
  }

  removePlayer(playerId) {
    const player = this.players.get(playerId) || this.spectators.get(playerId);
    if (!player) return false;
    
    this.players.delete(playerId);
    this.spectators.delete(playerId);
    player.roomId = null;
    player.isReady = false;
    player.isSpectator = false;
    return true;
  }

  canStart() {
    if (this.mode === 'training') {
      return this.players.size >= 1;
    }
    if (this.mode === '1v1') {
      return this.players.size === 2 && 
             Array.from(this.players.values()).every(p => p.isReady);
    }
    return false;
  }

  startGame() {
    this.state = 'playing';
    this.frameNumber = 0;
    this.frameInputs = [];
    this.gameStartTime = Date.now();
    
    // 通知所有玩家游戏开始
    const playersData = Array.from(this.players.values()).map(p => p.toJSON());
    this.broadcast(MSG.S_GAME_START, {
      roomId: this.id,
      players: playersData,
      mode: this.mode,
      startTime: this.gameStartTime,
    });
  }

  // 收集当前帧所有玩家的输入
  collectFrameInput() {
    const frameData = {
      frame: this.frameNumber,
      timestamp: Date.now(),
      inputs: {},
    };

    for (const [playerId, player] of this.players) {
      frameData.inputs[playerId] = player.lastInput || { idle: true };
    }

    this.frameInputs.push(frameData);
    return frameData;
  }

  // 广播帧数据给所有玩家和观战者
  broadcastFrame(frameData) {
    this.broadcast(MSG.S_FRAME, { frame: frameData });
  }

  broadcast(type, data = {}) {
    const message = JSON.stringify({ type, ...data });
    
    for (const player of this.players.values()) {
      if (player.ws.readyState === 1) {
        player.ws.send(message);
      }
    }
    
    for (const spectator of this.spectators.values()) {
      if (spectator.ws.readyState === 1) {
        spectator.ws.send(message);
      }
    }
  }

  toJSON() {
    return {
      id: this.id,
      host: this.host,
      mode: this.mode,
      state: this.state,
      players: Array.from(this.players.values()).map(p => p.toJSON()),
      spectatorCount: this.spectators.size,
      frameNumber: this.frameNumber,
    };
  }
}

// ==================== HTTP 服务器 ====================
const server = http.createServer((req, res) => {
  // CORS 头
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

  if (req.method === 'OPTIONS') {
    res.writeHead(200);
    res.end();
    return;
  }

  // API 路由
  if (req.url === '/api/rooms' && req.method === 'GET') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({
      rooms: Array.from(rooms.values()).map(r => r.toJSON()),
      online: players.size,
    }));
    return;
  }

  if (req.url === '/api/status' && req.method === 'GET') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({
      status: 'running',
      uptime: process.uptime(),
      rooms: rooms.size,
      players: players.size,
      matchmaking: matchmakingQueue.length,
    }));
    return;
  }

  // 静态文件
  serveStatic(req, res);
});

// ==================== WebSocket 服务器 ====================

// ==================== 静态文件服务 ====================
const fs = require('fs');
const path = require('path');
const gameDir = path.join(__dirname, '..', 'Code');

function serveStatic(req, res) {
  let filePath = req.url === '/' ? '/index_local_v03_enhanced.html' : req.url;
  filePath = path.join(gameDir, filePath);
  
  // Security: prevent directory traversal
  if (!filePath.startsWith(gameDir)) {
    res.writeHead(403);
    res.end('Forbidden');
    return;
  }
  
  const ext = path.extname(filePath);
  const mimeTypes = {
    '.html': 'text/html',
    '.js': 'text/javascript',
    '.css': 'text/css',
    '.json': 'application/json',
    '.png': 'image/png',
    '.jpg': 'image/jpeg',
    '.svg': 'image/svg+xml',
  };
  
  try {
    const data = fs.readFileSync(filePath);
    res.writeHead(200, { 'Content-Type': mimeTypes[ext] || 'text/plain' });
    res.end(data);
  } catch (err) {
    res.writeHead(404);
    res.end('Not Found');
  }
}

const wss = new WebSocketServer({ server });

wss.on('connection', (ws) => {
  const playerId = nextPlayerId++;
  const player = new Player(ws, playerId);
  players.set(ws, player);

  console.log(`[连接] 玩家 ${playerId} 已连接 (在线: ${players.size})`);

  // 发送欢迎消息
  player.send(MSG.S_WELCOME, {
    playerId,
    playerName: player.name,
    serverTime: Date.now(),
    tickRate: CONFIG.TICK_RATE,
  });

  // 心跳检测
  const heartbeat = setInterval(() => {
    if (Date.now() - player.lastActivity > CONFIG.IDLE_TIMEOUT) {
      ws.close(1000, '空闲超时');
    }
  }, CONFIG.IDLE_TIMEOUT / 2);

  // 消息处理
  ws.on('message', (raw) => {
    try {
      const msg = JSON.parse(raw);
      player.lastActivity = Date.now();
      handleMessage(player, msg);
    } catch (err) {
      player.send(MSG.S_ERROR, { message: '消息格式错误' });
    }
  });

  // 断开连接
  ws.on('close', () => {
    clearInterval(heartbeat);
    handleDisconnect(player);
    players.delete(ws);
    console.log(`[断开] 玩家 ${playerId} 已断开 (在线: ${players.size})`);
  });

  ws.on('error', (err) => {
    console.error(`[错误] 玩家 ${playerId}:`, err.message);
  });
});

// ==================== 消息处理 ====================
function handleMessage(player, msg) {
  switch (msg.type) {
    case MSG.C_JOIN:
      handleJoin(player, msg);
      break;
    case MSG.C_CREATE_ROOM:
      handleCreateRoom(player, msg);
      break;
    case MSG.C_JOIN_ROOM:
      handleJoinRoom(player, msg);
      break;
    case MSG.C_LEAVE_ROOM:
      handleLeaveRoom(player);
      break;
    case MSG.C_READY:
      handleReady(player, msg);
      break;
    case MSG.C_INPUT:
      handleInput(player, msg);
      break;
    case MSG.C_CHAT:
      handleChat(player, msg);
      break;
    case MSG.C_SPECTATE:
      handleSpectate(player, msg);
      break;
    case MSG.C_MATCHMAKE:
      handleMatchmake(player);
      break;
    case MSG.C_TRAINING:
      handleTraining(player);
      break;
    default:
      player.send(MSG.S_ERROR, { message: `未知消息类型: ${msg.type}` });
  }
}

// 加入服务器
function handleJoin(player, msg) {
  if (msg.name) {
    player.name = msg.name;
  }
  if (msg.character) {
    player.character = msg.character;
  }
  player.send(MSG.S_WELCOME, {
    playerId: player.id,
    playerName: player.name,
    serverTime: Date.now(),
    tickRate: CONFIG.TICK_RATE,
  });
  console.log(`[加入] 玩家 ${player.id} 改名为 "${player.name}"`);
}

// 创建房间
function handleCreateRoom(player, msg) {
  if (player.roomId) {
    player.send(MSG.S_ERROR, { message: '你已在房间中，请先离开' });
    return;
  }

  const roomId = nextRoomId++;
  const mode = msg.mode || '1v1';
  const room = new Room(roomId, player, mode);
  rooms.set(roomId, room);

  player.send(MSG.S_ROOM_CREATED, { room: room.toJSON() });
  console.log(`[房间] 玩家 ${player.name} 创建房间 ${roomId} (${mode})`);
}

// 加入房间
function handleJoinRoom(player, msg) {
  if (player.roomId) {
    player.send(MSG.S_ERROR, { message: '你已在房间中，请先离开' });
    return;
  }

  const room = rooms.get(msg.roomId);
  if (!room) {
    player.send(MSG.S_ERROR, { message: '房间不存在' });
    return;
  }

  if (room.state !== 'waiting') {
    player.send(MSG.S_ERROR, { message: '游戏已开始' });
    return;
  }

  if (!room.addPlayer(player)) {
    player.send(MSG.S_ERROR, { message: '房间已满' });
    return;
  }

  // 通知所有人
  player.send(MSG.S_ROOM_JOINED, { room: room.toJSON() });
  room.broadcast(MSG.S_PLAYER_JOINED, { player: player.toJSON() });
  console.log(`[房间] 玩家 ${player.name} 加入房间 ${room.id}`);
}

// 离开房间
function handleLeaveRoom(player) {
  if (!player.roomId) return;

  const room = rooms.get(player.roomId);
  if (!room) return;

  room.removePlayer(player.id);
  room.broadcast(MSG.S_PLAYER_LEFT, { playerId: player.id, playerName: player.name });
  player.send(MSG.S_ROOM_LEFT, { roomId: room.id });

  // 如果房间空了，删除它
  if (room.players.size === 0 && room.spectators.size === 0) {
    rooms.delete(room.id);
    console.log(`[房间] 房间 ${room.id} 已删除（空房间）`);
  }
}

// 准备就绪
function handleReady(player, msg) {
  if (!player.roomId) {
    player.send(MSG.S_ERROR, { message: '你不在房间中' });
    return;
  }

  const room = rooms.get(player.roomId);
  if (!room) return;

  player.isReady = msg.ready !== false;
  player.character = msg.character || player.character;
  player.weaponIndex = msg.weaponIndex || 0;

  room.broadcast(MSG.S_PLAYER_JOINED, { player: player.toJSON() });
  console.log(`[准备] 玩家 ${player.name} ${player.isReady ? '已准备' : '取消准备'}`);

  // 检查是否可以开始
  if (room.canStart()) {
    room.startGame();
    console.log(`[开始] 房间 ${room.id} 游戏开始！`);
  }
}

// 处理输入帧
function handleInput(player, msg) {
  if (!player.roomId) return;

  const room = rooms.get(player.roomId);
  if (!room || room.state !== 'playing') return;

  // 更新玩家的当前输入
  player.lastInput = {
    keys: msg.keys || {},
    mouse: msg.mouse || {},
    timestamp: Date.now(),
  };
}

// 聊天
function handleChat(player, msg) {
  if (!player.roomId) return;

  const room = rooms.get(player.roomId);
  if (!room) return;

  room.broadcast(MSG.S_CHAT, {
    playerId: player.id,
    playerName: player.name,
    message: msg.message,
    timestamp: Date.now(),
  });
}

// 观战
function handleSpectate(player, msg) {
  const room = rooms.get(msg.roomId);
  if (!room) {
    player.send(MSG.S_ERROR, { message: '房间不存在' });
    return;
  }

  if (!room.addSpectator(player)) {
    player.send(MSG.S_ERROR, { message: '观战位已满' });
    return;
  }

  player.send(MSG.S_SPECTATE, {
    room: room.toJSON(),
    frameHistory: room.frameInputs.slice(-100), // 最近100帧
  });
  console.log(`[观战] 玩家 ${player.name} 观战房间 ${room.id}`);
}

// 匹配系统
function handleMatchmake(player) {
  if (player.roomId) {
    player.send(MSG.S_ERROR, { message: '你已在房间中' });
    return;
  }

  if (matchmakingQueue.includes(player)) {
    player.send(MSG.S_ERROR, { message: '已在匹配队列中' });
    return;
  }

  matchmakingQueue.push(player);
  player.send(MSG.S_ROOM_JOINED, { message: '正在匹配中...' });
  console.log(`[匹配] 玩家 ${player.name} 加入匹配队列 (队列: ${matchmakingQueue.length})`);
}

// 训练场
function handleTraining(player) {
  if (player.roomId) {
    handleLeaveRoom(player);
  }

  const roomId = nextRoomId++;
  const room = new Room(roomId, player, 'training');
  rooms.set(roomId, room);

  // 训练场自动开始
  player.isReady = true;
  room.startGame();

  player.send(MSG.S_ROOM_CREATED, {
    room: room.toJSON(),
    message: '已进入训练场',
  });
  console.log(`[训练] 玩家 ${player.name} 进入训练场 ${roomId}`);
}

// 断开连接处理
function handleDisconnect(player) {
  // 从匹配队列移除
  const idx = matchmakingQueue.indexOf(player);
  if (idx !== -1) {
    matchmakingQueue.splice(idx, 1);
  }

  // 离开房间
  if (player.roomId) {
    const room = rooms.get(player.roomId);
    if (room) {
      room.removePlayer(player.id);
      room.broadcast(MSG.S_PLAYER_LEFT, {
        playerId: player.id,
        playerName: player.name,
        reason: '断开连接',
      });

      // 清理空房间
      if (room.players.size === 0 && room.spectators.size === 0) {
        rooms.delete(room.id);
      }
    }
  }
}

// ==================== 游戏循环 ====================
function gameLoop() {
  const now = Date.now();

  for (const room of rooms.values()) {
    if (room.state !== 'playing') continue;

    // 收集并广播帧数据
    const frameData = room.collectFrameInput();
    room.broadcastFrame(frameData);
    room.frameNumber++;
  }
}

// 启动游戏循环
setInterval(gameLoop, CONFIG.FRAME_INTERVAL);

// ==================== 匹配循环 ====================
function matchmakingLoop() {
  while (matchmakingQueue.length >= 2) {
    const player1 = matchmakingQueue.shift();
    const player2 = matchmakingQueue.shift();

    // 检查玩家是否还在线
    if (player1.ws.readyState !== 1 || player2.ws.readyState !== 1) {
      if (player1.ws.readyState === 1) matchmakingQueue.unshift(player1);
      continue;
    }

    // 创建房间
    const roomId = nextRoomId++;
    const room = new Room(roomId, player1, '1v1');
    room.addPlayer(player2);
    rooms.set(roomId, room);

    // 通知双方
    const roomData = room.toJSON();
    player1.send(MSG.S_MATCHED, {
      room: roomData,
      opponent: player2.toJSON(),
    });
    player2.send(MSG.S_MATCHED, {
      room: roomData,
      opponent: player1.toJSON(),
    });

    console.log(`[匹配] 玩家 ${player1.name} vs 玩家 ${player2.name} (房间 ${roomId})`);
  }
}

setInterval(matchmakingLoop, CONFIG.MATCHMAKING_INTERVAL);

// ==================== 房间清理 ====================
function cleanupRooms() {
  for (const [roomId, room] of rooms) {
    // 清理长时间空闲的等待房间
    if (room.state === 'waiting' && Date.now() - room.createdAt > 300000) {
      for (const player of room.players.values()) {
        player.send(MSG.S_ERROR, { message: '房间超时已关闭' });
        player.roomId = null;
      }
      rooms.delete(roomId);
      console.log(`[清理] 房间 ${roomId} 超时已关闭`);
    }
  }
}

setInterval(cleanupRooms, CONFIG.ROOM_CLEANUP_INTERVAL);

// ==================== 启动服务器 ====================
server.listen(CONFIG.PORT, () => {
  console.log('========================================');
  console.log('  Q版方块人大乱斗 - 帧同步服务器 V0.3');
  console.log('========================================');
  console.log(`  WebSocket: ws://localhost:${CONFIG.PORT}`);
  console.log(`  HTTP API:  http://localhost:${CONFIG.PORT}/api/status`);
  console.log(`  帧率: ${CONFIG.TICK_RATE} fps`);
  console.log('========================================');
});

// 优雅关闭
process.on('SIGINT', () => {
  console.log('\n[关闭] 正在关闭服务器...');
  
  for (const player of players.values()) {
    player.send(MSG.S_ERROR, { message: '服务器正在关闭' });
    player.ws.close(1001, '服务器关闭');
  }
  
  wss.close(() => {
    server.close(() => {
      console.log('[关闭] 服务器已关闭');
      process.exit(0);
    });
  });
});
