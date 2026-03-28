# Q版方块人大乱斗 · 服务器部署指南

## 环境要求

- Node.js v18+ 
- npm v9+

## 快速启动

```bash
# 1. 进入服务器目录
cd Server

# 2. 安装依赖（首次）
npm install

# 3. 启动服务器
npm start
# 或使用开发模式（自动重启）
npm run dev
```

启动后会看到：
```
========================================
  Q版方块人大乱斗 - 帧同步服务器 V0.3
========================================
  WebSocket: ws://localhost:3000
  HTTP API:  http://localhost:3000/api/status
  帧率: 20 fps
========================================
```

## API 接口

| 接口 | 方法 | 说明 |
|------|------|------|
| `/api/status` | GET | 服务器状态（在线人数、房间数等） |
| `/api/rooms` | GET | 房间列表 |

## WebSocket 消息类型

### 客户端 → 服务器

| 类型 | 说明 |
|------|------|
| `join` | 设置玩家名称/角色 |
| `create_room` | 创建房间 |
| `join_room` | 加入房间 |
| `leave_room` | 离开房间 |
| `ready` | 准备就绪 |
| `input` | 发送操作输入 |
| `matchmake` | 请求匹配 |
| `training` | 进入训练场 |
| `spectate` | 观战 |
| `chat` | 聊天 |

### 服务器 → 客户端

| 类型 | 说明 |
|------|------|
| `welcome` | 欢迎消息（含 playerId） |
| `room_created` | 房间创建成功 |
| `room_joined` | 加入房间成功 |
| `game_start` | 游戏开始 |
| `frame` | 帧同步数据 |
| `matched` | 匹配成功 |
| `error` | 错误信息 |

## 游戏访问

1. 启动服务器
2. 用浏览器打开 `Code/index_local_v03_enhanced.html`
3. 输入名字，选择角色，开始游戏

## 目录结构

```
Q版本流星蝴蝶剑/
├── Code/
│   ├── index_local_v01_enhanced.html  # V0.1 原型
│   ├── index_local_v02_enhanced.html  # V0.2 多角色
│   └── index_local_v03_enhanced.html  # V0.3 联机版
├── Client/
│   ├── network.js                     # 网络模块
│   └── lobby.js                       # 大厅 UI
├── Server/
│   ├── server.js                      # 帧同步服务器
│   ├── package.json                   # 依赖配置
│   └── node_modules/                  # 依赖包
├── Doc/
│   └── Q版方块人大乱斗计划书.md       # 游戏设计文档
└── README.md                          # 本文件
```

## 版本历史

- **V0.1** - Web 原型：基础格斗（跑/砍/闪/挡）
- **V0.2** - 多角色：4角色+双武器+连招+武器品质
- **V0.3** - 联机原型：帧同步+大厅+匹配+7角色
- **V0.4** - 进行中：更多角色+地图+完善部署
