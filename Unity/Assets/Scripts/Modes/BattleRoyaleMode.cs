using UnityEngine;
using System.Collections.Generic;

namespace QBlockyFighter.Modes
{
    /// <summary>
    /// 吃鸡模式 - 大逃杀/生存竞技
    /// 多名玩家(8-20人)在不断缩小的安全区内战斗
    /// 最后存活的玩家/队伍获胜
    /// </summary>
    public class BattleRoyaleMode : GameMode
    {
        [Header("吃鸡配置")]
        public int TotalPlayers = 20;            // 总玩家数（含AI填充）
        public int BotCount = 16;                // AI机器人数量
        public float SafeZoneStartRadius = 80f;  // 初始安全区半径
        public float SafeZoneMinRadius = 5f;     // 最小安全区
        public float ShrinkInterval = 90f;       // 缩圈间隔（秒）
        public float ShrinkDuration = 30f;       // 缩圈持续时间
        public float ZoneDamage = 5f;            // 圈外每秒伤害
        public float WeaponSpawnInterval = 30f;  // 武器刷新间隔

        private float safeRadius;
        private Vector3 safeCenter;
        private float shrinkTimer;
        private float weaponTimer;
        private bool isShrinking;
        private float nextShrinkRadius;
        private int aliveCount;
        private int playerRank; // 玩家排名

        // 武器掉落点
        private List<Vector3> weaponDrops = new List<Vector3>();

        public override void Initialize(List<PlayerController> modePlayers)
        {
            ModeType = GameModeType.BattleRoyale;
            base.Initialize(modePlayers);
            safeRadius = SafeZoneStartRadius;
            safeCenter = Vector3.zero;
            aliveCount = TotalPlayers;
            playerRank = 0;
        }

        protected override void OnModeStart()
        {
            Debug.Log($"[吃鸡] 大逃杀开始 - {TotalPlayers}人");

            // 随机分散玩家
            foreach (var p in players)
            {
                p.transform.position = GetRandomSpawnInZone();
            }

            // 生成AI填充
            SpawnBots();

            // 初始武器掉落
            for (int i = 0; i < 10; i++)
            {
                SpawnWeaponDrop();
            }
        }

        protected override void OnModeEnd()
        {
            if (playerRank <= 0) playerRank = aliveCount;
            Debug.Log($"[吃鸡] 结束 - 玩家排名: #{playerRank}/{TotalPlayers}");
        }

        protected override void OnTimeUp()
        {
            // 吃鸡模式没有时间限制，直到最后一个人
        }

        protected override void Update()
        {
            base.Update();
            if (!IsActive) return;

            // 缩圈计时
            shrinkTimer += Time.deltaTime;

            if (!isShrinking && shrinkTimer >= ShrinkInterval)
            {
                StartShrink();
            }

            if (isShrinking)
            {
                float shrinkProgress = (shrinkTimer - ShrinkInterval) / ShrinkDuration;
                safeRadius = Mathf.Lerp(safeRadius, nextShrinkRadius, shrinkProgress * Time.deltaTime);

                if (shrinkProgress >= 1f)
                {
                    isShrinking = false;
                    shrinkTimer = 0;
                    safeRadius = nextShrinkRadius;
                    Debug.Log($"[吃鸡] 安全区缩小完成 - 半径: {safeRadius:F0}");
                }
            }

            // 武器刷新
            weaponTimer += Time.deltaTime;
            if (weaponTimer >= WeaponSpawnInterval)
            {
                weaponTimer = 0;
                SpawnWeaponDrop();
            }

            // 圈外伤害检测
            foreach (var p in players)
            {
                var hp = p.GetComponent<Core.HealthSystem>();
                if (hp != null && !hp.IsDead && !IsInSafeZone(p.transform.position))
                {
                    float dmg = ZoneDamage * (1 + GetShrinkPhase() * 0.5f); // 后期伤害更高
                    hp.TakeDamage(dmg * Time.deltaTime, false);
                }
            }

            // 检查存活人数
            CheckAlive();
        }

        private void StartShrink()
        {
            isShrinking = true;
            nextShrinkRadius = Mathf.Max(SafeZoneMinRadius, safeRadius * 0.6f);

            // 安全区中心随机偏移
            Vector2 offset = Random.insideUnitCircle * (safeRadius - nextShrinkRadius) * 0.3f;
            safeCenter += new Vector3(offset.x, 0, offset.y);

            int phase = GetShrinkPhase();
            Debug.Log($"[吃鸡] 第{phase + 1}次缩圈 - {safeRadius:F0} → {nextShrinkRadius:F0}");
        }

        private int GetShrinkPhase()
        {
            return Mathf.FloorToInt(GameTime / ShrinkInterval);
        }

        private bool IsInSafeZone(Vector3 pos)
        {
            return Vector3.Distance(new Vector3(pos.x, 0, pos.z),
                   new Vector3(safeCenter.x, 0, safeCenter.z)) <= safeRadius;
        }

        private void CheckAlive()
        {
            int alive = 0;
            bool playerAlive = false;

            foreach (var p in players)
            {
                var hp = p.GetComponent<Core.HealthSystem>();
                if (hp != null && !hp.IsDead)
                {
                    alive++;
                    if (p.IsLocalPlayer) playerAlive = true;
                }
            }

            // 玩家死亡时记录排名
            if (playerAlive == false && playerRank <= 0)
            {
                playerRank = alive + 1;
                Debug.Log($"[吃鸡] 玩家淘汰 - 排名 #{playerRank}");
            }

            aliveCount = alive;

            // 最后一人获胜
            if (alive <= 1)
            {
                EndMode();
            }
        }

        private void SpawnBots()
        {
            for (int i = 0; i < BotCount; i++)
            {
                Vector3 pos = GetRandomSpawnInZone();
                var bot = CreateBot(pos, $"Bot_{i + 1}");
                players.Add(bot.GetComponent<PlayerController>());
            }
        }

        private GameObject CreateBot(Vector3 pos, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = pos;
            go.name = name;
            go.tag = "Player";

            var hp = go.AddComponent<Core.HealthSystem>();
            hp.MaxHp = 800;
            hp.CurrentHp = 800;

            var ai = go.AddComponent<AI.EnemyAI>();
            ai.AttackDamage = 30;
            ai.DetectRange = 15f;
            ai.MoveSpeed = 4f;

            // 随机颜色区分
            var renderer = go.GetComponent<Renderer>();
            renderer.material.color = new Color(Random.value, Random.value, Random.value);

            return go;
        }

        private Vector3 GetRandomSpawnInZone()
        {
            Vector2 circle = Random.insideUnitCircle * safeRadius * 0.9f;
            return new Vector3(safeCenter.x + circle.x, 0, safeCenter.z + circle.y);
        }

        private void SpawnWeaponDrop()
        {
            Vector3 pos = GetRandomSpawnInZone();
            weaponDrops.Add(pos);
            // 实际由WeaponDropSystem处理
            var spawner = Core.WeaponDropSystem.Instance;
            if (spawner != null)
            {
                var quality = (Core.WeaponQuality)Random.Range(0, 4);
                spawner.SpawnDropAt(pos, quality);
            }
        }

        public override void OnPlayerDeath(PlayerController player, PlayerController killer)
        {
            // 击杀奖励
            if (killer != null)
            {
                var killerHP = killer.GetComponent<Core.HealthSystem>();
                if (killerHP != null)
                {
                    killerHP.Heal(killerHP.MaxHp * 0.2f); // 击杀回复20%血量
                }
            }
        }

        public override bool CheckGameOver(out string winner)
        {
            winner = "";
            if (aliveCount <= 1)
            {
                // 找到存活者
                foreach (var p in players)
                {
                    var hp = p.GetComponent<Core.HealthSystem>();
                    if (hp != null && !hp.IsDead)
                    {
                        winner = p.name;
                        return true;
                    }
                }
                return true;
            }
            return false;
        }

        // ===== 公开接口 =====
        public float GetSafeZoneRadius() => safeRadius;
        public Vector3 GetSafeZoneCenter() => safeCenter;
        public int GetAliveCount() => aliveCount;
        public int GetTotalPlayers() => TotalPlayers;
        public int GetPlayerRank() => playerRank;
        public float GetZoneDamage() => ZoneDamage * (1 + GetShrinkPhase() * 0.5f);
        public bool IsZoneShrinking() => isShrinking;
    }
}
