using System;
using System.Threading;
using QBlockyFighter.Server.Network;

namespace QBlockyFighter.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 3000;
            if (args.Length > 0 && int.TryParse(args[0], out int p)) port = p;

            Console.WriteLine("========================================");
            Console.WriteLine("  Q版方块人大乱斗 - 帧同步服务器 V0.3 (C#)");
            Console.WriteLine("========================================");
            Console.WriteLine($"  WebSocket: ws://localhost:{port}");
            Console.WriteLine($"  帧率: {GameConfig.TICK_RATE} fps");
            Console.WriteLine("========================================");

            var matchmaker = new Matchmaker();
            var gameLoop = new GameLoop(matchmaker);
            var server = new WebSocketServerManager(port, matchmaker, gameLoop);

            server.Start();

            Console.WriteLine("按 Ctrl+C 关闭服务器...");
            var exitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\n[关闭] 正在关闭服务器...");
                server.Stop();
                exitEvent.Set();
            };
            exitEvent.WaitOne();
        }
    }

    public static class GameConfig
    {
        public const int TICK_RATE = 20;
        public const int FRAME_INTERVAL_MS = 1000 / TICK_RATE;
        public const int MAX_PLAYERS_PER_ROOM = 2;
        public const int MAX_SPECTATORS = 10;
        public const int MATCHMAKING_INTERVAL_MS = 1000;
        public const int ROOM_CLEANUP_INTERVAL_MS = 30000;
        public const int IDLE_TIMEOUT_MS = 60000;
        public const int ROOM_IDLE_TIMEOUT_MS = 300000;
    }
}
