using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace QBlockyFighter.Network
{
    /// <summary>
    /// Message types matching the Node.js server protocol.
    /// </summary>
    public static class MsgType
    {
        // Client -> Server
        public const string C_JOIN = "join";
        public const string C_CREATE_ROOM = "create_room";
        public const string C_JOIN_ROOM = "join_room";
        public const string C_LEAVE_ROOM = "leave_room";
        public const string C_READY = "ready";
        public const string C_INPUT = "input";
        public const string C_CHAT = "chat";
        public const string C_SPECTATE = "spectate";
        public const string C_MATCHMAKE = "matchmake";
        public const string C_TRAINING = "training";

        // Server -> Client
        public const string S_WELCOME = "welcome";
        public const string S_ROOM_CREATED = "room_created";
        public const string S_ROOM_JOINED = "room_joined";
        public const string S_ROOM_LEFT = "room_left";
        public const string S_PLAYER_JOINED = "player_joined";
        public const string S_PLAYER_LEFT = "player_left";
        public const string S_GAME_START = "game_start";
        public const string S_FRAME = "frame";
        public const string S_GAME_STATE = "game_state";
        public const string S_CHAT = "chat";
        public const string S_MATCHED = "matched";
        public const string S_ERROR = "error";
        public const string S_ROOM_LIST = "room_list";
        public const string S_SPECTATE = "spectate";
    }

    /// <summary>
    /// Serializable input frame data for network transmission.
    /// </summary>
    [Serializable]
    public class InputFrame
    {
        public float moveX;
        public float moveY;
        public float moveZ;
        public float camYaw;
        public float camPitch;
        public bool lightAttack;
        public bool heavyAttack;
        public bool dodge;
        public bool block;
        public bool jump;
        public bool skillQ;
        public bool skillE;
        public bool skillR;
        public bool switchWeapon;
        public bool lockTarget;

        public JObject ToJson()
        {
            return JObject.FromObject(new
            {
                keys = new
                {
                    w = moveZ > 0, s = moveZ < 0, a = moveX < 0, d = moveX > 0,
                    space = jump, shift = dodge, f = block,
                    q = skillQ, e = skillE, r = skillR,
                    tab = switchWeapon, v = lockTarget
                },
                mouse = new
                {
                    left = lightAttack, right = heavyAttack,
                    yaw = camYaw, pitch = camPitch
                }
            });
        }

        public static InputFrame FromKeyboardMouse(float moveX, float moveZ, bool light, bool heavy,
            bool dodge, bool block, bool jump, bool q, bool e, bool r, bool switchWpn, bool lockTarget,
            float camYaw = 0, float camPitch = 0)
        {
            return new InputFrame
            {
                moveX = moveX,
                moveZ = moveZ,
                camYaw = camYaw,
                camPitch = camPitch,
                lightAttack = light,
                heavyAttack = heavy,
                dodge = dodge,
                block = block,
                jump = jump,
                skillQ = q,
                skillE = e,
                skillR = r,
                switchWeapon = switchWpn,
                lockTarget = lockTarget
            };
        }
    }

    /// <summary>
    /// Server frame data structure.
    /// </summary>
    public class ServerFrame
    {
        public int frame;
        public long timestamp;
        public JObject inputs;

        public static ServerFrame FromJson(JObject json)
        {
            return new ServerFrame
            {
                frame = json.Value<int>("frame"),
                timestamp = json.Value<long>("timestamp"),
                inputs = json["inputs"] as JObject
            };
        }
    }

    /// <summary>
    /// Player info from server.
    /// </summary>
    public class PlayerInfo
    {
        public int id;
        public string name;
        public bool isReady;
        public string character;

        public static PlayerInfo FromJson(JObject json)
        {
            return new PlayerInfo
            {
                id = json.Value<int>("id"),
                name = json.Value<string>("name") ?? "",
                isReady = json.Value<bool>("isReady"),
                character = json.Value<string>("character")
            };
        }
    }
}
