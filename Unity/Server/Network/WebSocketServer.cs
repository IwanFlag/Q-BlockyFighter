using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fleck;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QBlockyFighter.Server.Protocol;

namespace QBlockyFighter.Server.Network
{
    public class WebSocketServerManager
    {
        private readonly int _port;
        private readonly Matchmaker _matchmaker;
        private readonly GameLoop _gameLoop;
        private Fleck.WebSocketServer? _server;
        private readonly Dictionary<IWebSocketConnection, Player> _players = new();
        private int _nextPlayerId = 1;
        private int _nextRoomId = 1;
        private bool _running;

        public WebSocketServerManager(int port, Matchmaker matchmaker, GameLoop gameLoop)
        {
            _port = port;
            _matchmaker = matchmaker;
            _gameLoop = gameLoop;
        }

        public void Start()
        {
            _running = true;
            _server = new Fleck.WebSocketServer($"ws://0.0.0.0:{_port}");

            _server.Start(socket =>
            {
                socket.OnOpen = () => OnConnection(socket);
                socket.OnMessage = msg => OnMessage(socket, msg);
                socket.OnClose = () => OnDisconnect(socket);
                socket.OnError = ex => Console.Error.WriteLine($"[错误] WebSocket: {ex.Message}");
            });

            Console.WriteLine($"[服务器] 已启动，监听端口 {_port}");

            // Start matchmaking loop
            Task.Run(() =>
            {
                while (_running)
                {
                    _matchmaker.ProcessQueue();
                    Thread.Sleep(GameConfig.MATCHMAKING_INTERVAL_MS);
                }
            });

            // Start room cleanup loop
            Task.Run(() =>
            {
                while (_running)
                {
                    CleanupRooms();
                    Thread.Sleep(GameConfig.ROOM_CLEANUP_INTERVAL_MS);
                }
            });

            _gameLoop.Start();
        }

        public void Stop()
        {
            _running = false;
            foreach (var player in _players.Values)
            {
                player.Send(MsgType.S_ERROR, new { message = "服务器正在关闭" });
                player.Connection.Close();
            }
            _gameLoop.Stop();
            _server?.Dispose();
        }

        private void OnConnection(IWebSocketConnection socket)
        {
            var playerId = Interlocked.Increment(ref _nextPlayerId);
            var player = new Player(socket, playerId);
            _players[socket] = player;

            Console.WriteLine($"[连接] 玩家 {playerId} 已连接 (在线: {_players.Count})");

            player.Send(MsgType.S_WELCOME, new
            {
                playerId,
                playerName = player.Name,
                serverTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                tickRate = GameConfig.TICK_RATE
            });
        }

        private void OnMessage(IWebSocketConnection socket, string raw)
        {
            if (!_players.TryGetValue(socket, out var player)) return;

            try
            {
                var msg = JObject.Parse(raw);
                player.LastActivity = DateTime.UtcNow;
                HandleMessage(player, msg);
            }
            catch (Exception ex)
            {
                player.Send(MsgType.S_ERROR, new { message = "消息格式错误" });
                Console.Error.WriteLine($"[错误] 消息处理: {ex.Message}");
            }
        }

        private void OnDisconnect(IWebSocketConnection socket)
        {
            if (!_players.TryGetValue(socket, out var player)) return;

            HandleDisconnect(player);
            _players.Remove(socket);
            Console.WriteLine($"[断开] 玩家 {player.Id} 已断开 (在线: {_players.Count})");
        }

        private void HandleMessage(Player player, JObject msg)
        {
            string type = msg.Value<string>("type") ?? "";

            switch (type)
            {
                case MsgType.C_JOIN:
                    if (msg["name"] != null) player.Name = msg.Value<string>("name")!;
                    if (msg["character"] != null) player.Character = msg.Value<string>("character")!;
                    player.Send(MsgType.S_WELCOME, new
                    {
                        playerId = player.Id,
                        playerName = player.Name,
                        serverTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        tickRate = GameConfig.TICK_RATE
                    });
                    break;

                case MsgType.C_CREATE_ROOM:
                    HandleCreateRoom(player, msg);
                    break;

                case MsgType.C_JOIN_ROOM:
                    HandleJoinRoom(player, msg);
                    break;

                case MsgType.C_LEAVE_ROOM:
                    HandleLeaveRoom(player);
                    break;

                case MsgType.C_READY:
                    HandleReady(player, msg);
                    break;

                case MsgType.C_INPUT:
                    HandleInput(player, msg);
                    break;

                case MsgType.C_CHAT:
                    HandleChat(player, msg);
                    break;

                case MsgType.C_SPECTATE:
                    HandleSpectate(player, msg);
                    break;

                case MsgType.C_MATCHMAKE:
                    _matchmaker.Enqueue(player);
                    break;

                case MsgType.C_TRAINING:
                    HandleTraining(player);
                    break;

                default:
                    player.Send(MsgType.S_ERROR, new { message = $"未知消息类型: {type}" });
                    break;
            }
        }

        private void HandleCreateRoom(Player player, JObject msg)
        {
            if (player.RoomId != null)
            {
                player.Send(MsgType.S_ERROR, new { message = "你已在房间中，请先离开" });
                return;
            }

            var roomId = Interlocked.Increment(ref _nextRoomId);
            string mode = msg.Value<string>("mode") ?? "1v1";
            var room = new Room(roomId, player, mode);
            _matchmaker.AddRoom(roomId, room);

            player.Send(MsgType.S_ROOM_CREATED, new { room = room.ToJson() });
            Console.WriteLine($"[房间] 玩家 {player.Name} 创建房间 {roomId} ({mode})");
        }

        private void HandleJoinRoom(Player player, JObject msg)
        {
            if (player.RoomId != null)
            {
                player.Send(MsgType.S_ERROR, new { message = "你已在房间中，请先离开" });
                return;
            }

            int roomId = msg.Value<int>("roomId");
            var room = _matchmaker.GetRoom(roomId);
            if (room == null)
            {
                player.Send(MsgType.S_ERROR, new { message = "房间不存在" });
                return;
            }

            if (room.State != "waiting")
            {
                player.Send(MsgType.S_ERROR, new { message = "游戏已开始" });
                return;
            }

            if (!room.AddPlayer(player))
            {
                player.Send(MsgType.S_ERROR, new { message = "房间已满" });
                return;
            }

            player.Send(MsgType.S_ROOM_JOINED, new { room = room.ToJson() });
            room.Broadcast(MsgType.S_PLAYER_JOINED, new { player = player.ToJson() });
            Console.WriteLine($"[房间] 玩家 {player.Name} 加入房间 {room.Id}");
        }

        private void HandleLeaveRoom(Player player)
        {
            if (player.RoomId == null) return;
            var room = _matchmaker.GetRoom(player.RoomId.Value);
            if (room == null) return;

            room.RemovePlayer(player.Id);
            room.Broadcast(MsgType.S_PLAYER_LEFT, new { playerId = player.Id, playerName = player.Name });
            player.Send(MsgType.S_ROOM_LEFT, new { roomId = room.Id });

            if (room.IsEmpty)
            {
                _matchmaker.RemoveRoom(room.Id);
                Console.WriteLine($"[房间] 房间 {room.Id} 已删除（空房间）");
            }
        }

        private void HandleReady(Player player, JObject msg)
        {
            if (player.RoomId == null)
            {
                player.Send(MsgType.S_ERROR, new { message = "你不在房间中" });
                return;
            }

            var room = _matchmaker.GetRoom(player.RoomId.Value);
            if (room == null) return;

            player.IsReady = msg.Value<bool?>("ready") ?? true;
            if (msg["character"] != null) player.Character = msg.Value<string>("character")!;
            if (msg["weaponIndex"] != null) player.WeaponIndex = msg.Value<int>("weaponIndex");

            room.Broadcast(MsgType.S_PLAYER_JOINED, new { player = player.ToJson() });

            if (room.CanStart())
            {
                room.StartGame();
                Console.WriteLine($"[开始] 房间 {room.Id} 游戏开始！");
            }
        }

        private void HandleInput(Player player, JObject msg)
        {
            if (player.RoomId == null) return;
            var room = _matchmaker.GetRoom(player.RoomId.Value);
            if (room == null || room.State != "playing") return;

            player.LastInput = new PlayerInput
            {
                Keys = msg["keys"] as JObject,
                Mouse = msg["mouse"] as JObject,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        private void HandleChat(Player player, JObject msg)
        {
            if (player.RoomId == null) return;
            var room = _matchmaker.GetRoom(player.RoomId.Value);
            if (room == null) return;

            room.Broadcast(MsgType.S_CHAT, new
            {
                playerId = player.Id,
                playerName = player.Name,
                message = msg.Value<string>("message"),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }

        private void HandleSpectate(Player player, JObject msg)
        {
            int roomId = msg.Value<int>("roomId");
            var room = _matchmaker.GetRoom(roomId);
            if (room == null)
            {
                player.Send(MsgType.S_ERROR, new { message = "房间不存在" });
                return;
            }

            if (!room.AddSpectator(player))
            {
                player.Send(MsgType.S_ERROR, new { message = "观战位已满" });
                return;
            }

            player.Send(MsgType.S_SPECTATE, new
            {
                room = room.ToJson(),
                frameHistory = room.GetRecentFrames(100)
            });
            Console.WriteLine($"[观战] 玩家 {player.Name} 观战房间 {room.Id}");
        }

        private void HandleTraining(Player player)
        {
            if (player.RoomId != null) HandleLeaveRoom(player);

            var roomId = Interlocked.Increment(ref _nextRoomId);
            var room = new Room(roomId, player, "training");
            _matchmaker.AddRoom(roomId, room);

            player.IsReady = true;
            room.StartGame();

            player.Send(MsgType.S_ROOM_CREATED, new { room = room.ToJson(), message = "已进入训练场" });
            Console.WriteLine($"[训练] 玩家 {player.Name} 进入训练场 {roomId}");
        }

        private void HandleDisconnect(Player player)
        {
            _matchmaker.RemoveFromQueue(player);

            if (player.RoomId != null)
            {
                var room = _matchmaker.GetRoom(player.RoomId.Value);
                if (room != null)
                {
                    room.RemovePlayer(player.Id);
                    room.Broadcast(MsgType.S_PLAYER_LEFT, new
                    {
                        playerId = player.Id,
                        playerName = player.Name,
                        reason = "断开连接"
                    });

                    if (room.IsEmpty)
                    {
                        _matchmaker.RemoveRoom(room.Id);
                    }
                }
            }
        }

        private void CleanupRooms()
        {
            var toRemove = new List<int>();
            foreach (var kvp in _matchmaker.GetAllRooms())
            {
                if (kvp.Value.State == "waiting" &&
                    (DateTime.UtcNow - kvp.Value.CreatedAt).TotalMilliseconds > GameConfig.ROOM_IDLE_TIMEOUT_MS)
                {
                    foreach (var p in kvp.Value.Players.Values)
                    {
                        p.Send(MsgType.S_ERROR, new { message = "房间超时已关闭" });
                        p.RoomId = null;
                    }
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var id in toRemove)
            {
                _matchmaker.RemoveRoom(id);
                Console.WriteLine($"[清理] 房间 {id} 超时已关闭");
            }
        }
    }
}
