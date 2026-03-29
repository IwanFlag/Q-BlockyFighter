namespace QBlockyFighter.Server.Protocol
{
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
}
