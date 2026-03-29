using UnityEngine;
using System.Collections.Generic;

namespace QBlockyFighter.Modes
{
    public class ChallengeMode : GameMode
    {
        public int CurrentWave { get; private set; }
        public int TotalScore { get; private set; }
        public int ComboMax { get; private set; }
        private int currentCombo;
        private List<GameObject> waveEnemies = new List<GameObject>();
        private bool waveInProgress;

        // 评分维度
        private float waveStartTime;
        private int hitCount;
        private int noDamageWaves;

        public override void Initialize(List<PlayerController> modePlayers)
        {
            ModeType = GameModeType.Challenge;
            base.Initialize(modePlayers);
            CurrentWave = 0;
            TotalScore = 0;
            ComboMax = 0;
        }

        protected override void OnModeStart()
        {
            StartWave(1);
        }

        protected override void OnModeEnd()
        {
            int finalScore = CalculateFinalScore();
            Debug.Log($"[Challenge] 闯关结束 - 波次:{CurrentWave} 总分:{finalScore}");
        }

        protected override void OnTimeUp()
        {
            EndMode();
        }

        public void StartWave(int wave)
        {
            CurrentWave = wave;
            waveInProgress = true;
            currentCombo = 0;
            hitCount = 0;
            waveStartTime = Time.time;
            SpawnWaveEnemies(wave);
        }

        private void SpawnWaveEnemies(int wave)
        {
            ClearWaveEnemies();
            int count = Mathf.Min(3 + wave, 8);
            bool isElite = wave % 5 == 0;
            bool isBoss = wave % 10 == 0;

            if (isBoss)
            {
                var boss = SpawnBoss(wave);
                waveEnemies.Add(boss);
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    float hp = (50 + wave * 30) * (isElite ? 3f : 1f);
                    float dmg = (5 + wave * 3) * (isElite ? 2f : 1f);
                    var enemy = SpawnEnemy(
                        new Vector3(Random.Range(-12f, 12f), 0, Random.Range(-12f, 12f)),
                        hp, dmg, isElite
                    );
                    waveEnemies.Add(enemy);
                }
            }

            Debug.Log($"[Challenge] 第{wave}波 - {waveEnemies.Count}个敌人" + (isElite ? " [精英]" : "") + (isBoss ? " [BOSS]" : ""));
        }

        private GameObject SpawnEnemy(Vector3 pos, float hp, float dmg, bool isElite)
        {
            var enemy = GameObject.CreatePrimitive(isElite ? PrimitiveType.Capsule : PrimitiveType.Cube);
            enemy.transform.position = pos;
            enemy.transform.localScale = isElite ? Vector3.one * 1.5f : Vector3.one;
            enemy.name = $"ChallengeEnemy_W{CurrentWave}";

            var health = enemy.AddComponent<HealthSystem>();
            health.MaxHP = hp;
            health.CurrentHP = hp;

            var ai = enemy.AddComponent<AI.EnemyAI>();
            ai.AttackDamage = dmg;

            enemy.tag = "Enemy";
            return enemy;
        }

        private GameObject SpawnBoss(int wave)
        {
            var boss = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            boss.transform.position = new Vector3(0, 0, -10f);
            boss.transform.localScale = Vector3.one * 2.5f;
            boss.name = $"ChallengeBoss_W{wave}";

            var health = boss.AddComponent<HealthSystem>();
            health.MaxHP = 500 + wave * 200;
            health.CurrentHP = health.MaxHP;

            var ai = boss.AddComponent<AI.BossAI>();
            ai.BossType = (wave / 10) % 3;

            boss.tag = "Enemy";
            return boss;
        }

        protected override void Update()
        {
            base.Update();
            if (!IsActive || !waveInProgress) return;

            // 检查波次完成
            waveEnemies.RemoveAll(e => e == null);
            if (waveEnemies.Count == 0 && waveInProgress)
            {
                OnWaveComplete();
            }
        }

        private void OnWaveComplete()
        {
            waveInProgress = false;
            float timeBonus = Mathf.Max(0, 1f - (Time.time - waveStartTime) / 60f) * 100;
            int waveScore = CurrentWave * 100 + (int)timeBonus + ComboMax * 10;
            TotalScore += waveScore;

            if (players.Count > 0 && players[0].GetComponent<HealthSystem>().CurrentHP == players[0].GetComponent<HealthSystem>().MaxHP)
            {
                noDamageWaves++;
            }

            Debug.Log($"[Challenge] 第{CurrentWave}波完成! 得分:{waveScore} 总分:{TotalScore}");

            // 自动进入下一波
            Invoke(nameof(NextWave), 2f);
        }

        private void NextWave()
        {
            if (CurrentWave >= 100)
            {
                EndMode();
                return;
            }
            StartWave(CurrentWave + 1);
        }

        public void RegisterHit()
        {
            hitCount++;
            currentCombo++;
            if (currentCombo > ComboMax) ComboMax = currentCombo;
        }

        public void ResetCombo()
        {
            currentCombo = 0;
        }

        private int CalculateFinalScore()
        {
            return TotalScore + noDamageWaves * 500 + ComboMax * 20;
        }

        private void ClearWaveEnemies()
        {
            foreach (var e in waveEnemies) if (e != null) Destroy(e);
            waveEnemies.Clear();
        }

        public override void OnPlayerDeath(PlayerController player, PlayerController killer)
        {
            // 闯关模式玩家死亡 = 结束
            EndMode();
        }

        public override bool CheckGameOver(out string winner)
        {
            winner = "";
            return CurrentWave >= 100;
        }
    }
}
