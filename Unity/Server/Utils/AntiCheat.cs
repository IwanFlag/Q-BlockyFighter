using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace QBlockyFighter.Server.Utils
{
    /// <summary>
    /// 防作弊验证系统 - 帧同步输入合法性校验
    /// </summary>
    public static class AntiCheat
    {
        // 检测阈值
        private const int MAX_INPUTS_PER_FRAME = 15;      // 单帧最大输入数
        private const float MAX_MOVE_SPEED = 20f;          // 最大移动速度
        private const int MAX_SKILLS_PER_SECOND = 8;       // 每秒最大技能释放数
        private const int SUSPICIOUS_THRESHOLD = 5;        // 可疑次数阈值
        private const int BAN_THRESHOLD = 10;              // 封禁阈值

        // 玩家状态追踪
        private static readonly ConcurrentDictionary<int, PlayerCheatState> _states = new();

        public class PlayerCheatState
        {
            public int PlayerId { get; set; }
            public int SuspiciousCount { get; set; }
            public int SkillCountThisSecond { get; set; }
            public DateTime LastSkillReset { get; set; }
            public float LastX { get; set; }
            public float LastY { get; set; }
            public float LastZ { get; set; }
            public DateTime LastPositionTime { get; set; }
            public List<string> Violations { get; set; } = new();
        }

        /// <summary>验证单帧输入</summary>
        public static ValidationResult ValidateInput(int playerId, Dictionary<string, object> input, float deltaTime)
        {
            var state = GetOrCreateState(playerId);
            var result = new ValidationResult { IsValid = true };

            if (input == null) return result;

            // 1. 检查输入数量（防止刷输入）
            if (input.Count > MAX_INPUTS_PER_FRAME)
            {
                result.IsValid = false;
                result.Reason = $"输入过多: {input.Count}/{MAX_INPUTS_PER_FRAME}";
                RecordViolation(state, result.Reason);
            }

            // 2. 检查移动速度（防止瞬移）
            if (input.ContainsKey("x") && input.ContainsKey("z"))
            {
                float x = Convert.ToSingle(input["x"]);
                float z = Convert.ToSingle(input["z"]);
                float dx = x - state.LastX;
                float dz = z - state.LastZ;
                float distance = (float)Math.Sqrt(dx * dx + dz * dz);
                float speed = distance / Math.Max(deltaTime, 0.016f);

                if (speed > MAX_MOVE_SPEED && state.LastX != 0)
                {
                    result.IsValid = false;
                    result.Reason = $"移动速度异常: {speed:F1} > {MAX_MOVE_SPEED}";
                    RecordViolation(state, result.Reason);
                }

                state.LastX = x;
                state.LastZ = z;
                state.LastPositionTime = DateTime.UtcNow;
            }

            // 3. 检查技能释放频率
            if (input.ContainsKey("q") || input.ContainsKey("e") || input.ContainsKey("r"))
            {
                if ((DateTime.UtcNow - state.LastSkillReset).TotalSeconds >= 1)
                {
                    state.SkillCountThisSecond = 0;
                    state.LastSkillReset = DateTime.UtcNow;
                }

                state.SkillCountThisSecond++;
                if (state.SkillCountThisSecond > MAX_SKILLS_PER_SECOND)
                {
                    result.IsValid = false;
                    result.Reason = $"技能释放过快: {state.SkillCountThisSecond}/{MAX_SKILLS_PER_SECOND}/s";
                    RecordViolation(state, result.Reason);
                }
            }

            // 4. 检查是否应被封禁
            if (state.SuspiciousCount >= BAN_THRESHOLD)
            {
                result.IsBanned = true;
                result.Reason = $"累计违规 {state.SuspiciousCount} 次，已封禁";
            }

            return result;
        }

        /// <summary>验证帧同步数据一致性</summary>
        public static bool ValidateFrameConsistency(int frame, Dictionary<int, Dictionary<string, object>> allInputs)
        {
            // 检查所有玩家在同一帧的输入是否合理
            foreach (var kvp in allInputs)
            {
                var result = ValidateInput(kvp.Key, kvp.Value, 0.05f);
                if (!result.IsValid)
                {
                    Logger.Warning($"[反作弊] 帧{frame} 玩家{kvp.Key}: {result.Reason}");
                }
            }
            return true;
        }

        private static PlayerCheatState GetOrCreateState(int playerId)
        {
            return _states.GetOrAdd(playerId, id => new PlayerCheatState
            {
                PlayerId = id,
                LastSkillReset = DateTime.UtcNow,
                LastPositionTime = DateTime.UtcNow
            });
        }

        private static void RecordViolation(PlayerCheatState state, string reason)
        {
            state.SuspiciousCount++;
            state.Violations.Add($"[{DateTime.Now:HH:mm:ss}] {reason}");

            if (state.SuspiciousCount >= SUSPICIOUS_THRESHOLD)
            {
                Logger.Warning($"[反作弊] 玩家{state.PlayerId} 可疑行为({state.SuspiciousCount}): {reason}");
            }

            // 只保留最近100条记录
            if (state.Violations.Count > 100)
                state.Violations.RemoveAt(0);
        }

        public static PlayerCheatState GetPlayerState(int playerId)
        {
            return _states.GetValueOrDefault(playerId);
        }

        public static void ResetPlayer(int playerId)
        {
            _states.TryRemove(playerId, out _);
        }

        public static void Clear()
        {
            _states.Clear();
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public bool IsBanned { get; set; }
        public string Reason { get; set; }
    }
}
