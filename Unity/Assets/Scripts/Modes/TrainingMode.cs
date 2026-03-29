using UnityEngine;
using System.Collections.Generic;

namespace QBlockyFighter.Modes
{
    public class TrainingMode : GameMode
    {
        private List<GameObject> dummies = new List<GameObject>();
        public int ChallengeWave { get; private set; }
        public int ChallengeScore { get; private set; }

        // 闯关区域
        private List<GameObject> challengeEnemies = new List<GameObject>();

        // 武器架区域
        private bool weaponRackActive = false;
        private int currentTestWeaponIndex = 0;

        // Boss模拟区域
        private bool bossSimActive = false;
        private int bossType = 0;

        public override void Initialize(List<PlayerController> modePlayers)
        {
            ModeType = GameModeType.Training;
            base.Initialize(modePlayers);
        }

        protected override void OnModeStart()
        {
            Debug.Log("[Training] 训练场开始");
            InitDummies();
        }

        protected override void OnModeEnd()
        {
            Debug.Log("[Training] 训练场结束");
            CleanupDummies();
        }

        protected override void OnTimeUp()
        {
            // 训练场无时间限制
        }

        public override void OnPlayerDeath(PlayerController player, PlayerController killer)
        {
            // 训练场不死，自动回满
            var hp = player.GetComponent<HealthSystem>();
            hp?.ResetState();
        }

        private void InitDummies()
        {
            CleanupDummies();
            // 在木人桩区生成3个训练假人
            for (int i = 0; i < 3; i++)
            {
                var dummy = CreateDummy(new Vector3(i * 3f, 0, -10f));
                dummies.Add(dummy);
            }
        }

        private GameObject CreateDummy(Vector3 pos)
        {
            var dummy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dummy.transform.position = pos;
            dummy.transform.localScale = new Vector3(1f, 1.5f, 1f);
            dummy.name = "TrainingDummy";

            var hp = dummy.AddComponent<HealthSystem>();
            hp.MaxHp = 999999;
            hp.CurrentHp = 999999;

            var rb = dummy.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            // 添加碰撞标签
            dummy.tag = "Enemy";
            return dummy;
        }

        private void CleanupDummies()
        {
            foreach (var d in dummies) if (d != null) Destroy(d);
            dummies.Clear();
        }

        // ===== 闯关挑战 =====
        public void StartChallenge(int startWave = 1)
        {
            ChallengeWave = startWave;
            ChallengeScore = 0;
            SpawnWave();
        }

        public void NextWave()
        {
            ChallengeWave++;
            SpawnWave();
        }

        private void SpawnWave()
        {
            int enemyCount = Mathf.Min(ChallengeWave + 2, 10);
            float enemyHP = 100 + ChallengeWave * 50;
            float enemyDmg = 10 + ChallengeWave * 5;

            challengeEnemies.Clear();
            for (int i = 0; i < enemyCount; i++)
            {
                var enemy = CreateEnemy(new Vector3(Random.Range(-8f, 8f), 0, Random.Range(-8f, 8f)), enemyHP, enemyDmg);
                challengeEnemies.Add(enemy);
            }
            Debug.Log($"[Challenge] 第{ChallengeWave}波 - {enemyCount}个敌人");
        }

        private GameObject CreateEnemy(Vector3 pos, float hp, float dmg)
        {
            var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.transform.position = pos;
            enemy.name = $"ChallengeEnemy_W{ChallengeWave}";

            var health = enemy.AddComponent<HealthSystem>();
            health.MaxHp = hp;
            health.CurrentHp = hp;

            var ai = enemy.AddComponent<AI.EnemyAI>();
            ai.AttackDamage = dmg;

            enemy.tag = "Enemy";
            return enemy;
        }

        // ===== 武器架试炼 =====
        public void StartWeaponRack()
        {
            weaponRackActive = true;
            currentTestWeaponIndex = 0;
            Debug.Log("[Training] 武器架试炼开始 - 16种武器");
        }

        public void SwitchTestWeapon(int index)
        {
            if (index >= 0 && index < 16)
            {
                currentTestWeaponIndex = index;
                // 切换当前玩家武器
                if (players.Count > 0)
                {
                    var ws = players[0].GetComponent<WeaponSystem>();
                    if (ws != null) ws.CurrentWeaponIndex = index;
                }
            }
        }

        // ===== Boss模拟 =====
        public void StartBossSimulation(int type = 0)
        {
            bossSimActive = true;
            bossType = type;
            var boss = CreateBoss(new Vector3(0, 0, -8f), type);
            Debug.Log($"[Training] Boss模拟开始 - 类型{type}");
        }

        private GameObject CreateBoss(Vector3 pos, int type)
        {
            var boss = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            boss.transform.position = pos;
            boss.transform.localScale = Vector3.one * 2f;
            boss.name = $"Boss_Type{type}";

            var hp = boss.AddComponent<HealthSystem>();
            hp.MaxHp = 5000;
            hp.CurrentHp = 5000;

            var ai = boss.AddComponent<AI.BossAI>();
            ai.BossType = type;

            boss.tag = "Enemy";
            return boss;
        }

        public override bool CheckGameOver(out string winner)
        {
            winner = "";
            return false; // 训练场不会结束
        }
    }
}
