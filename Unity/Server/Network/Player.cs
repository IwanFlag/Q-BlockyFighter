using System;
using Fleck;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QBlockyFighter.Server.Protocol;

namespace QBlockyFighter.Server.Network
{
    public class PlayerInput
    {
        public JObject? Keys { get; set; }
        public JObject? Mouse { get; set; }
        public long Timestamp { get; set; }
    }

    public class Player
    {
        public int Id { get; }
        public string Name { get; set; }
        public int? RoomId { get; set; }
        public bool IsReady { get; set; }
        public bool IsSpectator { get; set; }
        public string? Character { get; set; }
        public int WeaponIndex { get; set; }
        public PlayerInput? LastInput { get; set; }
        public DateTime LastActivity { get; set; }
        public IWebSocketConnection Connection { get; }

        public Player(IWebSocketConnection connection, int id)
        {
            Connection = connection;
            Id = id;
            Name = $"玩家{id}";
            LastActivity = DateTime.UtcNow;
        }

        public void Send(string type, object data)
        {
            try
            {
                if (Connection.IsAvailable)
                {
                    var msg = JObject.FromObject(data);
                    msg["type"] = type;
                    Connection.Send(msg.ToString(Formatting.None));
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[错误] 发送消息给玩家 {Id}: {ex.Message}");
            }
        }

        public object ToJson()
        {
            return new
            {
                id = Id,
                name = Name,
                isReady = IsReady,
                character = Character
            };
        }
    }
}
