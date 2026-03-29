# Q版方块人大乱斗 · Blocky Fighter

融合中国武术/西方魔法/东方仙术/黑巫术四大体系的多人实时格斗游戏。

---

## 项目结构

```
Q-BlockyFighter/
├── web/                            # 🌐 Web 原型（演示用）
│   ├── index_local_v01_enhanced.html
│   ├── index_local_v02_enhanced.html
│   ├── index_local_v03_enhanced.html
│   └── three.min.js
├── Desktop/                        # 🖥️ PC 桌面版（重点交付）
│   ├── main.js                     # Electron 主进程
│   ├── preload.js                  # 安全桥接
│   ├── launcher.html               # 启动器（Guest/登录）
│   ├── game.html                   # 游戏主文件（PC独立版）
│   ├── three.min.js
│   ├── Client/                     # 客户端模块
│   └── package.json
├── Unity/                          # 🎮 Unity 版本（V0.4，开发中）
│   └── Assets/Scripts/
├── Server/                         # 🔌 Node.js 帧同步服务器
├── Doc/                            # 📄 文档
└── README.md
```

---

## 快速开始

### PC 桌面版（推荐）

```bash
cd Desktop
npm install
npx electron .
```

弹出启动器 → 点「游客模式」→ 进入游戏

**操作：**
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

### Web 原型

```bash
cd Server
npm install
npm start
# 浏览器打开 web/index_local_v03_enhanced.html
```

---

## 版本状态

| 版本 | 内容 | 状态 |
|------|------|------|
| V0.1 | Web 原型：基础格斗 | ✅ 已完成 |
| V0.2 | 多角色：4 角色+双武器+连招 | ✅ 已完成 |
| V0.3 | 联机原型：帧同步+大厅+7 角色 | ✅ 已完成 |
| V0.7 | PC 桌面版：Electron + 启动器 | ✅ 已完成 |
| V0.8 | RPG 战斗：武器拾取换技能+缴械 | ⏳ 开发中 |
| V0.9 | 5V5 DOTA 模式 | ⏳ 待开始 |
| V1.0 | 10 人吃鸡 + 完整内容 | ⏳ 待开始 |

---

## 游戏特色

- **12 角色**：5 个定位（刺客/战士/坦克/远程/辅助）× 4 种力量体系
- **12 种武器**：捡到武器即获得该武器的 Q/E/R 技能
- **缴械系统**：连击打落对手武器，回到徒手战斗
- **4 种模式**：1V1 / 训练场 / 挑战 / Boss 模拟
- **纯外观付费**：皮肤/击杀特效/嘲讽动作，不卖数值

---

## License

Private Project — All Rights Reserved.
