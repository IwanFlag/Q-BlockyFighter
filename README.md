# Q版方块人大乱斗 · Blocky Fighter

融合中国武术/西方魔法/东方仙术/黑巫术四大体系的多人实时格斗游戏。
Unity 3D + .NET8 帧同步服务器，目标 Steam 上线。

---

## 版本状态

| 版本 | 内容 | 状态 |
|------|------|------|
| V0.1 | Web 原型：基础格斗 | ✅ 已完成 |
| V0.2 | 多角色：4 角色+双武器+连招 | ✅ 已完成 |
| V0.3 | 联机原型：帧同步+大厅+匹配+7 角色 | ✅ 已完成 |
| V0.4 | **Unity 迁移**：引擎迁移+全系统重建 | ✅ 代码完成 |
| V0.5 | 全内容实现：12 角色+5V5+野怪+武器系统 | ⏳ 待开始 |
| V0.6 | 打磨商业化：商城/Battle Pass/音效 | ⏳ 待开始 |
| V1.0 | Steam 上线 | ⏳ 待开始 |

---

## 快速开始

### 1. Web 版（直接体验）

```bash
# 启动服务器
cd Server
npm install
npm start
# 浏览器打开 Code/index_local_v03_enhanced.html
```

### 2. Unity 版（开发中）

```bash
# 启动 .NET 服务器
cd Unity/Server
dotnet restore
dotnet run
# Unity Editor 打开项目，参考 Unity/SCENE_SETUP.md
```

服务器默认 `ws://localhost:3000`，帧率 20fps。

---

## 游戏特色

- **12 角色**：5 个定位（刺客/战士/坦克/远程/辅助）× 4 种力量体系（武术/巫术/魔法/仙术）
- **12 种武器**：每角色双武器切换，切换后首击 +20% 伤害
- **完整战斗**：轻/重攻击、连招、闪避、格挡、弹反、体力、霸体、空中连招
- **4 种模式**：1V1 BO5 决斗、5V5 团战（10 分钟 Boss）、训练场、100 波闯关
- **6 张地图**：龙城演武、罗马竞技场、竹林幽径、北海冰原、长安城、波斯花园
- **排位系统**：7 个段位（初悟→无极）
- **纯外观付费**：皮肤/击杀特效/嘲讽动作，不卖数值

---

## 目录结构

```
Q-BlockyFighter/
├── Unity/                          # Unity 迁移版本（V0.4）
│   ├── Assets/Scripts/
│   │   ├── GameManager.cs          # 游戏总控
│   │   ├── SpectatorController.cs  # 观战系统
│   │   ├── Core/                   # 玩家/战斗/血量/武器/技能/背包/角色数据
│   │   ├── Network/                # WebSocket 客户端/帧同步/协议
│   │   ├── Modes/                  # 1V1/5V5/训练场/闯关
│   │   ├── AI/                     # 敌人 AI + Boss AI
│   │   ├── UI/                     # 主菜单/大厅/HUD/聊天/商城/排位/Battle Pass
│   │   ├── Map/                    # 地图管理/区域控制/野怪据点
│   │   └── Utils/                  # 对象池/配置常量
│   ├── Server/                     # .NET8 帧同步服务器
│   ├── Packages/                   # Unity 包配置
│   ├── ProjectSettings/            # Unity 项目配置
│   └── SCENE_SETUP.md              # Unity 场景搭建指南
├── Code/                           # Web 版原型
│   ├── index_local_v01_enhanced.html
│   ├── index_local_v02_enhanced.html
│   └── index_local_v03_enhanced.html
├── Client/                         # Web 版客户端
├── Server/                         # Node.js 版服务器
├── Doc/
│   ├── Q版方块人大乱斗计划书.md     # 游戏设计文档
│   └── 项目执行计划书模版/          # 项目管理文档
│       ├── 00-项目文档体系.md
│       ├── 01-项目商业计划书.md
│       ├── 02-项目计划书.md
│       ├── 03-项目实施手册.md
│       ├── 04-项目产品使用手册.md
│       └── 05-项目验收意见书.md
└── README.md
```

---

## WebSocket 消息协议

### 客户端 → 服务器

| 类型 | 说明 |
|------|------|
| `join` | 设置玩家名称/角色 |
| `create_room` | 创建房间（mode: 1v1/5v5） |
| `join_room` | 加入房间 |
| `leave_room` | 离开房间 |
| `ready` | 准备就绪 |
| `input` | 发送操作输入（keys + mouse） |
| `matchmake` | 请求匹配 |
| `training` | 进入训练场 |
| `spectate` | 观战 |
| `chat` | 聊天 |

### 服务器 → 客户端

| 类型 | 说明 |
|------|------|
| `welcome` | 欢迎消息（playerId, tickRate） |
| `room_created` | 房间创建成功 |
| `room_joined` | 加入房间成功 |
| `player_joined` | 新玩家加入 |
| `player_left` | 玩家离开 |
| `game_start` | 游戏开始 |
| `frame` | 帧同步数据（20fps） |
| `matched` | 匹配成功 |
| `spectate` | 进入观战 |
| `chat` | 聊天消息 |
| `error` | 错误信息 |

---

## 操作说明

| 按键 | 功能 |
|------|------|
| WASD | 移动 |
| 鼠标 | 视角旋转 |
| 左键 | 轻攻击 |
| 右键 | 重攻击 |
| Shift | 闪避 |
| 空格 | 格挡 |
| Q/E/R | 技能（伤害/位移/大招） |
| 1/2 | 切换武器 |
| B | 商城 |
| Tab | 排位面板 |
| Enter | 聊天 |
| ESC | 暂停 |

---

## 技术栈

| 层 | 技术 |
|----|------|
| 客户端 | Unity 2022.3 LTS / C# |
| 服务器 | .NET8 / Fleck WebSocket |
| 帧同步 | 20fps 确定性模拟 |
| 数据 | Newtonsoft.Json |
| 版本控制 | Git |

---

## License

Private Project — All Rights Reserved.
