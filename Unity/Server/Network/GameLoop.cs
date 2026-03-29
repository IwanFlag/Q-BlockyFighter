using System;
using System.Threading;
using System.Threading.Tasks;

namespace QBlockyFighter.Server.Network
{
    public class GameLoop
    {
        private readonly Matchmaker _matchmaker;
        private CancellationTokenSource? _cts;
        private Task? _loopTask;

        public GameLoop(Matchmaker matchmaker)
        {
            _matchmaker = matchmaker;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _loopTask = Task.Run(() => Run(_cts.Token));
            Console.WriteLine($"[游戏循环] 已启动 (帧率: {GameConfig.TICK_RATE} fps)");
        }

        public void Stop()
        {
            _cts?.Cancel();
            _loopTask?.Wait(2000);
            Console.WriteLine("[游戏循环] 已停止");
        }

        private void Run(CancellationToken token)
        {
            var nextTick = DateTime.UtcNow;

            while (!token.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;

                if (now >= nextTick)
                {
                    Tick();
                    nextTick = now.AddMilliseconds(GameConfig.FRAME_INTERVAL_MS);
                }

                // Sleep until next tick (with some margin to avoid busy-waiting)
                var sleepMs = (int)(nextTick - DateTime.UtcNow).TotalMilliseconds;
                if (sleepMs > 1)
                {
                    Thread.Sleep(sleepMs - 1);
                }
            }
        }

        private void Tick()
        {
            foreach (var kvp in _matchmaker.GetAllRooms())
            {
                var room = kvp.Value;
                if (room.State != "playing") continue;

                var frameData = room.CollectFrameInput();
                room.BroadcastFrame(frameData);
                room.AdvanceFrame();
            }
        }
    }
}
