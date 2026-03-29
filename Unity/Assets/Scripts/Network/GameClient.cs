using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace QBlockyFighter.Network
{
    public class GameClient : MonoBehaviour
    {
        public static GameClient Instance { get; private set; }

        [Header("Connection")]
        [SerializeField] private string serverUrl = "ws://localhost:3000";
        [SerializeField] private int maxReconnectAttempts = 5;
        [SerializeField] private float reconnectDelay = 2f;

        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;
        private bool _connected;
        private int _reconnectAttempts;

        public bool IsConnected => _connected;
        public int PlayerId { get; private set; }
        public string PlayerName { get; private set; }
        public int? RoomId { get; private set; }
        public bool IsSpectator { get; private set; }
        public int TickRate { get; private set; } = 20;
        public float Latency { get; private set; }
        public long LastFrameTime { get; private set; }

        // Frame buffer
        private readonly Queue<JObject> _frameBuffer = new();
        public Queue<JObject> FrameBuffer => _frameBuffer;
        private const int MAX_FRAME_BUFFER = 60;

        // Events - 与GameManager兼容
        public event Action OnConnected;
        public event Action<int, string> OnDisconnected;
        public event Action<JObject> OnWelcome;
        public event Action<JObject> OnRoomCreated;
        public event Action<JObject> OnRoomJoined;
        public event Action OnRoomLeft;
        public event Action<int, string> OnPlayerJoined;   // playerId, playerName
        public event Action<int> OnPlayerLeft;              // playerId
        public event Action OnGameStart;
        public event Action<int, JObject> OnFrameReceived;  // frame, inputs
        public event Action OnMatched;
        public event Action<JObject> OnChat;
        public event Action<JObject> OnSpectate;
        public event Action<string> OnError;

        // 保持旧签名兼容
        public event Action<JObject> OnGameStartRaw;
        public event Action<JObject> OnFrameRaw;
        public event Action<JObject> OnMatchedRaw;
        public event Action<JObject> OnPlayerJoinedRaw;
        public event Action<JObject> OnPlayerLeftRaw;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async void Connect(string url = null)
        {
            if (!string.IsNullOrEmpty(url)) serverUrl = url;

            try
            {
                _ws = new ClientWebSocket();
                _cts = new CancellationTokenSource();
                await _ws.ConnectAsync(new Uri(serverUrl), _cts.Token);
                _connected = true;
                _reconnectAttempts = 0;
                Debug.Log("[网络] 已连接到服务器");
                OnConnected?.Invoke();

                // 开始接收消息循环
                _ = ReceiveLoop();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[网络] 连接异常: {ex.Message}");
                OnError?.Invoke(ex.Message);
                TryReconnect();
            }
        }

        public async void Disconnect()
        {
            _reconnectAttempts = maxReconnectAttempts;
            _connected = false;
            try
            {
                if (_ws != null && _ws.State == WebSocketState.Open)
                {
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "客户端主动断开", CancellationToken.None);
                }
            }
            catch { }
            _ws?.Dispose();
            _ws = null;
            _cts?.Cancel();
            RoomId = null;
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[8192];
            var messageBuffer = new StringBuilder();

            try
            {
                while (_connected && _ws.State == WebSocketState.Open)
                {
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _connected = false;
                        Debug.Log($"[网络] 连接已关闭");
                        OnDisconnected?.Invoke((int)result.CloseStatus, result.CloseStatusDescription ?? "");
                        TryReconnect();
                        break;
                    }

                    messageBuffer.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                    if (result.EndOfMessage)
                    {
                        string fullMessage = messageBuffer.ToString();
                        messageBuffer.Clear();
                        try
                        {
                            var msg = JObject.Parse(fullMessage);
                            UnityMainThreadDispatcher.Instance().Enqueue(() => HandleMessage(msg));
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[网络] 消息解析错误: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (_connected)
                {
                    Debug.LogError($"[网络] 接收异常: {ex.Message}");
                    _connected = false;
                    TryReconnect();
                }
            }
        }

        private void TryReconnect()
        {
            if (_reconnectAttempts >= maxReconnectAttempts) return;
            _reconnectAttempts++;
            Debug.Log($"[网络] 尝试重连 ({_reconnectAttempts}/{maxReconnectAttempts})...");
            Task.Delay((int)(reconnectDelay * 1000)).ContinueWith(_ =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => Connect());
            });
        }

        public async void Send(string type, object data = null)
        {
            if (!_connected || _ws == null || _ws.State != WebSocketState.Open)
            {
                Debug.LogWarning($"[网络] 未连接，无法发送: {type}");
                return;
            }

            var msg = data != null ? JObject.FromObject(data) : new JObject();
            msg["type"] = type;
            var bytes = Encoding.UTF8.GetBytes(msg.ToString(Formatting.None));
            try
            {
                await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[网络] 发送失败: {ex.Message}");
            }
        }

        private void HandleMessage(JObject msg)
        {
            string type = msg.Value<string>("type") ?? "";

            switch (type)
            {
                case "welcome":
                    PlayerId = msg.Value<int>("playerId");
                    PlayerName = msg.Value<string>("playerName") ?? $"玩家{PlayerId}";
                    TickRate = msg.Value<int?>("tickRate") ?? 20;
                    Debug.Log($"[网络] 欢迎 {PlayerName} (ID: {PlayerId})");
                    OnWelcome?.Invoke(msg);
                    break;

                case "room_created":
                    RoomId = msg["room"]?.Value<int>("id");
                    Debug.Log($"[网络] 房间已创建: {RoomId}");
                    OnRoomCreated?.Invoke(msg);
                    break;

                case "room_joined":
                    RoomId = msg["room"]?.Value<int>("id");
                    Debug.Log($"[网络] 已加入房间: {RoomId}");
                    OnRoomJoined?.Invoke(msg);
                    break;

                case "room_left":
                    RoomId = null;
                    IsSpectator = false;
                    Debug.Log("[网络] 已离开房间");
                    OnRoomLeft?.Invoke();
                    break;

                case "player_joined":
                    var joinedPlayer = msg["player"] as JObject;
                    int jid = joinedPlayer?.Value<int>("id") ?? 0;
                    string jname = joinedPlayer?.Value<string>("name") ?? $"玩家{jid}";
                    OnPlayerJoined?.Invoke(jid, jname);
                    OnPlayerJoinedRaw?.Invoke(msg);
                    break;

                case "player_left":
                    int leftId = msg.Value<int>("playerId");
                    OnPlayerLeft?.Invoke(leftId);
                    OnPlayerLeftRaw?.Invoke(msg);
                    break;

                case "game_start":
                    Debug.Log("[网络] 游戏开始！");
                    OnGameStart?.Invoke();
                    OnGameStartRaw?.Invoke(msg);
                    break;

                case "frame":
                    var frameData = msg["frame"] as JObject;
                    if (frameData != null)
                    {
                        HandleFrame(frameData);
                        int frameNum = frameData.Value<int>("frame");
                        var inputs = frameData["inputs"] as JObject;
                        OnFrameReceived?.Invoke(frameNum, inputs?.ToObject<Dictionary<int, JObject>>() as Dictionary<int, object>);
                    }
                    OnFrameRaw?.Invoke(msg);
                    break;

                case "matched":
                    RoomId = msg["room"]?.Value<int>("id");
                    Debug.Log("[网络] 匹配成功！");
                    OnMatched?.Invoke();
                    OnMatchedRaw?.Invoke(msg);
                    break;

                case "chat":
                    OnChat?.Invoke(msg);
                    break;

                case "spectate":
                    IsSpectator = true;
                    RoomId = msg["room"]?.Value<int>("id");
                    Debug.Log("[网络] 进入观战模式");
                    OnSpectate?.Invoke(msg);
                    break;

                case "error":
                    string errMsg = msg.Value<string>("message") ?? "未知错误";
                    Debug.LogError($"[网络] 服务器错误: {errMsg}");
                    OnError?.Invoke(errMsg);
                    break;

                default:
                    Debug.Log($"[网络] 未知消息: {type}");
                    break;
            }
        }

        private void HandleFrame(JObject frameData)
        {
            _frameBuffer.Enqueue(frameData);
            while (_frameBuffer.Count > MAX_FRAME_BUFFER) _frameBuffer.Dequeue();

            long timestamp = frameData.Value<long?>("timestamp") ?? 0;
            if (timestamp > 0)
            {
                Latency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timestamp;
            }
            LastFrameTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        // ===== 便捷方法（与GameManager兼容的命名） =====
        public void SetName(string name) => Send("join", new { name });
        public void SetCharacter(string character) => Send("join", new { character });
        public void CreateRoom(string mode = "1v1") => Send("create_room", new { mode });
        public void JoinRoom(int roomId) => Send("join_room", new { roomId });
        public void LeaveRoom() => Send("leave_room");
        public void SetReady(bool ready = true, int character = 0, int weaponIndex = 0) =>
            Send("ready", new { ready, character, weaponIndex });
        public void SendInput(Dictionary<string, bool> keys, Dictionary<string, float> mouse) =>
            Send("input", new { keys, mouse });
        public void SendChat(string message) => Send("chat", new { message });
        public void Spectate(int roomId) => Send("spectate", new { roomId });
        public void RequestMatchmake() => Send("matchmake");
        public void EnterTraining() => Send("training");

        // 原始签名兼容
        public void SendInput(object keys, object mouse) =>
            Send("input", new { keys, mouse });
        public void SendJoin(string name, string character) => Send("join", new { name, character });
        public void SendCreateRoom(string mode = "1v1") => Send("create_room", new { mode });
        public void SendJoinRoom(int roomId) => Send("join_room", new { roomId });
        public void SendLeaveRoom() => Send("leave_room");
        public void SendReady(bool ready, string character, int weaponIndex = 0) =>
            Send("ready", new { ready, character, weaponIndex });
        public void SendSpectate(int roomId) => Send("spectate", new { roomId });
        public void SendMatchmake() => Send("matchmake");
        public void SendTraining() => Send("training");

        public async Task<JObject> GetRoomList()
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                var httpUrl = serverUrl.Replace("ws://", "http://").Replace("wss://", "https://");
                var resp = await client.GetStringAsync($"{httpUrl}/api/rooms");
                return JObject.Parse(resp);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[网络] 获取房间列表失败: {ex.Message}");
                return new JObject { ["rooms"] = new JArray(), ["online"] = 0 };
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }
    }

    /// <summary>
    /// 调度异步回调到Unity主线程
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher _instance;
        private readonly Queue<Action> _queue = new();
        private readonly object _lock = new();

        public static UnityMainThreadDispatcher Instance()
        {
            if (_instance == null)
            {
                var go = new GameObject("MainThreadDispatcher");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
            }
            return _instance;
        }

        public void Enqueue(Action action)
        {
            lock (_lock) { _queue.Enqueue(action); }
        }

        private void Update()
        {
            lock (_lock)
            {
                while (_queue.Count > 0) _queue.Dequeue()?.Invoke();
            }
        }
    }
}
