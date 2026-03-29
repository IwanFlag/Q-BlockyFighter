# Unity场景搭建指南

## 快速开始

### 1. 创建Unity项目
1. Unity Hub → 新建项目 → **3D (URP)** 模板
2. 项目名: `Q-BlockyFighter`
3. Unity版本: **2022.3 LTS** 或更新

### 2. 导入代码
将 `Unity/Assets/Scripts/` 整个文件夹拖入Unity的Assets窗口。

### 3. 安装依赖
打开 `Window → Package Manager`，确认安装：
- ✅ Input System (com.unity.inputsystem)
- ✅ TextMeshPro (com.unity.textmeshpro)
- ✅ Newtonsoft JSON (com.unity.nuget.newtonsoft-json)

### 4. 创建主场景

#### 4.1 创建场景
`File → New Scene → Basic (Built-in)` → 保存为 `Assets/Scenes/MainScene`

#### 4.2 创建GameManager
1. `GameObject → Create Empty` → 命名 `GameManager`
2. 添加组件: 拖入 `GameManager.cs`
3. 设置ServerUrl: `ws://localhost:3000`

#### 4.3 创建EventSystem（UI需要）
1. `GameObject → UI → Event System`

#### 4.4 创建方向光
1. `GameObject → Light → Directional Light`
2. Rotation: `(50, -30, 0)`

#### 4.5 摄像机设置
1. 选中Main Camera
2. Position: `(0, 15, -10)`
3. Rotation: `(60, 0, 0)`
4. Field of View: `60`

### 5. Prefab创建

#### 5.1 玩家Prefab
1. `GameObject → 3D Object → Capsule` → 命名 `PlayerPrefab`
2. 添加组件:
   - `PlayerController.cs`
   - `HealthSystem.cs`
   - `CombatSystem.cs`
   - `WeaponSystem.cs`
   - `SkillSystem.cs`
   - `Rigidbody` (Freeze Rotation X, Z)
   - `Capsule Collider`
3. Tag设为 `Player`
4. 拖入 `Assets/Prefabs/` 文件夹
5. 从场景中删除

#### 5.2 武器掉落Prefab
1. `GameObject → 3D Object → Cylinder` → 命名 `WeaponDropPrefab`
2. Scale: `(0.8, 0.3, 0.8)`
3. 添加 `WeaponPickup.cs`
4. Collider 设为 Is Trigger
5. 拖入 `Assets/Prefabs/`

#### 5.3 训练假人Prefab
1. `GameObject → 3D Object → Cylinder` → 命名 `DummyPrefab`
2. Scale: `(1, 1.5, 1)`
3. 添加 `HealthSystem.cs`
4. Tag: `Enemy`
5. 拖入 `Assets/Prefabs/`

### 6. 启动服务器

```bash
cd Unity/Server
dotnet restore
dotnet run
```

服务器启动在 `ws://localhost:3000`

### 7. 运行游戏
1. Unity中点击 **Play**
2. 主菜单会出现
3. 输入名字，选择模式，选择角色
4. 开始战斗！

---

## 地图预制体

### 1V1地图 - 龙城演武
- 地面: Cube, Scale(24, 0.5, 24), 棕色
- 4根柱子: Cylinder, Scale(0.8, 3, 0.8), 红色, 位置(±6, 1.5, ±6)
- 4个灯笼: Cylinder, Scale(0.3, 1, 0.3), 橙色, 位置(±5, 1, ±5)

### 5V5地图 - 北海冰原
- 地面: Cube, Scale(50, 0.1, 50), 浅蓝色
- 冰柱x8: Cylinder, Scale(1.5, 4, 1.5), 半透明蓝
- 冰山x2: Cube, Scale(6, 4, 5), 白色

---

## 输入绑定

| 按键 | 功能 |
|------|------|
| WASD | 移动 |
| 鼠标 | 视角旋转 |
| 左键 | 轻攻击 |
| 右键 | 重攻击 |
| Shift | 闪避 |
| 空格 | 格挡 |
| Q/E/R | 技能 |
| 1/2 | 切换武器 |
| B | 商城 |
| Tab | 排位面板 |
| Enter | 聊天 |
| ESC | 暂停 |

---

## 服务端API

| 消息类型 | 方向 | 说明 |
|----------|------|------|
| `join` | C→S | 设置玩家名字 |
| `create_room` | C→S | 创建房间 |
| `join_room` | C→S | 加入房间 |
| `ready` | C→S | 准备 |
| `input` | C→S | 发送操作输入 |
| `chat` | C→S | 发送聊天 |
| `matchmake` | C→S | 匹配 |
| `training` | C→S | 进入训练场 |
| `spectate` | C→S | 观战 |
| `welcome` | S→C | 欢迎消息(含playerId) |
| `room_created` | S→C | 房间创建成功 |
| `player_joined` | S→C | 玩家加入 |
| `game_start` | S→C | 游戏开始 |
| `frame` | S→C | 帧同步数据 |
| `matched` | S→C | 匹配成功 |

---

## 文件结构

```
Unity/
├── Assets/
│   ├── Scenes/
│   │   └── MainScene.unity
│   ├── Prefabs/
│   │   ├── PlayerPrefab.prefab
│   │   ├── WeaponDropPrefab.prefab
│   │   └── DummyPrefab.prefab
│   └── Scripts/
│       ├── GameManager.cs              ← 游戏总控
│       ├── SpectatorController.cs      ← 观战系统
│       ├── Core/
│       │   ├── PlayerController.cs     ← 玩家控制
│       │   ├── CombatSystem.cs         ← 战斗系统
│       │   ├── HealthSystem.cs         ← 血量体力
│       │   ├── WeaponSystem.cs         ← 武器系统
│       │   ├── WeaponDropSystem.cs     ← 武器掉落
│       │   ├── SkillSystem.cs          ← 技能系统
│       │   ├── InventorySystem.cs      ← 背包系统
│       │   └── CharacterData.cs        ← 角色数据(12角色)
│       ├── Network/
│       │   ├── GameClient.cs           ← WebSocket客户端
│       │   ├── FrameSyncManager.cs     ← 帧同步
│       │   └── Protocol.cs             ← 协议定义
│       ├── Modes/
│       │   ├── GameModeManager.cs      ← 模式基类
│       │   ├── DuelMode.cs             ← 1V1
│       │   ├── TeamBattleMode.cs       ← 5V5
│       │   ├── TrainingMode.cs         ← 训练场
│       │   └── ChallengeMode.cs        ← 闯关
│       ├── AI/
│       │   ├── EnemyAI.cs              ← 敌人AI
│       │   └── BossAI.cs               ← Boss AI
│       ├── UI/
│       │   ├── MainMenuUI.cs           ← 主菜单
│       │   ├── LobbyUI.cs              ← 大厅
│       │   ├── CharacterSelectUI.cs    ← 角色选择
│       │   ├── HUDManager.cs           ← 战斗HUD
│       │   ├── ChatUI.cs               ← 聊天
│       │   ├── ShopUI.cs               ← 商城
│       │   ├── RankUI.cs               ← 排位
│       │   └── BattlePassUI.cs         ← 战斗通行证
│       ├── Map/
│       │   ├── MapManager.cs           ← 地图管理
│       │   ├── ZoneController.cs       ← 区域/缩圈
│       │   └── WildMonsterSpawner.cs   ← 野怪/据点
│       └── Utils/
│           ├── ObjectPool.cs           ← 对象池
│           └── GameConfig.cs           ← 配置常量
└── Server/
    ├── Server.csproj
    ├── Program.cs
    ├── Network/
    │   ├── WebSocketServer.cs
    │   ├── Player.cs
    │   ├── Room.cs
    │   ├── Matchmaker.cs
    │   └── GameLoop.cs
    └── Protocol/
        └── Messages.cs
```
