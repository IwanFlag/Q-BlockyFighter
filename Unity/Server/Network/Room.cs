using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using QBlockyFighter.Server.Protocol;

namespace QBlockyFighter.Server.Network
{
    public class Room
    {
        public int Id { get; }
        public int HostId { get; }
        public string Mode { get; }
        public string State { get; private set; }
        public ConcurrentDictionary<int, Player> Players { get; } = new();
        public ConcurrentDictionary<int, Player> Spectators { get; } = new();
        public int FrameNumber { get; private set; }
        public List<object> FrameInputs { get; } = new();
        public DateTime CreatedAt { get; }
        public long? GameStartTime { get; private set; }
        public bool IsEmpty => Players.IsEmpty && Spectators.IsEmpty;

        public Room(int id, Player host, string mode = "1v1")
        {
            Id = id;
            HostId = host.Id;
            Mode = mode;
            State = "waiting";
            CreatedAt = DateTime.UtcNow;
            AddPlayer(host);
        }

        public bool AddPlayer(Player player)
        {
            int maxPlayers = Mode switch
            {
                "1v1" => GameConfig.MAX_PLAYERS_PER_ROOM,
                "5v5" => 10,
                _ => GameConfig.MAX_PLAYERS_PER_ROOM
            };
            if (Players.Count >= maxPlayers)
                return false;

            Players[player.Id] = player;
            player.RoomId = Id;
            player.IsSpectator = false;
            return true;
        }

        public bool AddSpectator(Player player)
        {
            if (Spectators.Count >= GameConfig.MAX_SPECTATORS)
                return false;

            Spectators[player.Id] = player;
            player.RoomId = Id;
            player.IsSpectator = true;
            return true;
        }

        public bool RemovePlayer(int playerId)
        {
            bool removed = Players.TryRemove(playerId, out var player) ||
                           Spectators.TryRemove(playerId, out player);
            if (player != null)
            {
                player.RoomId = null;
                player.IsReady = false;
                player.IsSpectator = false;
            }
            return removed;
        }

        public bool CanStart()
        {
            if (Mode == "training") return Players.Count >= 1;
            if (Mode == "1v1") return Players.Count == 2 && Players.Values.All(p => p.IsReady);
            if (Mode == "5v5") return Players.Count >= 2 && Players.Values.All(p => p.IsReady);
            if (Mode == "challenge") return Players.Count >= 1;
            return false;
        }

        public void StartGame()
        {
            State = "playing";
            FrameNumber = 0;
            FrameInputs.Clear();
            GameStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var playersData = Players.Values.Select(p => p.ToJson()).ToArray();
            Broadcast(MsgType.S_GAME_START, new
            {
                roomId = Id,
                players = playersData,
                mode = Mode,
                startTime = GameStartTime
            });
        }

        public object CollectFrameInput()
        {
            var inputs = new Dictionary<string, object>();
            foreach (var kvp in Players)
            {
                inputs[kvp.Key.ToString()] = kvp.Value.LastInput != null
                    ? new
                    {
                        keys = kvp.Value.LastInput.Keys,
                        mouse = kvp.Value.LastInput.Mouse,
                        timestamp = kvp.Value.LastInput.Timestamp
                    }
                    : (object)new { idle = true };
            }

            var frameData = new
            {
                frame = FrameNumber,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                inputs
            };

            FrameInputs.Add(frameData);
            return frameData;
        }

        public void BroadcastFrame(object frameData)
        {
            Broadcast(MsgType.S_FRAME, new { frame = frameData });
        }

        public void AdvanceFrame()
        {
            FrameNumber++;
        }

        public void Broadcast(string type, object data)
        {
            var msg = JObject.FromObject(data);
            msg["type"] = type;
            string json = msg.ToString(Newtonsoft.Json.Formatting.None);

            foreach (var player in Players.Values)
            {
                try
                {
                    if (player.Connection.IsAvailable)
                        player.Connection.Send(json);
                }
                catch { }
            }

            foreach (var spectator in Spectators.Values)
            {
                try
                {
                    if (spectator.Connection.IsAvailable)
                        spectator.Connection.Send(json);
                }
                catch { }
            }
        }

        public List<object> GetRecentFrames(int count)
        {
            int start = Math.Max(0, FrameInputs.Count - count);
            return FrameInputs.GetRange(start, FrameInputs.Count - start);
        }

        public object ToJson()
        {
            return new
            {
                id = Id,
                host = HostId,
                mode = Mode,
                state = State,
                players = Players.Values.Select(p => p.ToJson()).ToArray(),
                spectatorCount = Spectators.Count,
                frameNumber = FrameNumber
            };
        }
    }
}
