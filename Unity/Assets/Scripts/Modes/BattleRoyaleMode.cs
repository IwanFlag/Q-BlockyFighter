using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace QBlockyFighter.Modes
{
    /// <summary>
    /// 吃鸡模式 V2 - 10人双人组队大逃杀
    /// 特色：NPC任务、野怪刷宝、遗迹探险、Boss战
    /// </summary>
    public class BattleRoyaleMode : GameMode
    {
        [Header("基础配置")]
        public int TotalPlayers = 10;            // 总玩家数
        public int TeamSize = 2;                 // 每队人数
        public int BotCount = 8;                 // AI机器人数量（填满10人）

        [Header("安全区配置")]
        public float SafeZoneStartRadius = 60f;  // 初始安全区半径
        public float SafeZoneMinRadius = 8f;     // 最小安全区
        public float ShrinkInterval = 120f;      // 缩圈间隔（秒）
        public float ShrinkDuration = 30f;       // 缩圈持续时间
        public float ZoneDamage = 5f;            // 圈外每秒伤害

        [Header("武器/掉落")]
        public float WeaponSpawnInterval = 45f;  // 武器刷新间隔
        public int InitialWeaponDrops = 8;       // 初始武器掉落

        [Header("野怪配置")]
        public int WildMonsterCampCount = 6;     // 野怪营地数量
        public float MonsterRespawnDelay = 45f;  // 野怪重生时间
        public float MonsterLevelUpInterval = 180f; // 野怪升级间隔

        [Header("Boss配置")]
        public float BossSpawnTime = 300f;       // Boss首次刷新（5分钟）
        public float BossRespawnTime = 420f;     // Boss重生时间（7分钟）

        [Header("遗迹配置")]
        public int RuinCount = 3;               // 遗迹数量
        public float RuinExploreTime = 5f;      // 遗迹探索时间

        [Header("NPC配置")]
        public int MerchantCount = 2;           // 商人NPC数量
        public int QuestGiverCount = 2;         // 任务NPC数量

        // ===== 内部状态 =====
        private float safeRadius;
        private Vector3 safeCenter;
        private float shrinkTimer;
        private float weaponTimer;
        private bool isShrinking;
        private float nextShrinkRadius;
        private int aliveCount;
        private int playerRank;
        private int monsterLevel = 1;
        private bool bossSpawned = false;
        private float bossTimer;

        // 组队
        private List<Team> teams = new List<Team>();

        // 武器掉落点
        private List<Vector3> weaponDrops = new List<Vector3>();

        // 野怪营地
        private List<WildMonsterCamp> monsterCamps = new List<WildMonsterCamp>();

        // 遗迹
        private List<RuinSite> ruins = new List<RuinSite>();

        // NPC
        private List<GameObject> npcObjects = new List<GameObject>();

        // Boss
        private GameObject currentBoss;

        // 金币系统
        private Dictionary<int, int> playerGold = new Dictionary<int, int>(); // playerId -> gold

        // ===== 初始化 =====
        public override void Initialize(List<PlayerController> modePlayers)
        {
            ModeType = GameModeType.BattleRoyale;
            base.Initialize(modePlayers);
            safeRadius = SafeZoneStartRadius;
            safeCenter = Vector3.zero;
            aliveCount = TotalPlayers;
            playerRank = 0;
            monsterLevel = 1;
        }

        protected override void OnModeStart()
        {
            Debug.Log($"[吃鸡V2] 大逃杀开始 - {TotalPlayers}人双人组队");

            // 组队分配
            AssignTeams();

            // 队伍内分散出生
            SpawnTeams();

            // 生成AI填充
            SpawnBots();

            // 初始化所有玩家为徒手状态
            var rpgPickup = Core.RPGWeaponPickup.Instance;
            if (rpgPickup != null)
            {
                foreach (var p in players)
                {
                    rpgPickup.InitPlayerUnarmed(p.gameObject);
                }
            }

            // 初始武器掉落
            for (int i = 0; i < InitialWeaponDrops; i++)
            {
                SpawnWeaponDrop();
            }

            // 生成野怪营地
            InitMonsterCamps();

            // 生成遗迹
            InitRuins();

            // 生成NPC
            SpawnNPCs();

            Debug.Log($"[吃鸡V2] 地图初始化完成 - {RuinCount}遗迹, {WildMonsterCampCount}野怪营地, " +
                      $"{MerchantCount}商人, {QuestGiverCount}任务NPC, 全员徒手出发!");
        }

        protected override void OnModeEnd()
        {
            if (playerRank <= 0) playerRank = aliveCount;

            // 结算队伍排名
            var sortedTeams = teams.OrderByDescending(t => t.GetAliveCount()).ThenByDescending(t => t.totalKills).ToList();
            for (int i = 0; i < sortedTeams.Count; i++)
            {
                Debug.Log($"[吃鸡V2] 第{i + 1}名: {sortedTeams[i].teamName} " +
                          $"(击杀:{sortedTeams[i].totalKills} 存活:{sortedTeams[i].GetAliveCount()})");
            }
        }

        protected override void OnTimeUp()
        {
            // 吃鸡模式没有时间限制，直到最后一队
        }

        // ===== 主循环 =====
        protected override void Update()
        {
            base.Update();
            if (!IsActive) return;

            UpdateSafeZone();
            UpdateWeaponSpawns();
            UpdateZoneDamage();
            UpdateMonsterCamps();
            UpdateBoss();
            CheckAlive();
        }

        // ===== 组队系统 =====
        private void AssignTeams()
        {
            teams.Clear();
            int teamCount = TotalPlayers / TeamSize;
            for (int i = 0; i < teamCount; i++)
            {
                var team = new Team
                {
                    teamId = i,
                    teamName = $"队伍{i + 1}",
                    members = new List<PlayerController>(),
                    totalKills = 0
                };
                teams.Add(team);
            }

            // 分配真实玩家
            for (int i = 0; i < players.Count && i < TotalPlayers; i++)
            {
                int teamIdx = i / TeamSize;
                if (teamIdx < teams.Count)
                {
                    teams[teamIdx].members.Add(players[i]);
                    playerGold[players[i].GetInstanceID()] = 0;
                }
            }
        }

        private void SpawnTeams()
        {
            foreach (var team in teams)
            {
                // 每队一个随机出生点（队员在一起）
                Vector3 teamSpawn = GetRandomSpawnInZone();
                float spawnSpread = 3f;

                foreach (var member in team.members)
                {
                    Vector3 offset = new Vector3(Random.Range(-spawnSpread, spawnSpread), 0,
                                                 Random.Range(-spawnSpread, spawnSpread));
                    member.transform.position = teamSpawn + offset;
                }
            }
        }

        /// <summary>
        /// 获取玩家所在队伍
        /// </summary>
        public Team GetPlayerTeam(PlayerController player)
        {
            return teams.FirstOrDefault(t => t.members.Contains(player));
        }

        /// <summary>
        /// 获取队友
        /// </summary>
        public PlayerController GetTeammate(PlayerController player)
        {
            var team = GetPlayerTeam(player);
            if (team == null) return null;
            return team.members.FirstOrDefault(m => m != player);
        }

        // ===== 安全区系统 =====
        private void UpdateSafeZone()
        {
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
                    Debug.Log($"[吃鸡V2] 安全区缩小完成 - 半径: {safeRadius:F0}");
                }
            }
        }

        private void StartShrink()
        {
            isShrinking = true;
            nextShrinkRadius = Mathf.Max(SafeZoneMinRadius, safeRadius * 0.65f);

            Vector2 offset = Random.insideUnitCircle * (safeRadius - nextShrinkRadius) * 0.3f;
            safeCenter += new Vector3(offset.x, 0, offset.y);

            int phase = GetShrinkPhase();
            Debug.Log($"[吃鸡V2] 第{phase + 1}次缩圈 - {safeRadius:F0} -> {nextShrinkRadius:F0}");
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

        // ===== 圈外伤害 =====
        private void UpdateZoneDamage()
        {
            foreach (var p in players)
            {
                var hp = p.GetComponent<Core.HealthSystem>();
                if (hp != null && !hp.IsDead && !IsInSafeZone(p.transform.position))
                {
                    float dmg = ZoneDamage * (1 + GetShrinkPhase() * 0.5f);
                    hp.TakeDamage(dmg * Time.deltaTime, false);
                }
            }
        }

        // ===== 武器刷新 =====
        private void UpdateWeaponSpawns()
        {
            weaponTimer += Time.deltaTime;
            if (weaponTimer >= WeaponSpawnInterval)
            {
                weaponTimer = 0;
                SpawnWeaponDrop();
            }
        }

        private void SpawnWeaponDrop()
        {
            Vector3 pos = GetRandomSpawnInZone();
            weaponDrops.Add(pos);

            // RPG模式：随机武器类型 + 品质
            int weaponType = Random.Range(0, Core.RPGWeaponPickup.GetWeaponCount());
            var quality = (Core.WeaponQuality)Random.Range(0, 4);
            Core.RPGWeaponPickup.SpawnGroundWeapon(pos, weaponType, quality);
        }

        // ===== 野怪系统 =====
        private void InitMonsterCamps()
        {
            monsterCamps.Clear();
            for (int i = 0; i < WildMonsterCampCount; i++)
            {
                var camp = new WildMonsterCamp
                {
                    position = GetRandomSpawnInZone(),
                    type = (WildMonsterCamp.CampType)Random.Range(0, 3),
                    monsters = new List<GameObject>(),
                    isAlive = true,
                    respawnTimer = 0
                };
                SpawnCampMonsters(camp);
                monsterCamps.Add(camp);
            }
        }

        private void SpawnCampMonsters(WildMonsterCamp camp)
        {
            int count;
            float baseHp;
            float scale;

            switch (camp.type)
            {
                case WildMonsterCamp.CampType.Normal:
                    count = 3; baseHp = 200; scale = 0.8f;
                    break;
                case WildMonsterCamp.CampType.Elite:
                    count = 2; baseHp = 500; scale = 1.2f;
                    break;
                case WildMonsterCamp.CampType.Boss:
                    count = 1; baseHp = 1500; scale = 2f;
                    break;
                default:
                    count = 3; baseHp = 200; scale = 0.8f;
                    break;
            }

            float hp = baseHp * (1 + (monsterLevel - 1) * 0.2f);
            camp.monsters.Clear();

            for (int i = 0; i < count; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
                var monster = CreateWildMonster(camp.position + offset, hp, scale, camp.type);
                camp.monsters.Add(monster);
            }

            Debug.Log($"[吃鸡V2] 野怪营地生成 - {camp.type} x{count} HP:{hp:F0}");
        }

        private GameObject CreateWildMonster(Vector3 pos, float hp, float scale, WildMonsterCamp.CampType type)
        {
            var go = GameObject.CreatePrimitive(type == WildMonsterCamp.CampType.Boss
                ? PrimitiveType.Capsule : PrimitiveType.Cube);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            go.name = $"WildMonster_L{monsterLevel}_{type}";
            go.tag = "Enemy";

            var health = go.AddComponent<Core.HealthSystem>();
            health.MaxHp = hp;
            health.CurrentHp = hp;

            var ai = go.AddComponent<AI.EnemyAI>();
            ai.AttackDamage = 10 + monsterLevel * 5;
            ai.DetectRange = 10f;
            ai.MoveSpeed = type == WildMonsterCamp.CampType.Elite ? 3.5f : 2.5f;

            var renderer = go.GetComponent<Renderer>();
            switch (type)
            {
                case WildMonsterCamp.CampType.Elite:
                    renderer.material.color = new Color(0.8f, 0.3f, 0.1f);
                    break;
                case WildMonsterCamp.CampType.Boss:
                    renderer.material.color = new Color(0.6f, 0f, 0.8f);
                    break;
                default:
                    renderer.material.color = new Color(0.3f, 0.6f, 0.3f);
                    break;
            }

            // 死亡回调 - 掉宝
            var campPos = pos;
            var campType = type;
            health.OnDeath += () => OnWildMonsterDeath(go, campPos, campType);

            return go;
        }

        private void OnWildMonsterDeath(GameObject monster, Vector3 campPos, WildMonsterCamp.CampType type)
        {
            // 从营地移除
            foreach (var camp in monsterCamps)
            {
                if (camp.monsters.Contains(monster))
                {
                    camp.monsters.Remove(monster);
                    if (camp.monsters.Count == 0)
                    {
                        camp.isAlive = false;
                        camp.respawnTimer = MonsterRespawnDelay;
                        Debug.Log($"[吃鸡V2] 营地清除，{MonsterRespawnDelay}秒后重生");
                    }
                    break;
                }
            }

            // 掉落宝箱/金币
            DropLoot(monster.transform.position, type);

            Object.Destroy(monster);
        }

        private void DropLoot(Vector3 pos, WildMonsterCamp.CampType type)
        {
            // 金币掉落
            int goldAmount = type switch
            {
                WildMonsterCamp.CampType.Normal => Random.Range(10, 30),
                WildMonsterCamp.CampType.Elite => Random.Range(30, 80),
                WildMonsterCamp.CampType.Boss => Random.Range(80, 200),
                _ => 10
            };

            // 创建金币掉落物
            var goldObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            goldObj.transform.position = pos + Vector3.up * 0.5f;
            goldObj.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
            goldObj.name = $"Gold_{goldAmount}";
            goldObj.tag = "Pickup";
            goldObj.GetComponent<Renderer>().material.color = new Color(1f, 0.84f, 0f);
            var col = goldObj.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            var pickup = goldObj.AddComponent<GoldPickup>();
            pickup.goldAmount = goldAmount;

            // 精英/Boss额外掉宝箱
            if (type == WildMonsterCamp.CampType.Elite || type == WildMonsterCamp.CampType.Boss)
            {
                SpawnTreasureChest(pos + new Vector3(2f, 0, 0), type == WildMonsterCamp.CampType.Boss);
            }
        }

        private void UpdateMonsterCamps()
        {
            // 野怪升级
            int newLevel = 1 + (int)(GameTime / MonsterLevelUpInterval);
            if (newLevel > monsterLevel)
            {
                monsterLevel = newLevel;
                Debug.Log($"[吃鸡V2] 野怪等级提升至 {monsterLevel}");
            }

            // 野怪重生
            foreach (var camp in monsterCamps)
            {
                if (!camp.isAlive && camp.respawnTimer > 0)
                {
                    camp.respawnTimer -= Time.deltaTime;
                    if (camp.respawnTimer <= 0)
                    {
                        SpawnCampMonsters(camp);
                    }
                }
            }
        }

        // ===== 宝箱系统 =====
        private void SpawnTreasureChest(Vector3 pos, bool isLegendary)
        {
            var chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chest.transform.position = pos + Vector3.up * 0.5f;
            chest.transform.localScale = isLegendary ? Vector3.one * 1.2f : Vector3.one * 0.8f;
            chest.name = isLegendary ? "LegendaryChest" : "TreasureChest";
            chest.tag = "Pickup";
            chest.GetComponent<Renderer>().material.color = isLegendary
                ? new Color(1f, 0.6f, 0f)  // 金色
                : new Color(0.6f, 0.4f, 0.2f); // 棕色

            var col = chest.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            var chestPickup = chest.AddComponent<TreasureChestPickup>();
            chestPickup.isLegendary = isLegendary;
        }

        // ===== Boss系统 =====
        private void UpdateBoss()
        {
            if (!bossSpawned)
            {
                bossTimer += Time.deltaTime;
                if (bossTimer >= BossSpawnTime)
                {
                    bossSpawned = true;
                    SpawnMapBoss();
                }
            }
            else if (currentBoss == null)
            {
                // Boss已死，等待重生
                bossTimer += Time.deltaTime;
                if (bossTimer >= BossRespawnTime)
                {
                    SpawnMapBoss();
                    bossTimer = 0;
                    Debug.Log("[吃鸡V2] Boss重生!");
                }
            }
        }

        private void SpawnMapBoss()
        {
            // Boss刷新在安全区中心附近
            Vector3 bossPos = safeCenter + new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));

            currentBoss = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            currentBoss.transform.position = bossPos;
            currentBoss.transform.localScale = Vector3.one * 3f;
            currentBoss.name = "BattleRoyaleBoss";
            currentBoss.tag = "Enemy";

            var health = currentBoss.AddComponent<Core.HealthSystem>();
            health.MaxHp = 3000 + monsterLevel * 500;
            health.CurrentHp = health.MaxHp;

            var ai = currentBoss.AddComponent<AI.BossAI>();
            ai.BossType = 2; // 混合型
            ai.AttackDamage = 50 + monsterLevel * 10;
            ai.MaxHP = health.MaxHp;

            var renderer = currentBoss.GetComponent<Renderer>();
            renderer.material.color = new Color(0.9f, 0.1f, 0.1f);

            // Boss死亡掉落高级宝箱
            health.OnDeath += () =>
            {
                DropLoot(bossPos, WildMonsterCamp.CampType.Boss);
                SpawnTreasureChest(bossPos + new Vector3(3, 0, 0), true);
                SpawnTreasureChest(bossPos + new Vector3(-3, 0, 0), true);
                Debug.Log("[吃鸡V2] Boss被击杀! 掉落传说宝箱!");
                currentBoss = null;
            };

            Debug.Log($"[吃鸡V2] Boss刷新在 {bossPos} HP:{health.MaxHp}");
        }

        // ===== 遗迹系统 =====
        private void InitRuins()
        {
            ruins.Clear();
            for (int i = 0; i < RuinCount; i++)
            {
                var ruin = new RuinSite
                {
                    position = GetRandomSpawnInZone(),
                    ruinName = GetRuinName(i),
                    isExplored = false,
                    isBeingExplored = false,
                    exploreProgress = 0f
                };

                CreateRuinStructure(ruin);
                ruins.Add(ruin);
            }
        }

        private string GetRuinName(int index)
        {
            string[] names = { "远古神殿", "废弃矿洞", "沉没宝库", "破败王陵", "遗忘祭坛" };
            return index < names.Length ? names[index] : $"遗迹{index}";
        }

        private void CreateRuinStructure(RuinSite ruin)
        {
            // 遗迹外观 - 废墟建筑群
            var ruinRoot = new GameObject($"Ruin_{ruin.ruinName}");
            ruinRoot.transform.position = ruin.position;

            // 主建筑
            var main = GameObject.CreatePrimitive(PrimitiveType.Cube);
            main.transform.parent = ruinRoot.transform;
            main.transform.localPosition = Vector3.up * 2.5f;
            main.transform.localScale = new Vector3(6, 5, 6);
            main.GetComponent<Renderer>().material.color = new Color(0.45f, 0.4f, 0.35f);
            main.name = "RuinMain";
            main.isStatic = true;

            // 残破墙壁
            for (int w = 0; w < 3; w++)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.parent = ruinRoot.transform;
                float angle = w * 120f * Mathf.Deg2Rad;
                wall.transform.localPosition = new Vector3(Mathf.Cos(angle) * 4f, 1.5f, Mathf.Sin(angle) * 4f);
                wall.transform.localScale = new Vector3(3, Random.Range(1.5f, 3f), 0.5f);
                wall.transform.rotation = Quaternion.Euler(0, w * 120f, 0);
                wall.GetComponent<Renderer>().material.color = new Color(0.5f, 0.45f, 0.35f);
                wall.name = $"RuinWall_{w}";
                wall.isStatic = true;
            }

            // 入口标记（发光柱子）
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.transform.parent = ruinRoot.transform;
            marker.transform.localPosition = new Vector3(0, 2f, 4f);
            marker.transform.localScale = new Vector3(0.5f, 2f, 0.5f);
            marker.GetComponent<Renderer>().material.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            marker.name = "RuinMarker";

            // 探索触发区
            var trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trigger.transform.parent = ruinRoot.transform;
            trigger.transform.localPosition = Vector3.up * 0.5f;
            trigger.transform.localScale = new Vector3(5, 1, 5);
            trigger.name = "RuinTrigger";
            trigger.GetComponent<Renderer>().enabled = false; // 不可见
            var col = trigger.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            var exploreZone = trigger.AddComponent<RuinExploreZone>();
            exploreZone.ruin = ruin;
            exploreZone.exploreTime = RuinExploreTime;

            ruin.rootObject = ruinRoot;
        }

        // ===== NPC系统 =====
        private void SpawnNPCs()
        {
            npcObjects.Clear();

            // 商人NPC
            for (int i = 0; i < MerchantCount; i++)
            {
                var npc = CreateNPC($"商人_{i + 1}", NPCType.Merchant, GetRandomSpawnInZone());
                npcObjects.Add(npc);
            }

            // 任务NPC
            for (int i = 0; i < QuestGiverCount; i++)
            {
                var npc = CreateNPC($"任务使者_{i + 1}", NPCType.QuestGiver, GetRandomSpawnInZone());
                npcObjects.Add(npc);
            }
        }

        private GameObject CreateNPC(string name, NPCType npcType, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = pos;
            go.name = name;
            go.tag = "NPC";
            go.transform.localScale = Vector3.one * 1.2f;

            var renderer = go.GetComponent<Renderer>();
            switch (npcType)
            {
                case NPCType.Merchant:
                    renderer.material.color = new Color(0.2f, 0.6f, 0.8f); // 蓝色 - 商人
                    break;
                case NPCType.QuestGiver:
                    renderer.material.color = new Color(0.8f, 0.7f, 0.1f); // 金色 - 任务
                    break;
            }

            var npcInteract = go.AddComponent<NPCInteraction>();
            npcInteract.npcType = npcType;
            npcInteract.npcName = name;

            // NPC上方标记
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.transform.parent = go.transform;
            marker.transform.localPosition = Vector3.up * 2f;
            marker.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            marker.GetComponent<Renderer>().material.color = npcType == NPCType.Merchant
                ? new Color(0, 1, 1) : new Color(1, 1, 0);
            marker.name = "NPCMarker";

            return go;
        }

        // ===== 存活检测 =====
        private void CheckAlive()
        {
            // 按队伍统计存活
            foreach (var team in teams)
            {
                int teamAlive = 0;
                foreach (var member in team.members)
                {
                    var hp = member.GetComponent<Core.HealthSystem>();
                    if (hp != null && !hp.IsDead) teamAlive++;
                }
                team.aliveCount = teamAlive;
            }

            // 玩家队伍被淘汰时记录排名
            var playerTeam = teams.FirstOrDefault(t => t.members.Any(m => m.IsLocalPlayer));
            if (playerTeam != null && playerTeam.aliveCount == 0 && playerRank <= 0)
            {
                int eliminatedTeams = teams.Count(t => t.aliveCount == 0);
                playerRank = TotalPlayers / TeamSize - eliminatedTeams + 1;
                Debug.Log($"[吃鸡V2] 玩家队伍淘汰 - 排名 #{playerRank}");
            }

            aliveCount = teams.Sum(t => t.aliveCount);

            // 最后一队存活 → 胜利
            int survivingTeams = teams.Count(t => t.aliveCount > 0);
            if (survivingTeams <= 1)
            {
                EndMode();
            }
        }

        // ===== 死亡处理 =====
        public override void OnPlayerDeath(PlayerController player, PlayerController killer)
        {
            if (killer != null)
            {
                // 击杀回血
                var killerHP = killer.GetComponent<Core.HealthSystem>();
                if (killerHP != null) killerHP.Heal(killerHP.MaxHp * 0.15f);

                // 记录队伍击杀
                var killerTeam = GetPlayerTeam(killer);
                if (killerTeam != null) killerTeam.totalKills++;

                // 击杀金币奖励
                int killGold = 50;
                AddGold(killer, killGold);
            }
        }

        // ===== 金币系统 =====
        public void AddGold(PlayerController player, int amount)
        {
            int id = player.GetInstanceID();
            if (!playerGold.ContainsKey(id)) playerGold[id] = 0;
            playerGold[id] += amount;
        }

        public bool SpendGold(PlayerController player, int amount)
        {
            int id = player.GetInstanceID();
            if (!playerGold.ContainsKey(id) || playerGold[id] < amount) return false;
            playerGold[id] -= amount;
            return true;
        }

        public int GetGold(PlayerController player)
        {
            int id = player.GetInstanceID();
            return playerGold.ContainsKey(id) ? playerGold[id] : 0;
        }

        // ===== 工具方法 =====
        private void SpawnBots()
        {
            for (int i = 0; i < BotCount; i++)
            {
                // Bot加入已有队伍
                int teamIdx = (players.Count + i) / TeamSize;
                if (teamIdx >= teams.Count) teamIdx = teams.Count - 1;

                Vector3 pos = teams.Count > 0 && teams[teamIdx].members.Count > 0
                    ? teams[teamIdx].members[0].transform.position + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f))
                    : GetRandomSpawnInZone();

                var bot = CreateBot(pos, $"Bot_{i + 1}");
                var botCtrl = bot.GetComponent<PlayerController>();
                players.Add(botCtrl);

                if (teamIdx < teams.Count)
                {
                    teams[teamIdx].members.Add(botCtrl);
                    playerGold[bot.GetInstanceID()] = 0;
                }
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

            var renderer = go.GetComponent<Renderer>();
            renderer.material.color = new Color(Random.value, Random.value, Random.value);

            return go;
        }

        private Vector3 GetRandomSpawnInZone()
        {
            Vector2 circle = Random.insideUnitCircle * safeRadius * 0.9f;
            return new Vector3(safeCenter.x + circle.x, 0, safeCenter.z + circle.y);
        }

        // ===== 胜负判断 =====
        public override bool CheckGameOver(out string winner)
        {
            winner = "";
            var survivingTeam = teams.FirstOrDefault(t => t.aliveCount > 0);
            if (survivingTeam != null && teams.Count(t => t.aliveCount > 0) <= 1)
            {
                winner = survivingTeam.teamName;
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
        public int GetMonsterLevel() => monsterLevel;
        public List<Team> GetTeams() => teams;
        public List<RuinSite> GetRuins() => ruins;
        public bool IsBossAlive() => currentBoss != null;
    }

    // =============================================
    //  数据结构
    // =============================================

    /// <summary>队伍</summary>
    [System.Serializable]
    public class Team
    {
        public int teamId;
        public string teamName;
        public List<PlayerController> members;
        public int totalKills;
        public int aliveCount;

        public int GetAliveCount()
        {
            return members.Count(m =>
            {
                var hp = m.GetComponent<Core.HealthSystem>();
                return hp != null && !hp.IsDead;
            });
        }
    }

    /// <summary>野怪营地</summary>
    [System.Serializable]
    public class WildMonsterCamp
    {
        public enum CampType { Normal, Elite, Boss }
        public Vector3 position;
        public CampType type;
        public List<GameObject> monsters;
        public bool isAlive;
        public float respawnTimer;
    }

    /// <summary>遗迹</summary>
    [System.Serializable]
    public class RuinSite
    {
        public Vector3 position;
        public string ruinName;
        public bool isExplored;
        public bool isBeingExplored;
        public float exploreProgress;
        public GameObject rootObject;
    }

    /// <summary>NPC类型</summary>
    public enum NPCType
    {
        Merchant,    // 商人 - 用金币购买武器/药水
        QuestGiver   // 任务发布者 - 完成任务获得奖励
    }
}
