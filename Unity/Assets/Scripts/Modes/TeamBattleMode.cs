using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace QBlockyFighter.Modes
{
    public class TeamBattleMode : GameMode
    {
        private List<PlayerController> teamA = new List<PlayerController>();
        private List<PlayerController> teamB = new List<PlayerController>();
        private int teamAScore;
        private int teamBScore;
        private float bossSpawnTimer;
        private float bossSpawnInterval = 600f; // 10分钟
        private bool bossSpawned = false;

        public override void Initialize(List<PlayerController> modePlayers)
        {
            ModeType = GameModeType.TeamBattle5v5;
            base.Initialize(modePlayers);
            // 分组：前5个A队，后5个B队
            for (int i = 0; i < players.Count; i++)
            {
                if (i < 5) teamA.Add(players[i]);
                else teamB.Add(players[i]);
            }
            teamAScore = 0;
            teamBScore = 0;
        }

        protected override void OnModeStart()
        {
            Debug.Log("[Team] 5V5团战开始");
            // A队左半场，B队右半场
            foreach (var p in teamA) p.transform.position = new Vector3(Random.Range(-20f, -5f), 0, Random.Range(-10f, 10f));
            foreach (var p in teamB) p.transform.position = new Vector3(Random.Range(5f, 20f), 0, Random.Range(-10f, 10f));
        }

        protected override void OnModeEnd()
        {
            Debug.Log($"[Team] 团战结束 - A:{teamAScore} B:{teamBScore}");
        }

        protected override void OnTimeUp()
        {
            EndMode();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsActive) return;

            // Boss刷新计时
            if (!bossSpawned)
            {
                bossSpawnTimer += Time.deltaTime;
                if (bossSpawnTimer >= bossSpawnInterval)
                {
                    bossSpawned = true;
                    SpawnBoss();
                }
            }

            // 检查全灭
            bool teamADead = teamA.All(p => p.GetComponent<HealthSystem>().IsDead);
            bool teamBDead = teamB.All(p => p.GetComponent<HealthSystem>().IsDead);

            if (teamADead && !teamBDead) { teamBScore++; CheckTeamGameOver(); }
            else if (teamBDead && !teamADead) { teamAScore++; CheckTeamGameOver(); }
            else if (teamADead && teamBDead) { /* 平局 */ CheckTeamGameOver(); }
        }

        public override void OnPlayerDeath(PlayerController player, PlayerController killer)
        {
            if (teamA.Contains(killer)) teamAScore++;
            else if (teamB.Contains(killer)) teamBScore++;
        }

        private void SpawnBoss()
        {
            Debug.Log("[Team] Boss刷新在地图中央!");
            // Boss生成逻辑由BossAI组件处理
        }

        private void CheckTeamGameOver()
        {
            if (GameTime >= MaxGameTime || teamAScore >= 30 || teamBScore >= 30)
            {
                EndMode();
            }
            else
            {
                // 重置所有玩家
                foreach (var p in players)
                {
                    p.GetComponent<HealthSystem>()?.ResetHealth();
                }
                Invoke(nameof(OnModeStart), 3f);
            }
        }

        public int GetTeamAScore() => teamAScore;
        public int GetTeamBScore() => teamBScore;
        public bool IsInTeamA(PlayerController p) => teamA.Contains(p);
        public override bool CheckGameOver(out string winner)
        {
            winner = teamAScore > teamBScore ? "A队" : teamBScore > teamAScore ? "B队" : "平局";
            return GameTime >= MaxGameTime || teamAScore >= 30 || teamBScore >= 30;
        }
    }
}
