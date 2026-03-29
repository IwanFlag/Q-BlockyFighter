using UnityEngine;
using System.Collections.Generic;

namespace QBlockyFighter.Map
{
    /// <summary>
    /// 野怪/资源刷新系统 - 5V5团战模式的野区管理
    /// 包含：野怪刷新、据点占领、Boss刷新
    /// </summary>
    public class WildMonsterSpawner : MonoBehaviour
    {
        [Header("野怪配置")]
        public float respawnDelay = 60f;          // 野怪重生时间
        public float levelUpInterval = 300f;      // 野怪升级间隔（5分钟）
        public float bossSpawnTime = 600f;        // Boss首次刷新（10分钟）

        [Header("据点")]
        public float captureTime = 10f;           // 据点占领时间
        public float captureRadius = 5f;

        private float gameTime;
        private int monsterLevel = 1;
        private bool bossSpawned = false;

        // 野怪点
        private List<WildCamp> camps = new List<WildCamp>();
        // 据点
        private List<CapturePoint> capturePoints = new List<CapturePoint>();

        void Start()
        {
            InitCamps();
            InitCapturePoints();
        }

        void Update()
        {
            gameTime += Time.deltaTime;

            // 野怪升级
            int newLevel = 1 + (int)(gameTime / levelUpInterval);
            if (newLevel > monsterLevel)
            {
                monsterLevel = newLevel;
                Debug.Log($"[野区] 野怪等级提升至 {monsterLevel}");
            }

            // Boss刷新
            if (!bossSpawned && gameTime >= bossSpawnTime)
            {
                bossSpawned = true;
                SpawnBoss();
            }

            // 更新野怪重生
            foreach (var camp in camps)
            {
                if (!camp.isAlive && camp.respawnTimer > 0)
                {
                    camp.respawnTimer -= Time.deltaTime;
                    if (camp.respawnTimer <= 0)
                    {
                        RespawnCamp(camp);
                    }
                }
            }

            // 更新据点
            UpdateCapturePoints();
        }

        private void InitCamps()
        {
            // 中路野区 - 左
            camps.Add(new WildCamp
            {
                position = new Vector3(-8, 0, 0),
                type = WildCamp.CampType.Normal,
                monsters = new List<GameObject>()
            });
            // 中路野区 - 右
            camps.Add(new WildCamp
            {
                position = new Vector3(8, 0, 0),
                type = WildCamp.CampType.Normal,
                monsters = new List<GameObject>()
            });
            // 上路野区
            camps.Add(new WildCamp
            {
                position = new Vector3(0, 0, 12),
                type = WildCamp.CampType.Elite,
                monsters = new List<GameObject>()
            });
            // 下路野区
            camps.Add(new WildCamp
            {
                position = new Vector3(0, 0, -12),
                type = WildCamp.CampType.Elite,
                monsters = new List<GameObject>()
            });

            // 初始生成
            foreach (var camp in camps)
            {
                SpawnCamp(camp);
            }
        }

        private void InitCapturePoints()
        {
            // 3个据点：左、中、右
            capturePoints.Add(new CapturePoint { position = new Vector3(-15, 0, 0), name = "左据点" });
            capturePoints.Add(new CapturePoint { position = new Vector3(0, 0, 0), name = "中央据点" });
            capturePoints.Add(new CapturePoint { position = new Vector3(15, 0, 0), name = "右据点" });
        }

        private void SpawnCamp(WildCamp camp)
        {
            int count = camp.type == WildCamp.CampType.Elite ? 2 : 3;
            float hp = camp.type == WildCamp.CampType.Elite ? 500 : 200;
            hp *= (1 + (monsterLevel - 1) * 0.3f);

            camp.monsters.Clear();
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
                var monster = CreateMonster(camp.position + offset, hp, camp.type);
                camp.monsters.Add(monster);
            }

            camp.isAlive = true;
            Debug.Log($"[野区] {camp.type}营地刷新 ({count}只, HP:{hp})");
        }

        private void RespawnCamp(WildCamp camp)
        {
            SpawnCamp(camp);
        }

        private GameObject CreateMonster(Vector3 pos, float hp, WildCamp.CampType type)
        {
            var go = GameObject.CreatePrimitive(type == WildCamp.CampType.Elite ? PrimitiveType.Capsule : PrimitiveType.Cube);
            go.transform.position = pos;
            go.transform.localScale = type == WildCamp.CampType.Elite ? Vector3.one * 1.3f : Vector3.one * 0.8f;
            go.name = $"WildMonster_L{monsterLevel}";
            go.tag = "Enemy";

            var health = go.AddComponent<Core.HealthSystem>();
            health.MaxHp = hp;
            health.CurrentHp = hp;

            var ai = go.AddComponent<AI.EnemyAI>();
            ai.AttackDamage = 10 + monsterLevel * 5;
            ai.DetectRange = 8f;

            // 颜色区分
            var renderer = go.GetComponent<Renderer>();
            renderer.material.color = type == WildCamp.CampType.Elite
                ? new Color(0.8f, 0.2f, 0.2f)
                : new Color(0.3f, 0.6f, 0.3f);

            // 死亡回调
            health.OnDeath += () => OnMonsterDeath(go, pos);

            return go;
        }

        private void OnMonsterDeath(GameObject monster, Vector3 campPos)
        {
            // 查找对应的营地
            foreach (var camp in camps)
            {
                if (camp.monsters.Contains(monster))
                {
                    camp.monsters.Remove(monster);
                    if (camp.monsters.Count == 0)
                    {
                        camp.isAlive = false;
                        camp.respawnTimer = respawnDelay;
                        Debug.Log($"[野区] 营地清除，{respawnDelay}秒后重生");
                    }
                    break;
                }
            }

            // 击杀者获得经验和武器品质提升
            // （由HealthSystem的OnDeath事件处理）

            Destroy(monster);
        }

        private void SpawnBoss()
        {
            Vector3 bossPos = new Vector3(0, 0, 0); // 地图中央
            var boss = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            boss.transform.position = bossPos;
            boss.transform.localScale = Vector3.one * 3f;
            boss.name = "WildBoss";
            boss.tag = "Enemy";

            var health = boss.AddComponent<Core.HealthSystem>();
            health.MaxHp = 3000;
            health.CurrentHp = 3000;

            var ai = boss.AddComponent<AI.BossAI>();
            ai.BossType = 2; // 混合型
            ai.AttackDamage = 50;

            var renderer = boss.GetComponent<Renderer>();
            renderer.material.color = new Color(0.9f, 0.1f, 0.1f);

            Debug.Log("[野区] Boss 刷新在地图中央!");
        }

        private void UpdateCapturePoints()
        {
            foreach (var cp in capturePoints)
            {
                // 检查范围内玩家
                var colliders = Physics.OverlapSphere(cp.position, captureRadius);
                bool teamAPresent = false;
                bool teamBPresent = false;

                foreach (var col in colliders)
                {
                    if (!col.CompareTag("Player")) continue;
                    // 简化：左侧半场为A队，右侧为B队
                    if (col.transform.position.x < 0) teamAPresent = true;
                    else teamBPresent = true;
                }

                if (teamAPresent && !teamBPresent)
                {
                    cp.captureProgress += Time.deltaTime / captureTime;
                    cp.owner = "A";
                }
                else if (teamBPresent && !teamAPresent)
                {
                    cp.captureProgress += Time.deltaTime / captureTime;
                    cp.owner = "B";
                }

                if (cp.captureProgress >= 1f && !cp.isCaptured)
                {
                    cp.isCaptured = true;
                    Debug.Log($"[据点] {cp.name} 被 {cp.owner} 队占领!");
                }
            }
        }

        public int GetCapturedCount(string team)
        {
            int count = 0;
            foreach (var cp in capturePoints)
            {
                if (cp.isCaptured && cp.owner == team) count++;
            }
            return count;
        }
    }

    // ===== 数据结构 =====

    [System.Serializable]
    public class WildCamp
    {
        public enum CampType { Normal, Elite }
        public Vector3 position;
        public CampType type;
        public List<GameObject> monsters;
        public bool isAlive;
        public float respawnTimer;
    }

    [System.Serializable]
    public class CapturePoint
    {
        public Vector3 position;
        public string name;
        public string owner;
        public float captureProgress;
        public bool isCaptured;
    }
}
