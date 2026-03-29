using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace QBlockyFighter.Network
{
    public class FrameSyncManager : MonoBehaviour
    {
        public static FrameSyncManager Instance { get; private set; }

        [Header("Frame Sync")]
        [SerializeField] private int tickRate = 20;
        public int TickRate => tickRate;
        public float FrameInterval => 1f / tickRate;

        public int CurrentFrame { get; private set; }
        public bool IsRunning { get; private set; }

        // Input
        private JObject _localInput;
        private readonly Dictionary<int, Dictionary<string, JObject>> _frameInputs = new();

        // Prediction & Reconciliation
        private readonly List<JObject> _pendingInputs = new();
        private int _lastConfirmedFrame = -1;

        // Events
        public event Action<int, Dictionary<string, JObject>> OnFrameReceived;
        public event Action<int> OnFrameProcessed;

        private GameClient _client;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _client = GameClient.Instance;
            if (_client != null)
            {
                _client.OnFrameRaw += OnNetworkFrame;
                _client.OnGameStartRaw += OnGameStart;
            }
        }

        public void StartSync(int startFrame = 0)
        {
            CurrentFrame = startFrame;
            IsRunning = true;
            _frameInputs.Clear();
            _pendingInputs.Clear();
            Debug.Log($"[帧同步] 开始 (帧率: {tickRate} fps)");
        }

        public void StopSync()
        {
            IsRunning = false;
            Debug.Log("[帧同步] 停止");
        }

        /// <summary>
        /// Collect local player input to send to server this frame.
        /// </summary>
        public void SetLocalInput(JObject input)
        {
            _localInput = input;
        }

        /// <summary>
        /// Send current frame input to server.
        /// </summary>
        public void SendInput()
        {
            if (_client == null || !_client.IsConnected) return;

            _client.SendInput(_localInput ?? new JObject(), new JObject());

            // Save for client prediction
            _pendingInputs.Add(new JObject
            {
                ["frame"] = CurrentFrame,
                ["input"] = _localInput ?? new JObject()
            });

            // Limit pending buffer
            while (_pendingInputs.Count > 60) _pendingInputs.RemoveAt(0);
        }

        /// <summary>
        /// Advance local simulation by one frame (for client prediction).
        /// </summary>
        public void AdvanceLocalFrame()
        {
            if (!IsRunning) return;
            CurrentFrame++;
        }

        private void OnNetworkFrame(JObject msg)
        {
            var frameData = msg["frame"] as JObject;
            if (frameData == null) return;

            int frame = frameData.Value<int>("frame");
            var inputs = frameData["inputs"] as JObject;
            if (inputs == null) return;

            var parsedInputs = new Dictionary<string, JObject>();
            foreach (var prop in inputs.Properties())
            {
                parsedInputs[prop.Name] = prop.Value as JObject ?? new JObject();
            }

            _frameInputs[frame] = parsedInputs;

            // Limit stored frames
            if (_frameInputs.Count > 300)
            {
                int minFrame = frame - 300;
                var toRemove = new List<int>();
                foreach (var key in _frameInputs.Keys)
                {
                    if (key < minFrame) toRemove.Add(key);
                }
                foreach (var key in toRemove) _frameInputs.Remove(key);
            }

            // Reconciliation: remove confirmed pending inputs
            if (_lastConfirmedFrame < frame)
            {
                _lastConfirmedFrame = frame;
                _pendingInputs.RemoveAll(p => p.Value<int>("frame") <= frame);
            }

            OnFrameReceived?.Invoke(frame, parsedInputs);
            OnFrameProcessed?.Invoke(frame);
        }

        private void OnGameStart(JObject msg)
        {
            tickRate = _client != null ? _client.TickRate : 20;
            StartSync(0);
        }

        /// <summary>
        /// Get inputs for a specific frame from server.
        /// </summary>
        public Dictionary<string, JObject> GetFrameInputs(int frame)
        {
            return _frameInputs.TryGetValue(frame, out var inputs) ? inputs : null;
        }

        /// <summary>
        /// Get the local player's key for frame input lookup.
        /// </summary>
        public string GetLocalPlayerKey()
        {
            return _client != null ? _client.PlayerId.ToString() : "0";
        }

        /// <summary>
        /// Get all pending inputs for client-side prediction reconciliation.
        /// </summary>
        public List<JObject> GetPendingInputs() => new(_pendingInputs);

        private void OnDestroy()
        {
            if (_client != null)
            {
                _client.OnFrame -= OnNetworkFrame;
                _client.OnGameStart -= OnGameStart;
            }
        }
    }
}
