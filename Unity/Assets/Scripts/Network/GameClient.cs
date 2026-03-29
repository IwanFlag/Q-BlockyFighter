using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using WebSocketSharp;

namespace QBlockyFighter.Network
{
    public class GameClient : MonoBehaviour
    {
        public static GameClient Instance { get; private set; }

        [Header("Connection")]
        [SerializeField] private string serverUrl = "ws://localhost:3000";
        [SerializeField] private int maxReconnectAttempts = 5;
        [SerializeField] private float reconnectDelay = 2f;

        private WebSocket _ws;
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

        // Events
        public event Action OnConnected;
        public event Action<int, string> OnDisconnected;
        public event Action<JObject> OnWelcome;
        public event Action<JObject> OnRoomCreated;
        public event Action<JObject> OnRoomJoined;
        public event Action OnRoomLeft;
        public event Action<JObject> OnPlayerJoined;
        public event Action<JObject> OnPlayerLeft;
        public event Action<JObject> OnGameStart;
        public event Action<JObject> OnFrame;
        public event Action<JObject> OnMatched;
        public event Action<JObject> OnChat;
        public event Action<JObject> OnSpectate;
        public event Action<string> OnError;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Connect(string url = null)
        {
            if (!string.IsNullOrEmpty(url)) serverUrl = url;

            try
            {
                _ws = new WebSocket(serverUrl);
                _ws.OnOpen += (s, e) =>
                {
                    _connected = true;
                    _reconnectAttempts = 0;
                    Debug.Log("[网络] 已连接到服务器");
                    UnityMainThreadDispatcher.Instance().Enqueue(() => OnConnected?.Invoke());
                };

                _ws.OnMessage += (s, e) =>
                {
                    try
                    {
                        var msg = JObject.Parse(e.Data);
                        UnityMainThreadDispatcher.Instance().Enqueue(() => HandleMessage(msg));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[网络] 消息解析错误: {ex.Message}");
                    }
                };

                _ws.OnClose += (s, e) =>
                {
                    _connected = false;
                    Debug.Log($"[网络] 连接已关闭: {e.Code} {e.Reason}");
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        OnDisconnected?.Invoke(e.Code, e.Reason);
                        TryReconnect();
                    });
                };

                _ws.OnError += (s, e) =>
                {
                    Debug.LogError($"[网络] 连接错误: {e.Message}");
                    UnityMainThreadDispatcher.Instance().Enqueue(() => OnError?.Invoke(e.Message));
                };

                _ws.ConnectAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[网络] 连接异常: {ex.Message}");
                OnError?.Invoke(ex.Message);
            }
        }

        public void Disconnect()
        {
            _reconnectAttempts = maxReconnectAttempts;
            _ws?.Close(CloseStatusCode.Normal, "客户端主动断开");
            _ws = null;
            _connected = false;
            RoomId = null;
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

        public void Send(string type, object data = null)
        {
            if (!_connected || _ws == null)
            {
                Debug.LogWarning($"[网络] 未连接，无法发送: {type}");
                return;
            }

            var msg = data != null ? JObject.FromObject(data) : new JObject();
            msg["type"] = type;
            _ws.Send(msg.ToString(Formatting.None));
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
                    OnPlayerJoined?.Invoke(msg);
                    break;

                case "player_left":
                    OnPlayerLeft?.Invoke(msg);
                    break;

                case "game_start":
                    Debug.Log("[网络] 游戏开始！");
                    OnGameStart?.Invoke(msg);
                    break;

                case "frame":
                    HandleFrame(msg);
                    OnFrame?.Invoke(msg);
                    break;

                case "matched":
                    RoomId = msg["room"]?.Value<int>("id");
                    Debug.Log($"[网络] 匹配成功！");
                    OnMatched?.Invoke(msg);
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

        private void HandleFrame(JObject msg)
        {
            var frameData = msg["frame"] as JObject;
            if (frameData == null) return;

            _frameBuffer.Enqueue(frameData);
            while (_frameBuffer.Count > MAX_FRAME_BUFFER) _frameBuffer.Dequeue();

            long timestamp = frameData.Value<long?>("timestamp") ?? 0;
            if (timestamp > 0)
            {
                Latency = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timestamp);
            }
            LastFrameTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        // Convenience methods
        public void SendJoin(string name, string character) =>
            Send("join", new { name, character });

        public void SendCreateRoom(string mode = "1v1") =>
            Send("create_room", new { mode });

        public void SendJoinRoom(int roomId) =>
            Send("join_room", new { roomId });

        public void SendLeaveRoom() => Send("leave_room");

        public void SendReady(bool ready, string character, int weaponIndex = 0) =>
            Send("ready", new { ready, character, weaponIndex });

        public void SendInput(object keys, object mouse) =>
            Send("input", new { keys, mouse });

        public void SendChat(string message) =>
            Send("chat", new { message });

        public void SendSpectate(int roomId) =>
            Send("spectate", new { roomId });

        public void SendMatchmake() => Send("matchmake");

        public void SendTraining() => Send("training");

        private void OnDestroy()
        {
            Disconnect();
        }
    }

    /// <summary>
    /// Dispatches actions to Unity main thread. Attach to a persistent GameObject.
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
