/**
 * Q版方块人大乱斗 - 帧同步服务器 V0.4
 * 
 * 功能：
 * - WebSocket 实时通信
 * - 房间管理（创建/加入/离开）
 * - 帧同步（确定性锁步）
 * - 1V1 匹配系统
 * - 吃鸡模式（10人双人组队）
 * - 训练场模式
 * - 观战系统
 */

const { WebSocketServer } = require('ws');
const http = require('http');
const crypto = require('crypto');

// ==================== 配置 ====================
const CONFIG = {
  PORT: 3000,
  TICK_RATE: 20,
  FRAME_INTERVAL: 1000 / 20,
  MAX_PLAYERS_PER_ROOM: 2,
  MAX_BR_PLAYERS: 10,               // 吃鸡模式最大玩家数
  MAX_BR_TEAM_SIZE: 2,              // 吃鸡队伍大小
  MAX_SPECTATORS: 10,
  MATCHMAKING_INTERVAL: 1000,
  BR_MATCHMAKING_INTERVAL: 2000,    // 吃鸡匹配间隔
  ROOM_CLEANUP_INTERVAL: 30000,
  IDLE_TIMEOUT: 60000,
  BR_MIN_PLAYERS_TO_START: 2,       // 吃鸡最少启动人数（不够用Bot填）
};

// ==================== 全局状态 ====================
const rooms = new Map();
const players = new Map();
const matchmakingQueue = [];
const brMatchmakingQueue = [];       // 吃鸡匹配队列
let nextRoomId = 1;
let nextPlayerId = 1;

// ==================== 消息类型 ====================
const MSG = {
  // 客户端 -> 服务器
  C_JOIN: 'join',
  C_CREATE_ROOM: 'create_room',
  C_JOIN_ROOM: 'join_room',
  C_LEAVE_ROOM: 'leave_room',
  C_READY: 'ready',
  C_INPUT: 'input',
  C_CHAT: 'chat',
  C_SPECTATE: 'spectate',
  C_MATCHMAKE: 'matchmake',
  C_TRAINING: 'training',
  C_BR_MATCHMAKE: 'br_matchmake',   // 吃鸡匹配
  C_BR_ACTION: 'br_action',         // 吃鸡交互（拾取/NPC/探索）

  // 服务器 -> 客户端
  S_WELCOME: 'welcome',
  S_ROOM_CREATED: 'room_created',
  S_ROOM_JOINED: 'room_joined',
  S_ROOM_LEFT: 'room_left',
  S_PLAYER_JOINED: 'player_joined',
  S_PLAYER_LEFT: 'player_left',
  S_GAME_START: 'game_start',
  S_FRAME: 'frame',
  S_GAME_STATE: 'game_state',
  S_CHAT: 'chat',
  S_MATCHED: 'matched',
  S_ERROR: 'error',
  S_ROOM_LIST: 'room_list',
  S_SPECTATE: 'spectate',
  S_BR_MATCHED: 'br_matched',       // 吃鸡匹配成功
  S_BR_STATE: 'br_state',           // 吃鸡状态更新（缩圈/击杀/排名）
  S_BR_TEAM_UPDATE: 'br_team_update', // 队伍状态更新
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
    this.character = null;
    this.weaponIndex = 0;
    this.lastInput = null;
    this.lastActivity = Date.now();

    // 吃鸡模式
    this.teamId = -1;
    this.gold = 0;
    this.kills = 0;
    this.isAlive = true;
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
      teamId: this.teamId,
      gold: this.gold,
      kills: this.kills,
      isAlive: this.isAlive,
    };
  }
}

// ==================== 房间类 ====================
class Room {
  constructor(id, host, mode = '1v1') {
    this.id = id;
    this.host = host.id;
    this.mode = mode;               // '1v1' | 'training' | 'battleroyale'
    this.players = new Map();
    this.spectators = new Map();
    this.state = 'waiting';
    this.frameNumber = 0;
    this.frameInputs = [];
    this.gameStartTime = null;
    this.createdAt = Date.now();

    // 吃鸡模式专用
    this.teams = [];                // [{teamId, members: [playerId], aliveCount, kills}]
    this.safeZone = { radius: 60, centerX: 0, centerZ: 0, shrinking: false };
    this.aliveCount = 0;
    this.gameEvents = [];           // 游戏事件日志

    this.addPlayer(host);
  }

  addPlayer(player) {
    if (this.mode === '1v1' && this.players.size >= CONFIG.MAX_PLAYERS_PER_ROOM) {
      return false;
    }
    if (this.mode === 'battleroyale' && this.players.size >= CONFIG.MAX_BR_PLAYERS) {
      return false;
    }
    this.players.set(player.id, player);
    player.roomId = this.id;
    player.isSpectator = false;
    return true;
  }

  addSpectator(player) {
    if (this.spectators.size >= CONFIG.MAX_SPECTATORS) return false;
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
    if (this.mode === 'battleroyale') {
      return this.players.size >= CONFIG.BR_MIN_PLAYERS_TO_START &&
        Array.from(this.players.values()).every(p => p.isReady);
    }
    return false;
  }

  startGame() {
    this.state = 'playing';
    this.frameNumber = 0;
    this.frameInputs = [];
    this.gameStartTime = Date.now();

    if (this.mode === 'battleroyale') {
      this.initBattleRoyale();
    }

    const playersData = Array.from(this.players.values()).map(p => p.toJSON());
    this.broadcast(MSG.S_GAME_START, {
      roomId: this.id,
      players: playersData,
      mode: this.mode,
      startTime: this.gameStartTime,
      teams: this.mode === 'battleroyale' ? this.teams : undefined,
    });
  }

  // ===== 吃鸡模式初始化 =====
  initBattleRoyale() {
    const playerList = Array.from(this.players.values());
    const teamSize = CONFIG.MAX_BR_TEAM_SIZE;
    const teamCount = Math.ceil(playerList.length / teamSize);

    this.teams = [];
    for (let i = 0; i < teamCount; i++) {
      this.teams.push({
        teamId: i,
        teamName: `队伍${i + 1}`,
        members: [],
        aliveCount: 0,
        kills: 0,
      });
    }

    // 分配队伍
    playerList.forEach((player, idx) => {
      const teamIdx = Math.floor(idx / teamSize);
      if (teamIdx < this.teams.length) {
        player.teamId = teamIdx;
        this.teams[teamIdx].members.push(player.id);
        this.teams[teamIdx].aliveCount++;
      }
    });

    this.aliveCount = playerList.length;

    // 广播队伍信息
    this.broadcast(MSG.S_BR_TEAM_UPDATE, {
      teams: this.teams.map(t => ({
        teamId: t.teamId,
        teamName: t.teamName,
        members: t.members.map(pid => {
          const p = this.players.get(pid);
          return p ? p.toJSON() : null;
        }).filter(Boolean),
      })),
    });

    console.log(`[吃鸡] 房间${this.id} 吃鸡初始化完成 - ${this.teams.length}队`);
  }

  // ===== 吃鸡事件处理 =====
  handleBRAction(player, msg) {
    if (this.mode !== 'battleroyale') return;

    const action = msg.action;
    switch (action) {
      case 'pickup_gold':
        player.gold += msg.amount || 10;
        this.broadcast(MSG.S_BR_STATE, {
          event: 'gold_pickup',
          playerId: player.id,
          playerName: player.name,
          gold: player.gold,
          amount: msg.amount || 10,
        });
        break;

      case 'pickup_chest':
        this.broadcast(MSG.S_BR_STATE, {
          event: 'chest_opened',
          playerId: player.id,
          playerName: player.name,
          isLegendary: msg.isLegendary || false,
        });
        break;

      case 'explore_ruin':
        this.broadcast(MSG.S_BR_STATE, {
          event: 'ruin_explored',
          playerId: player.id,
          playerName: player.name,
          ruinName: msg.ruinName || '遗迹',
        });
        break;

      case 'kill_monster':
        player.gold += msg.goldDrop || 20;
        this.broadcast(MSG.S_BR_STATE, {
          event: 'monster_killed',
          playerId: player.id,
          playerName: player.name,
          monsterType: msg.monsterType || 'normal',
        });
        break;

      case 'npc_interact':
        this.broadcast(MSG.S_BR_STATE, {
          event: 'npc_interact',
          playerId: player.id,
          playerName: player.name,
          npcType: msg.npcType || 'merchant',
          npcName: msg.npcName || '商人',
        });
        break;

      case 'player_death':
        player.isAlive = false;
        if (player.teamId >= 0 && player.teamId < this.teams.length) {
          this.teams[player.teamId].aliveCount = Math.max(0, this.teams[player.teamId].aliveCount - 1);
        }
        this.aliveCount--;

        // 记录击杀
        if (msg.killerId) {
          const killer = this.players.get(msg.killerId);
          if (killer) {
            killer.kills++;
            killer.gold += 50;
            if (killer.teamId >= 0 && killer.teamId < this.teams.length) {
              this.teams[killer.teamId].kills++;
            }
          }
        }

        // 检查游戏结束
        const survivingTeams = this.teams.filter(t => t.aliveCount > 0);
        this.broadcast(MSG.S_BR_STATE, {
          event: 'player_eliminated',
          playerId: player.id,
          playerName: player.name,
          teamId: player.teamId,
          killerId: msg.killerId,
          aliveCount: this.aliveCount,
          survivingTeams: survivingTeams.length,
        });

        if (survivingTeams.length <= 1) {
          this.endBattleRoyale(survivingTeams[0]);
        }
        break;

      case 'boss_killed':
        this.broadcast(MSG.S_BR_STATE, {
          event: 'boss_killed',
          playerId: player.id,
          playerName: player.name,
          killerTeam: player.teamId,
        });
        break;

      case 'zone_shrink':
        this.safeZone = {
          ...this.safeZone,
          radius: msg.radius || this.safeZone.radius * 0.65,
          centerX: msg.centerX ?? this.safeZone.centerX,
          centerZ: msg.centerZ ?? this.safeZone.centerZ,
          shrinking: true,
        };
        this.broadcast(MSG.S_BR_STATE, {
          event: 'zone_shrinking',
          safeZone: this.safeZone,
        });
        break;
    }
  }

  endBattleRoyale(winningTeam) {
    this.state = 'finished';

    // 计算排名
    const rankings = this.teams
      .sort((a, b) => {
        if (a.aliveCount !== b.aliveCount) return b.aliveCount - a.aliveCount;
        return b.kills - a.kills;
      })
      .map((t, idx) => ({
        rank: idx + 1,
        teamId: t.teamId,
        teamName: t.teamName,
        kills: t.kills,
        members: t.members.map(pid => {
          const p = this.players.get(pid);
          return p ? { id: p.id, name: p.name, gold: p.gold, kills: p.kills } : null;
        }).filter(Boolean),
      }));

    this.broadcast(MSG.S_BR_STATE, {
      event: 'game_over',
      winner: winningTeam ? {
        teamId: winningTeam.teamId,
        teamName: winningTeam.teamName,
        kills: winningTeam.kills,
      } : null,
      rankings,
    });

    console.log(`[吃鸡] 房间${this.id} 游戏结束! 获胜: ${winningTeam ? winningTeam.teamName : '无'}`);
  }

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

  broadcastFrame(frameData) {
    this.broadcast(MSG.S_FRAME, { frame: frameData });
  }

  broadcast(type, data = {}) {
    const message = JSON.stringify({ type, ...data });
    for (const player of this.players.values()) {
      if (player.ws.readyState === 1) player.ws.send(message);
    }
    for (const spectator of this.spectators.values()) {
      if (spectator.ws.readyState === 1) spectator.ws.send(message);
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
      teams: this.mode === 'battleroyale' ? this.teams : undefined,
      safeZone: this.mode === 'battleroyale' ? this.safeZone : undefined,
      aliveCount: this.aliveCount,
    };
  }
}

// ==================== HTTP 服务器 ====================
const server = http.createServer((req, res) => {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

  if (req.method === 'OPTIONS') { res.writeHead(200); res.end(); return; }

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
      brMatchmaking: brMatchmakingQueue.length,
    }));
    return;
  }

  serveStatic(req, res);
});

// ==================== 静态文件 ====================
const fs = require('fs');
const path = require('path');
const gameDir = path.join(__dirname, '..', 'Code');

function serveStatic(req, res) {
  let filePath = req.url === '/' ? '/index_local_v03_enhanced.html' : req.url;
  filePath = path.join(gameDir, filePath);
  if (!filePath.startsWith(gameDir)) { res.writeHead(403); res.end('Forbidden'); return; }

  const ext = path.extname(filePath);
  const mimeTypes = {
    '.html': 'text/html; charset=utf-8',
    '.js': 'text/javascript; charset=utf-8',
    '.css': 'text/css; charset=utf-8',
    '.json': 'application/json; charset=utf-8',
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

// ==================== WebSocket ====================
const wss = new WebSocketServer({ server });

wss.on('connection', (ws) => {
  const playerId = nextPlayerId++;
  const player = new Player(ws, playerId);
  players.set(ws, player);

  console.log(`[连接] 玩家 ${playerId} 已连接 (在线: ${players.size})`);

  player.send(MSG.S_WELCOME, {
    playerId,
    playerName: player.name,
    serverTime: Date.now(),
    tickRate: CONFIG.TICK_RATE,
  });

  const heartbeat = setInterval(() => {
    if (Date.now() - player.lastActivity > CONFIG.IDLE_TIMEOUT) {
      ws.close(1000, '空闲超时');
    }
  }, CONFIG.IDLE_TIMEOUT / 2);

  ws.on('message', (raw) => {
    try {
      const msg = JSON.parse(raw);
      player.lastActivity = Date.now();
      handleMessage(player, msg);
    } catch (err) {
      player.send(MSG.S_ERROR, { message: '消息格式错误' });
    }
  });

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
    case MSG.C_JOIN: handleJoin(player, msg); break;
    case MSG.C_CREATE_ROOM: handleCreateRoom(player, msg); break;
    case MSG.C_JOIN_ROOM: handleJoinRoom(player, msg); break;
    case MSG.C_LEAVE_ROOM: handleLeaveRoom(player); break;
    case MSG.C_READY: handleReady(player, msg); break;
    case MSG.C_INPUT: handleInput(player, msg); break;
    case MSG.C_CHAT: handleChat(player, msg); break;
    case MSG.C_SPECTATE: handleSpectate(player, msg); break;
    case MSG.C_MATCHMAKE: handleMatchmake(player); break;
    case MSG.C_TRAINING: handleTraining(player); break;
    case MSG.C_BR_MATCHMAKE: handleBRMatchmake(player); break;
    case MSG.C_BR_ACTION: handleBRAction(player, msg); break;
    default: player.send(MSG.S_ERROR, { message: `未知消息类型: ${msg.type}` });
  }
}

function handleJoin(player, msg) {
  if (msg.name) player.name = msg.name;
  if (msg.character) player.character = msg.character;
  player.send(MSG.S_WELCOME, {
    playerId: player.id,
    playerName: player.name,
    serverTime: Date.now(),
    tickRate: CONFIG.TICK_RATE,
  });
  console.log(`[加入] 玩家 ${player.id} 改名为 "${player.name}"`);
}

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

function handleJoinRoom(player, msg) {
  if (player.roomId) {
    player.send(MSG.S_ERROR, { message: '你已在房间中，请先离开' });
    return;
  }
  const room = rooms.get(msg.roomId);
  if (!room) { player.send(MSG.S_ERROR, { message: '房间不存在' }); return; }
  if (room.state !== 'waiting') { player.send(MSG.S_ERROR, { message: '游戏已开始' }); return; }
  if (!room.addPlayer(player)) { player.send(MSG.S_ERROR, { message: '房间已满' }); return; }

  player.send(MSG.S_ROOM_JOINED, { room: room.toJSON() });
  room.broadcast(MSG.S_PLAYER_JOINED, { player: player.toJSON() });
  console.log(`[房间] 玩家 ${player.name} 加入房间 ${room.id}`);
}

function handleLeaveRoom(player) {
  if (!player.roomId) return;
  const room = rooms.get(player.roomId);
  if (!room) return;
  room.removePlayer(player.id);
  room.broadcast(MSG.S_PLAYER_LEFT, { playerId: player.id, playerName: player.name });
  player.send(MSG.S_ROOM_LEFT, { roomId: room.id });
  if (room.players.size === 0 && room.spectators.size === 0) {
    rooms.delete(room.id);
  }
}

function handleReady(player, msg) {
  if (!player.roomId) { player.send(MSG.S_ERROR, { message: '你不在房间中' }); return; }
  const room = rooms.get(player.roomId);
  if (!room) return;
  player.isReady = msg.ready !== false;
  player.character = msg.character || player.character;
  player.weaponIndex = msg.weaponIndex || 0;
  room.broadcast(MSG.S_PLAYER_JOINED, { player: player.toJSON() });
  console.log(`[准备] 玩家 ${player.name} ${player.isReady ? '已准备' : '取消准备'}`);
  if (room.canStart()) {
    room.startGame();
    console.log(`[开始] 房间 ${room.id} 游戏开始！`);
  }
}

function handleInput(player, msg) {
  if (!player.roomId) return;
  const room = rooms.get(player.roomId);
  if (!room || room.state !== 'playing') return;
  player.lastInput = {
    keys: msg.keys || {},
    mouse: msg.mouse || {},
    timestamp: Date.now(),
  };
}

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

function handleSpectate(player, msg) {
  const room = rooms.get(msg.roomId);
  if (!room) { player.send(MSG.S_ERROR, { message: '房间不存在' }); return; }
  if (!room.addSpectator(player)) { player.send(MSG.S_ERROR, { message: '观战位已满' }); return; }
  player.send(MSG.S_SPECTATE, {
    room: room.toJSON(),
    frameHistory: room.frameInputs.slice(-100),
  });
}

function handleMatchmake(player) {
  if (player.roomId) { player.send(MSG.S_ERROR, { message: '你已在房间中' }); return; }
  if (matchmakingQueue.includes(player)) { player.send(MSG.S_ERROR, { message: '已在匹配队列中' }); return; }
  matchmakingQueue.push(player);
  player.send(MSG.S_ROOM_JOINED, { message: '正在匹配中...' });
}

function handleTraining(player) {
  if (player.roomId) handleLeaveRoom(player);
  const roomId = nextRoomId++;
  const room = new Room(roomId, player, 'training');
  rooms.set(roomId, room);
  player.isReady = true;
  room.startGame();
  player.send(MSG.S_ROOM_CREATED, { room: room.toJSON(), message: '已进入训练场' });
}

// ===== 吃鸡匹配 =====
function handleBRMatchmake(player) {
  if (player.roomId) { player.send(MSG.S_ERROR, { message: '你已在房间中' }); return; }
  if (brMatchmakingQueue.includes(player)) { player.send(MSG.S_ERROR, { message: '已在吃鸡匹配队列中' }); return; }
  brMatchmakingQueue.push(player);
  player.send(MSG.S_ROOM_JOINED, { message: '正在匹配吃鸡模式...' });
  console.log(`[吃鸡匹配] 玩家 ${player.name} 加入吃鸡队列 (队列: ${brMatchmakingQueue.length})`);
}

// ===== 吃鸡交互 =====
function handleBRAction(player, msg) {
  if (!player.roomId) return;
  const room = rooms.get(player.roomId);
  if (!room || room.mode !== 'battleroyale') return;
  room.handleBRAction(player, msg);
}

function handleDisconnect(player) {
  const idx = matchmakingQueue.indexOf(player);
  if (idx !== -1) matchmakingQueue.splice(idx, 1);

  const brIdx = brMatchmakingQueue.indexOf(player);
  if (brIdx !== -1) brMatchmakingQueue.splice(brIdx, 1);

  if (player.roomId) {
    const room = rooms.get(player.roomId);
    if (room) {
      room.removePlayer(player.id);
      room.broadcast(MSG.S_PLAYER_LEFT, {
        playerId: player.id,
        playerName: player.name,
        reason: '断开连接',
      });
      if (room.players.size === 0 && room.spectators.size === 0) {
        rooms.delete(room.id);
      }
    }
  }
}

// ==================== 游戏循环 ====================
function gameLoop() {
  for (const room of rooms.values()) {
    if (room.state !== 'playing') continue;
    const frameData = room.collectFrameInput();
    room.broadcastFrame(frameData);
    room.frameNumber++;
  }
}
setInterval(gameLoop, CONFIG.FRAME_INTERVAL);

// ==================== 1V1匹配 ====================
function matchmakingLoop() {
  while (matchmakingQueue.length >= 2) {
    const p1 = matchmakingQueue.shift();
    const p2 = matchmakingQueue.shift();
    if (p1.ws.readyState !== 1 || p2.ws.readyState !== 1) {
      if (p1.ws.readyState === 1) matchmakingQueue.unshift(p1);
      continue;
    }
    const roomId = nextRoomId++;
    const room = new Room(roomId, p1, '1v1');
    room.addPlayer(p2);
    rooms.set(roomId, room);
    const roomData = room.toJSON();
    p1.send(MSG.S_MATCHED, { room: roomData, opponent: p2.toJSON() });
    p2.send(MSG.S_MATCHED, { room: roomData, opponent: p1.toJSON() });
    console.log(`[匹配] ${p1.name} vs ${p2.name} (房间 ${roomId})`);
  }
}
setInterval(matchmakingLoop, CONFIG.MATCHMAKING_INTERVAL);

// ==================== 吃鸡匹配 ====================
function brMatchmakingLoop() {
  // 最少2人/1队开始，不够的用Bot填满到10人
  if (brMatchmakingQueue.length >= CONFIG.BR_MIN_PLAYERS_TO_START) {
    const brPlayers = [];
    while (brMatchmakingQueue.length > 0 && brPlayers.length < CONFIG.MAX_BR_PLAYERS) {
      const p = brMatchmakingQueue.shift();
      if (p.ws.readyState === 1) brPlayers.push(p);
    }

    if (brPlayers.length < 1) return;

    const roomId = nextRoomId++;
    const room = new Room(roomId, brPlayers[0], 'battleroyale');
    for (let i = 1; i < brPlayers.length; i++) {
      room.addPlayer(brPlayers[i]);
    }
    rooms.set(roomId, room);

    // 通知所有玩家
    const roomData = room.toJSON();
    for (const p of brPlayers) {
      p.send(MSG.S_BR_MATCHED, {
        room: roomData,
        playerCount: brPlayers.length,
        botCount: CONFIG.MAX_BR_PLAYERS - brPlayers.length,
        message: `吃鸡匹配成功! ${brPlayers.length}人 + ${CONFIG.MAX_BR_PLAYERS - brPlayers.length}Bot`,
      });
    }

    console.log(`[吃鸡匹配] 房间${roomId} 匹配成功 - ${brPlayers.length}人 + ${CONFIG.MAX_BR_PLAYERS - brPlayers.length}Bot`);
  }
}
setInterval(brMatchmakingLoop, CONFIG.BR_MATCHMAKING_INTERVAL);

// ==================== 房间清理 ====================
function cleanupRooms() {
  for (const [roomId, room] of rooms) {
    if (room.state === 'waiting' && Date.now() - room.createdAt > 300000) {
      for (const player of room.players.values()) {
        player.send(MSG.S_ERROR, { message: '房间超时已关闭' });
        player.roomId = null;
      }
      rooms.delete(roomId);
    }
  }
}
setInterval(cleanupRooms, CONFIG.ROOM_CLEANUP_INTERVAL);

// ==================== 启动 ====================
server.listen(CONFIG.PORT, '0.0.0.0', () => {
  console.log('========================================');
  console.log('  Q版方块人大乱斗 - 帧同步服务器 V0.4');
  console.log('========================================');
  console.log(`  WebSocket: ws://localhost:${CONFIG.PORT}`);
  console.log(`  HTTP API:  http://localhost:${CONFIG.PORT}/api/status`);
  console.log(`  帧率: ${CONFIG.TICK_RATE} fps`);
  console.log(`  吃鸡: ${CONFIG.MAX_BR_PLAYERS}人/${CONFIG.MAX_BR_TEAM_SIZE}人组队`);
  console.log('========================================');
});

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
