using UnityEngine;
using System.Collections.Generic;

namespace QBlockyFighter.Modes
{
    public class DuelMode : GameMode
    {
        private int[] wins = new int[2];
        private int targetWins = 3; // BO5

        public override void Initialize(List<PlayerController> modePlayers)
        {
            ModeType = GameModeType.Duel1v1;
            base.Initialize(modePlayers);
            wins[0] = 0;
            wins[1] = 0;
        }

        protected override void OnModeStart()
        {
            Debug.Log("[Duel] 1V1决斗开始 - BO5");
            ResetRound();
        }

        protected override void OnModeEnd()
        {
            Debug.Log("[Duel] 决斗结束");
        }

        protected override void OnTimeUp()
        {
            // 时间到，血量多的赢
            float hp0 = players.Count > 0 ? players[0].GetComponent<HealthSystem>().CurrentHp : 0;
            float hp1 = players.Count > 1 ? players[1].GetComponent<HealthSystem>().CurrentHp : 0;
            int winner = hp0 >= hp1 ? 0 : 1;
            wins[winner]++;
            CheckRoundEnd();
        }

        public override void OnPlayerDeath(PlayerController player, PlayerController killer)
        {
            if (players.Count < 2) return;
            int killerIdx = players.IndexOf(killer);
            if (killerIdx >= 0 && killerIdx < 2)
            {
                wins[killerIdx]++;
            }
            CheckRoundEnd();
        }

        private void CheckRoundEnd()
        {
            if (wins[0] >= targetWins)
            {
                Debug.Log($"[Duel] 玩家1 获胜！({wins[0]}-{wins[1]})");
                EndMode();
            }
            else if (wins[1] >= targetWins)
            {
                Debug.Log($"[Duel] 玩家2 获胜！({wins[0]}-{wins[1]})");
                EndMode();
            }
            else
            {
                Invoke(nameof(ResetRound), 2f);
            }
        }

        private void ResetRound()
        {
            GameTime = 0f;
            foreach (var p in players)
            {
                var hp = p.GetComponent<HealthSystem>();
                hp?.ResetState();
                // 重置位置
                p.transform.position = players.IndexOf(p) == 0 ? new Vector3(-3, 0, 0) : new Vector3(3, 0, 0);
            }
        }

        public int GetWins(int playerIndex) => playerIndex >= 0 && playerIndex < 2 ? wins[playerIndex] : 0;
        public override bool CheckGameOver(out string winner)
        {
            winner = "";
            if (wins[0] >= targetWins) { winner = "玩家1"; return true; }
            if (wins[1] >= targetWins) { winner = "玩家2"; return true; }
            return false;
        }
    }
}
