using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using QBlockyFighter.Server.Protocol;

namespace QBlockyFighter.Server.Network
{
    public class Matchmaker
    {
        private readonly List<Player> _queue = new();
        private readonly ConcurrentDictionary<int, Room> _rooms = new();
        private readonly object _lock = new();

        public void Enqueue(Player player)
        {
            lock (_lock)
            {
                if (player.RoomId != null)
                {
                    player.Send(MsgType.S_ERROR, new { message = "你已在房间中" });
                    return;
                }

                if (_queue.Contains(player))
                {
                    player.Send(MsgType.S_ERROR, new { message = "已在匹配队列中" });
                    return;
                }

                _queue.Add(player);
                player.Send(MsgType.S_ROOM_JOINED, new { message = "正在匹配中..." });
                Console.WriteLine($"[匹配] 玩家 {player.Name} 加入匹配队列 (队列: {_queue.Count})");
            }
        }

        public void RemoveFromQueue(Player player)
        {
            lock (_lock)
            {
                _queue.Remove(player);
            }
        }

        public void ProcessQueue()
        {
            lock (_lock)
            {
                while (_queue.Count >= 2)
                {
                    var player1 = _queue[0];
                    var player2 = _queue[1];

                    if (!player1.Connection.IsAvailable || !player2.Connection.IsAvailable)
                    {
                        if (!player1.Connection.IsAvailable) _queue.RemoveAt(0);
                        if (!player2.Connection.IsAvailable) _queue.Remove(player2);
                        continue;
                    }

                    _queue.RemoveAt(0);
                    _queue.RemoveAt(0);

                    int roomId = Math.Abs(Guid.NewGuid().GetHashCode());
                    var room = new Room(roomId, player1, "1v1");
                    room.AddPlayer(player2);
                    _rooms[roomId] = room;

                    var roomData = room.ToJson();
                    player1.Send(MsgType.S_MATCHED, new { room = roomData, opponent = player2.ToJson() });
                    player2.Send(MsgType.S_MATCHED, new { room = roomData, opponent = player1.ToJson() });

                    Console.WriteLine($"[匹配] 玩家 {player1.Name} vs 玩家 {player2.Name} (房间 {roomId})");
                }
            }
        }

        public void AddRoom(int id, Room room) => _rooms[id] = room;

        public Room? GetRoom(int id) => _rooms.TryGetValue(id, out var room) ? room : null;

        public bool RemoveRoom(int id) => _rooms.TryRemove(id, out _);

        public IEnumerable<KeyValuePair<int, Room>> GetAllRooms() => _rooms;

        public int QueueCount
        {
            get { lock (_lock) { return _queue.Count; } }
        }

        public int RoomCount => _rooms.Count;
        public int PlayerCount => _rooms.Values.Sum(r => r.Players.Count);
    }
}
