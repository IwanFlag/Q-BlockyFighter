using UnityEngine;
using System.Collections.Generic;
using QBlockyFighter.Core;
using QBlockyFighter.Network;
using QBlockyFighter.Modes;
using QBlockyFighter.UI;
using QBlockyFighter.Map;
using QBlockyFighter.AI;
using QBlockyFighter.Utils;

namespace QBlockyFighter
{
    /// <summary>
    /// 游戏总管理器 - 负责初始化所有系统、管理游戏状态流转
    /// 挂载到场景中的 GameManager GameObject 上
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ===== 游戏状态 =====
        public enum GameState { MainMenu, CharacterSelect, Connecting, Playing, Paused, GameOver, Spectating }
        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        // ===== 引用 =====
        [Header("Network")]
        public string ServerUrl = "ws://localhost:3000";

        // UI组件（自动获取）
        private MainMenuUI mainMenuUI;
        private CharacterSelectUI charSelectUI;
        private LobbyUI lobbyUI;
        private HUDManager hudManager;
        private ChatUI chatUI;
        private ShopUI shopUI;
        private RankUI rankUI;

        // 核心系统
        private GameClient networkClient;
        private FrameSyncManager frameSync;
        private MapManager mapManager;
        private ZoneController zoneController;

        // 当前玩家
        private GameObject localPlayer;
        private PlayerController localPlayerController;
        private HealthSystem localHealth;
        private CombatSystem localCombat;
        private WeaponSystem localWeapons;
        private SkillSystem localSkills;

        // 游戏模式
        private GameMode currentMode;

        // 远端玩家
        private Dictionary<int, GameObject> remotePlayers = new Dictionary<int, GameObject>();

        // 当前选择
        private int selectedCharIndex = 0;
        private int selectedMapIndex = 0;
        private string selectedMode = "1v1";

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            InitSystems();
            SetState(GameState.MainMenu);
        }

        private void InitSystems()
        {
            // 创建或获取组件
            mainMenuUI = GetOrCreateComponent<MainMenuUI>();
            charSelectUI = GetOrCreateComponent<CharacterSelectUI>();
            lobbyUI = GetOrCreateComponent<LobbyUI>();
            hudManager = GetOrCreateComponent<HUDManager>();
            chatUI = GetOrCreateComponent<ChatUI>();
            shopUI = GetOrCreateComponent<ShopUI>();
            rankUI = GetOrCreateComponent<RankUI>();

            // 网络
            networkClient = GetOrCreateComponent<GameClient>();
            frameSync = GetOrCreateComponent<FrameSyncManager>();

            // 地图
            mapManager = GetOrCreateComponent<MapManager>();
            zoneController = GetOrCreateComponent<ZoneController>();

            // 连接网络事件
            networkClient.OnConnected += OnNetworkConnected;
            networkClient.OnDisconnected += OnNetworkDisconnected;
            networkClient.OnGameStart += OnGameStart;
            networkClient.OnFrameReceived += OnFrameReceived;
            networkClient.OnPlayerJoined += OnRemotePlayerJoined;
            networkClient.OnPlayerLeft += OnRemotePlayerLeft;
            networkClient.OnMatched += OnMatched;

            // UI事件
            charSelectUI.OnCharacterConfirmed += OnCharacterConfirmed;
            chatUI.OnChatSent += OnChatSent;

            // 主菜单引用
            mainMenuUI.networkClient = networkClient;
            lobbyUI.networkClient = networkClient;

            Debug.Log("[Game] 所有系统初始化完成");
        }

        private T GetOrCreateComponent<T>() where T : MonoBehaviour
        {
            T comp = GetComponent<T>();
            if (comp == null) comp = gameObject.AddComponent<T>();
            return comp;
        }

        // ===== 状态管理 =====
        public void SetState(GameState newState)
        {
            GameState oldState = CurrentState;
            CurrentState = newState;
            Debug.Log($"[Game] 状态切换: {oldState} → {newState}");

            // 隐藏所有UI
            mainMenuUI.Hide();
            charSelectUI.Hide();
            lobbyUI.Hide();
            shopUI.Hide();

            switch (newState)
            {
                case GameState.MainMenu:
                    mainMenuUI.Show();
                    SetCursorLock(false);
                    break;

                case GameState.CharacterSelect:
                    charSelectUI.Show(selectedMode);
                    SetCursorLock(false);
                    break;

                case GameState.Connecting:
                    // 显示连接中提示
                    hudManager.ShowStatus("连接服务器中...", 10f);
                    break;

                case GameState.Playing:
                    hudManager.ShowStatus("战斗开始!", 2f);
                    SetCursorLock(true);
                    break;

                case GameState.Paused:
                    SetCursorLock(false);
                    break;

                case GameState.GameOver:
                    SetCursorLock(false);
                    string winner;
                    if (currentMode != null && currentMode.CheckGameOver(out winner))
                    {
                        hudManager.ShowStatus($"游戏结束 - {winner} 获胜!", 5f);
                        // 更新排位
                        rankUI.UpdateAfterMatch(winner == "玩家1");
                    }
                    Invoke(nameof(ReturnToMenu), 5f);
                    break;

                case GameState.Spectating:
                    SetCursorLock(true);
                    break;
            }
        }

        // ===== 网络事件 =====
        private void OnNetworkConnected()
        {
            Debug.Log("[Game] 已连接到服务器");
            if (CurrentState == GameState.Connecting)
            {
                SetState(GameState.CharacterSelect);
            }
        }

        private void OnNetworkDisconnected()
        {
            Debug.Log("[Game] 与服务器断开");
            SetState(GameState.MainMenu);
            CleanupGame();
        }

        private void OnMatched()
        {
            Debug.Log("[Game] 匹配成功!");
            SetState(GameState.CharacterSelect);
        }

        private void OnGameStart()
        {
            Debug.Log("[Game] 游戏开始信号");
            StartLocalGame();
        }

        private void OnFrameReceived(int frame, Dictionary<int, object> inputs)
        {
            frameSync?.OnFrameReceived(frame, inputs);
        }

        private void OnRemotePlayerJoined(int playerId, string playerName)
        {
            if (remotePlayers.ContainsKey(playerId)) return;
            var remote = CreatePlayerObject(playerId, playerName);
            remotePlayers[playerId] = remote;
            Debug.Log($"[Game] 远端玩家加入: {playerName} ({playerId})");
        }

        private void OnRemotePlayerLeft(int playerId)
        {
            if (remotePlayers.TryGetValue(playerId, out var go))
            {
                Destroy(go);
                remotePlayers.Remove(playerId);
                Debug.Log($"[Game] 远端玩家离开: {playerId}");
            }
        }

        // ===== UI事件 =====
        private void OnCharacterConfirmed(int charIndex, int mapIndex)
        {
            selectedCharIndex = charIndex;
            selectedMapIndex = mapIndex;
            Debug.Log($"[Game] 确认角色:{charIndex} 地图:{mapIndex}");

            // 连接服务器
            if (!networkClient.IsConnected)
            {
                SetState(GameState.Connecting);
                networkClient.Connect(ServerUrl);
            }
            else
            {
                // 发送准备
                networkClient.SetReady(true, charIndex, 0);
                // 如果是训练场，直接开始
                if (selectedMode == "training")
                {
                    StartLocalGame();
                }
            }
        }

        private void OnChatSent(string message)
        {
            networkClient?.SendChat(message);
            chatUI.AddMessage("你", message);
        }

        // ===== 游戏流程 =====
        public void StartGame(string mode)
        {
            selectedMode = mode;
            SetState(GameState.CharacterSelect);
        }

        private void StartLocalGame()
        {
            CleanupGame();

            // 确定地图
            MapManager.MapType mapType = GetMapType(selectedMapIndex);

            // 加载地图
            mapManager.LoadMap(mapType, selectedMode);

            // 创建本地玩家
            localPlayer = CreateLocalPlayer(selectedCharIndex);
            localPlayer.transform.position = mapManager.GetSpawnPoint(0);

            // 设置游戏模式
            switch (selectedMode)
            {
                case "1v1":
                    currentMode = gameObject.AddComponent<DuelMode>();
                    break;
                case "5v5":
                    currentMode = gameObject.AddComponent<TeamBattleMode>();
                    break;
                case "training":
                    currentMode = gameObject.AddComponent<TrainingMode>();
                    break;
                case "challenge":
                    currentMode = gameObject.AddComponent<ChallengeMode>();
                    break;
            }

            var players = new List<PlayerController> { localPlayerController };
            currentMode.Initialize(players);
            currentMode.StartMode();

            // 初始化HUD
            hudManager.Initialize(localHealth, localSkills, localWeapons, localCombat);

            // 正确设置体力引用：体力系统在HealthSystem里
            float stamina = localHealth != null ? localHealth.StaminaPercent : 1f;

            SetState(GameState.Playing);
        }

        private GameObject CreateLocalPlayer(int charIndex)
        {
            var go = CreatePlayerObject(0, "LocalPlayer");
            localPlayerController = go.GetComponent<PlayerController>();
            localPlayerController.IsLocalPlayer = true;
            localHealth = go.GetComponent<HealthSystem>();
            localCombat = go.GetComponent<CombatSystem>();
            localWeapons = go.GetComponent<WeaponSystem>();
            localSkills = go.GetComponent<SkillSystem>();

            // 设置角色
            var charData = CharacterData.GetCharacter(charIndex);
            localHealth.MaxHp = charData.baseHp;
            localHealth.CurrentHp = charData.baseHp;

            return go;
        }

        private GameObject CreatePlayerObject(int id, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"Player_{name}_{id}";
            go.tag = "Player";

            // 核心组件
            go.AddComponent<PlayerController>();
            var hp = go.AddComponent<HealthSystem>();
            var combat = go.AddComponent<CombatSystem>();
            var weapons = go.AddComponent<WeaponSystem>();
            var skills = go.AddComponent<SkillSystem>();

            // 刚体
            var rb = go.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            return go;
        }

        private MapManager.MapType GetMapType(int index)
        {
            if (selectedMode == "5v5")
            {
                return index switch
                {
                    0 => MapManager.MapType.NorthSeaIce,
                    1 => MapManager.MapType.ChangAnCity,
                    2 => MapManager.MapType.PersianGarden,
                    _ => MapManager.MapType.NorthSeaIce
                };
            }
            return index switch
            {
                0 => MapManager.MapType.DragonArena,
                1 => MapManager.MapType.RomanColosseum,
                2 => MapManager.MapType.BambooForest,
                _ => MapManager.MapType.DragonArena
            };
        }

        // ===== 更新循环 =====
        void Update()
        {
            if (CurrentState != GameState.Playing) return;

            // ESC暂停/菜单
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (shopUI != null && IsShopOpen()) { shopUI.Hide(); return; }
                SetState(CurrentState == GameState.Paused ? GameState.Playing : GameState.Paused);
            }

            // Tab排位面板
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                rankUI.Toggle();
            }

            // B商城
            if (Input.GetKeyDown(KeyCode.B))
            {
                shopUI.Toggle();
            }

            // Enter聊天
            if (Input.GetKeyDown(KeyCode.Return))
            {
                chatUI?.Toggle();
            }

            // 发送输入到服务器
            if (localPlayerController != null && networkClient != null && networkClient.IsConnected)
            {
                var keys = new Dictionary<string, bool>
                {
                    ["w"] = Input.GetKey(KeyCode.W),
                    ["a"] = Input.GetKey(KeyCode.A),
                    ["s"] = Input.GetKey(KeyCode.S),
                    ["d"] = Input.GetKey(KeyCode.D),
                    ["shift"] = Input.GetKey(KeyCode.LeftShift),
                    ["space"] = Input.GetKey(KeyCode.Space),
                    ["q"] = Input.GetKeyDown(KeyCode.Q),
                    ["e"] = Input.GetKeyDown(KeyCode.E),
                    ["r"] = Input.GetKeyDown(KeyCode.R),
                    ["light"] = Input.GetMouseButtonDown(0),
                    ["heavy"] = Input.GetMouseButtonDown(1),
                };
                var mouse = new Dictionary<string, float>
                {
                    ["x"] = Input.GetAxis("Mouse X"),
                    ["y"] = Input.GetAxis("Mouse Y"),
                };
                networkClient.SendInput(keys, mouse);
            }

            // 区域外伤害检测
            if (zoneController != null && localHealth != null && !localHealth.IsDead)
            {
                if (!zoneController.IsInSafeZone(localPlayer.transform.position))
                {
                    float dmg = zoneController.GetOutsideDamage(Time.deltaTime);
                    localHealth.TakeDamage(dmg * Time.deltaTime, null);
                }
            }
        }

        private bool IsShopOpen()
        {
            return false; // ShopUI内部管理
        }

        // ===== 工具方法 =====
        private void SetCursorLock(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void ReturnToMenu()
        {
            CleanupGame();
            SetState(GameState.MainMenu);
        }

        private void CleanupGame()
        {
            if (localPlayer != null) { Destroy(localPlayer); localPlayer = null; }
            foreach (var rp in remotePlayers.Values) if (rp != null) Destroy(rp);
            remotePlayers.Clear();
            if (currentMode != null) { Destroy(currentMode); currentMode = null; }
            mapManager?.ClearMap();
            localPlayerController = null;
            localHealth = null;
            localCombat = null;
            localWeapons = null;
            localSkills = null;
        }

        // ===== 公开接口 =====
        public PlayerController GetLocalPlayer() => localPlayerController;
        public HealthSystem GetLocalHealth() => localHealth;
        public bool IsPlaying() => CurrentState == GameState.Playing;
        public string GetSelectedMode() => selectedMode;
    }
}
