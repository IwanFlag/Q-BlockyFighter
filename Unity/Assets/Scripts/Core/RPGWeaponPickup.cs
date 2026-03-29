using System;
using System.Collections.Generic;
using UnityEngine;

namespace QBlockyFighter.Core
{
    /// <summary>
    /// RPG武器拾取系统 - 吃鸡模式专用
    /// 捡起武器后：替换武器连招 + 替换Q/E/R技能
    /// 被缴械后：回到徒手战斗
    /// </summary>
    public class RPGWeaponPickup : MonoBehaviour
    {
        public static RPGWeaponPickup Instance { get; private set; }

        [Header("拾取配置")]
        public float pickupRange = 3f;
        public float dropSpreadRadius = 5f;

        // 玩家当前装备的武器索引（-1 = 徒手）
        private Dictionary<int, int> playerWeaponIndex = new Dictionary<int, int>();
        // 玩家原始角色数据备份（用于恢复徒手技能）
        private Dictionary<int, CharacterData> originalCharData = new Dictionary<int, CharacterData>();

        // 12种武器对应的技能组
        private static readonly WeaponSkillSet[] WeaponSkills = InitWeaponSkills();

        void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// 捡起武器 - 替换武器连招和Q/E/R技能
        /// </summary>
        public bool PickupWeapon(GameObject playerObj, int weaponTypeIndex, WeaponQuality quality)
        {
            var player = playerObj.GetComponent<PlayerController>();
            var weaponSys = playerObj.GetComponent<WeaponSystem>();
            var skillSys = playerObj.GetComponent<SkillSystem>();
            var charData = GetCharacterData(playerObj);

            if (player == null || weaponSys == null || skillSys == null || charData == null) return false;

            int pid = playerObj.GetInstanceID();

            // 备份原始数据
            if (!originalCharData.ContainsKey(pid))
            {
                originalCharData[pid] = DeepCopyCharData(charData);
            }

            // 设置武器品质
            weaponSys.Quality = quality;

            // 替换角色的武器数据（连招表跟随武器变化）
            var weaponSkill = GetWeaponSkillSet(weaponTypeIndex);
            charData.weapons = new WeaponData[]
            {
                new WeaponData(weaponSkill.weaponName, weaponSkill.icon,
                    weaponSkill.range, weaponSkill.attackAngle, weaponSkill.weaponType)
            };
            charData.lightDamage = weaponSkill.lightDamage;
            charData.heavyDamage = weaponSkill.heavyDamage;

            // 替换Q/E/R技能
            charData.skillQ = DeepCopySkill(weaponSkill.skillQ);
            charData.skillE = DeepCopySkill(weaponSkill.skillE);
            charData.skillR = DeepCopySkill(weaponSkill.skillR);

            // 重置技能CD
            skillSys.ResetCooldowns();

            // 记录当前武器
            playerWeaponIndex[pid] = weaponTypeIndex;

            Debug.Log($"[RPG武器] {playerObj.name} 捡起 {weaponSkill.weaponName} ({quality}) - 技能已替换");
            return true;
        }

        /// <summary>
        /// 被缴械 - 丢弃武器，回到徒手战斗
        /// </summary>
        public void DisarmPlayer(GameObject playerObj)
        {
            var player = playerObj.GetComponent<PlayerController>();
            var weaponSys = playerObj.GetComponent<WeaponSystem>();
            var skillSys = playerObj.GetComponent<SkillSystem>();
            var charData = GetCharacterData(playerObj);

            if (player == null || charData == null) return;

            int pid = playerObj.GetInstanceID();
            int oldWeapon = playerWeaponIndex.ContainsKey(pid) ? playerWeaponIndex[pid] : -1;

            // 生成掉落的武器
            if (oldWeapon >= 0)
            {
                SpawnDroppedWeapon(playerObj.transform.position, oldWeapon,
                    weaponSys != null ? weaponSys.Quality : WeaponQuality.Black);
            }

            // 切换为徒手
            var unarmed = UnarmedCombat.GetUnarmedSkillSet();
            charData.weapons = new WeaponData[]
            {
                new WeaponData(unarmed.weaponName, unarmed.icon,
                    unarmed.range, unarmed.attackAngle, unarmed.weaponType)
            };
            charData.lightDamage = unarmed.lightDamage;
            charData.heavyDamage = unarmed.heavyDamage;
            charData.skillQ = DeepCopySkill(unarmed.skillQ);
            charData.skillE = DeepCopySkill(unarmed.skillE);
            charData.skillR = DeepCopySkill(unarmed.skillR);

            // 重置品质
            if (weaponSys != null) weaponSys.ResetQuality();

            // 重置技能CD
            if (skillSys != null) skillSys.ResetCooldowns();

            playerWeaponIndex[pid] = -1;

            Debug.Log($"[RPG缴械] {playerObj.name} 武器被打落! 切换为徒手战斗");
        }

        /// <summary>
        /// 生成掉落的武器（可被其他人捡起）
        /// </summary>
        private void SpawnDroppedWeapon(Vector3 pos, int weaponTypeIndex, WeaponQuality quality)
        {
            var weaponSkill = GetWeaponSkillSet(weaponTypeIndex);

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = pos + Vector3.up * 0.5f;
            go.transform.localScale = new Vector3(0.6f, 0.15f, 0.6f);
            go.name = $"WeaponDrop_{weaponSkill.weaponName}";
            go.tag = "WeaponDrop";

            var renderer = go.GetComponent<Renderer>();
            renderer.material.color = GetQualityColor(quality);

            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            var pickup = go.AddComponent<RPGWeaponPickupItem>();
            pickup.weaponTypeIndex = weaponTypeIndex;
            pickup.quality = quality;
            pickup.weaponName = weaponSkill.weaponName;
        }

        /// <summary>
        /// 生成地面上的武器（地图刷新用）
        /// </summary>
        public static void SpawnGroundWeapon(Vector3 pos, int weaponTypeIndex, WeaponQuality quality)
        {
            if (Instance == null) return;
            Instance.SpawnDroppedWeapon(pos, weaponTypeIndex, quality);
        }

        /// <summary>
        /// 获取玩家当前武器类型（-1 = 徒手）
        /// </summary>
        public int GetCurrentWeapon(GameObject playerObj)
        {
            int pid = playerObj.GetInstanceID();
            return playerWeaponIndex.ContainsKey(pid) ? playerWeaponIndex[pid] : -1;
        }

        /// <summary>
        /// 初始化玩家为徒手状态
        /// </summary>
        public void InitPlayerUnarmed(GameObject playerObj)
        {
            var charData = GetCharacterData(playerObj);
            if (charData == null) return;

            int pid = playerObj.GetInstanceID();
            if (!originalCharData.ContainsKey(pid))
            {
                originalCharData[pid] = DeepCopyCharData(charData);
            }

            // 设置为徒手
            var unarmed = UnarmedCombat.GetUnarmedSkillSet();
            charData.weapons = new WeaponData[]
            {
                new WeaponData(unarmed.weaponName, unarmed.icon,
                    unarmed.range, unarmed.attackAngle, unarmed.weaponType)
            };
            charData.lightDamage = unarmed.lightDamage;
            charData.heavyDamage = unarmed.heavyDamage;
            charData.skillQ = DeepCopySkill(unarmed.skillQ);
            charData.skillE = DeepCopySkill(unarmed.skillE);
            charData.skillR = DeepCopySkill(unarmed.skillR);

            playerWeaponIndex[pid] = -1;
        }

        #region 武器技能映射表

        public struct WeaponSkillSet
        {
            public string weaponName;
            public string icon;
            public string weaponType;
            public float range;
            public float attackAngle;
            public int[] lightDamage;
            public int[] heavyDamage;
            public SkillData skillQ;
            public SkillData skillE;
            public SkillData skillR;
        }

        private static WeaponSkillSet[] InitWeaponSkills()
        {
            return new WeaponSkillSet[]
            {
                // 0: 剑 - 平衡型
                new WeaponSkillSet
                {
                    weaponName = "剑", icon = "🗡️", weaponType = "sword",
                    range = 3f, attackAngle = Mathf.PI / 3,
                    lightDamage = new[] { 30, 35, 40, 55 },
                    heavyDamage = new[] { 80, 90, 100 },
                    skillQ = new SkillData("剑气突刺", "💨", 8, 6, 120, "dash"),
                    skillE = new SkillData("十字斩", "⚔️", 10, 4, 100, "line") { width = 2 },
                    skillR = new SkillData("月影斩", "🌀", 30, 10, 250, "aoe"),
                },
                // 1: 鞭 - 缠绕控制
                new WeaponSkillSet
                {
                    weaponName = "鞭", icon = "🪢", weaponType = "whip",
                    range = 5f, attackAngle = Mathf.PI / 4,
                    lightDamage = new[] { 25, 28, 32, 45 },
                    heavyDamage = new[] { 65, 75, 85 },
                    skillQ = new SkillData("缠绕", "🪢", 10, 8, 60, "stun") { stunDuration = 1.5f },
                    skillE = new SkillData("鞭挞", "💥", 12, 6, 90, "line") { width = 2 },
                    skillR = new SkillData("千鞭缠", "🕸️", 28, 10, 200, "aoe") { stunDuration = 2f },
                },
                // 2: 匕首 - 高速连击
                new WeaponSkillSet
                {
                    weaponName = "匕首", icon = "🔪", weaponType = "dagger",
                    range = 2.5f, attackAngle = Mathf.PI / 4,
                    lightDamage = new[] { 22, 26, 30, 42 },
                    heavyDamage = new[] { 60, 70, 80 },
                    skillQ = new SkillData("影步", "👻", 8, 8, 50, "blink"),
                    skillE = new SkillData("毒刃", "☠️", 12, 3, 40, "dot") { dotDamage = 10, dotDuration = 5 },
                    skillR = new SkillData("暗影千刃", "🔪", 28, 8, 200, "multi") { hits = 5 },
                },
                // 3: 锁链 - 拉扯控制
                new WeaponSkillSet
                {
                    weaponName = "锁链", icon = "⛓️", weaponType = "chain",
                    range = 5f, attackAngle = Mathf.PI / 6,
                    lightDamage = new[] { 28, 32, 36, 48 },
                    heavyDamage = new[] { 70, 80, 95 },
                    skillQ = new SkillData("锁链拉扯", "⛓️", 10, 10, 70, "hook") { pullDist = 6 },
                    skillE = new SkillData("链刃旋", "🌀", 12, 5, 110, "spin") { hits = 4 },
                    skillR = new SkillData("天罗地网", "🕸️", 30, 10, 180, "aoe") { stunDuration = 2f },
                },
                // 4: 长戟 - 大范围
                new WeaponSkillSet
                {
                    weaponName = "长戟", icon = "🔱", weaponType = "halberd",
                    range = 4f, attackAngle = Mathf.PI / 2,
                    lightDamage = new[] { 35, 40, 48, 62 },
                    heavyDamage = new[] { 100, 120, 140 },
                    skillQ = new SkillData("龙斩", "🐉", 8, 6, 150, "line") { width = 2 },
                    skillE = new SkillData("挑飞", "⬆️", 12, 4, 80, "launch") { launches = true },
                    skillR = new SkillData("青龙怒", "🐲", 35, 12, 350, "aoe") { hasSuperArmor = true },
                },
                // 5: 长弓 - 远程
                new WeaponSkillSet
                {
                    weaponName = "长弓", icon = "🏹", weaponType = "bow",
                    range = 12f, attackAngle = Mathf.PI / 8,
                    lightDamage = new[] { 20, 24, 28, 35 },
                    heavyDamage = new[] { 60, 70, 90 },
                    skillQ = new SkillData("天眼", "👁️", 10, 15, 100, "snipe"),
                    skillE = new SkillData("陷阱箭", "🪤", 12, 8, 50, "trap") { stunDuration = 1.5f },
                    skillR = new SkillData("箭雨", "🌧️", 25, 12, 180, "rain") { hits = 8 },
                },
                // 6: 飞刀 - 远程暗器
                new WeaponSkillSet
                {
                    weaponName = "飞刀", icon = "🔪", weaponType = "throw",
                    range = 10f, attackAngle = Mathf.PI / 6,
                    lightDamage = new[] { 18, 22, 26, 35 },
                    heavyDamage = new[] { 50, 60, 75 },
                    skillQ = new SkillData("三连飞刀", "🔪", 8, 12, 80, "multi") { hits = 3 },
                    skillE = new SkillData("毒镖", "☠️", 12, 10, 40, "dot") { dotDamage = 12, dotDuration = 6 },
                    skillR = new SkillData("万刃归宗", "⚔️", 30, 10, 220, "aoe") { hits = 6 },
                },
                // 7: 战斧 - 重击型
                new WeaponSkillSet
                {
                    weaponName = "战斧", icon = "🪓", weaponType = "axe",
                    range = 4.5f, attackAngle = Mathf.PI * 0.6f,
                    lightDamage = new[] { 40, 48, 55, 70 },
                    heavyDamage = new[] { 110, 130, 160 },
                    skillQ = new SkillData("狂战冲锋", "💪", 8, 8, 120, "charge") { hasSuperArmor = true },
                    skillE = new SkillData("战嚎", "🗣️", 14, 6, 60, "fear") { stunDuration = 1.5f },
                    skillR = new SkillData("劈山裂地", "⚡", 30, 8, 300, "aoe") { hasSuperArmor = true, launches = true },
                },
                // 8: 弯刀 - 快速斩击
                new WeaponSkillSet
                {
                    weaponName = "弯刀", icon = "⚔️", weaponType = "saber",
                    range = 3.5f, attackAngle = Mathf.PI / 2,
                    lightDamage = new[] { 32, 38, 44, 58 },
                    heavyDamage = new[] { 85, 100, 120 },
                    skillQ = new SkillData("雷电附魔", "⚡", 10, 0, 0, "enchant") { duration = 6 },
                    skillE = new SkillData("雷暴旋转", "🌪️", 12, 5, 120, "spin") { hits = 4 },
                    skillR = new SkillData("天雷怒", "🌩️", 30, 10, 300, "aoe") { stunDuration = 1.5f },
                },
                // 9: 盾牌 - 防御型
                new WeaponSkillSet
                {
                    weaponName = "盾牌", icon = "🛡️", weaponType = "shield",
                    range = 2f, attackAngle = Mathf.PI,
                    lightDamage = new[] { 20, 24, 28, 38 },
                    heavyDamage = new[] { 55, 65, 80 },
                    skillQ = new SkillData("盾击", "🛡️", 8, 3, 80, "stun") { stunDuration = 1.2f },
                    skillE = new SkillData("盾墙", "🧱", 15, 0, 0, "shield_wall") { defBuff = 3f, duration = 4 },
                    skillR = new SkillData("罗马阵列", "⚔️", 30, 8, 200, "aoe") { defBuff = 2f, duration = 6 },
                },
                // 10: 火铳 - 远程射击
                new WeaponSkillSet
                {
                    weaponName = "火铳", icon = "🔫", weaponType = "musket",
                    range = 12f, attackAngle = Mathf.PI / 8,
                    lightDamage = new[] { 22, 26, 32, 42 },
                    heavyDamage = new[] { 65, 78, 95 },
                    skillQ = new SkillData("架设炮台", "🧱", 10, 8, 60, "turret") { turretDuration = 10 },
                    skillE = new SkillData("炮火覆盖", "💥", 14, 10, 150, "aoe") { radius = 4 },
                    skillR = new SkillData("火炮齐射", "🚢", 35, 12, 350, "aoe") { radius = 6, hits = 5 },
                },
                // 11: 图腾 - 辅助型
                new WeaponSkillSet
                {
                    weaponName = "图腾", icon = "🎵", weaponType = "totem",
                    range = 6f, attackAngle = Mathf.PI / 4,
                    lightDamage = new[] { 18, 22, 26, 35 },
                    heavyDamage = new[] { 50, 60, 75 },
                    skillQ = new SkillData("祖灵祝福", "🙏", 10, 8, 0, "heal") { healAmount = 200 },
                    skillE = new SkillData("诅咒之铃", "🔔", 12, 8, 60, "debuff") { slowMult = 0.5f, duration = 3 },
                    skillR = new SkillData("祖灵降临", "✨", 35, 10, 0, "resurrect") { healAmount = 500, atkBuff = 1.5f, duration = 8 },
                },
            };
        }

        public static WeaponSkillSet GetWeaponSkillSet(int index)
        {
            if (WeaponSkills == null || index < 0 || index >= WeaponSkills.Length) return WeaponSkills[0];
            return WeaponSkills[index];
        }

        public static int GetWeaponCount() => WeaponSkills?.Length ?? 0;

        #endregion

        #region 工具方法

        private CharacterData GetCharacterData(GameObject obj)
        {
            // 尝试从多个可能的位置获取CharacterData
            // 实际项目中CharacterData可能由GameManager或PlayerController持有
            var gm = FindObjectOfType<GameManager>();
            if (gm != null)
            {
                // 通过GameManager获取
                var field = typeof(GameManager).GetField("characterData",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) return field.GetValue(gm) as CharacterData;
            }
            return null;
        }

        private CharacterData DeepCopyCharData(CharacterData src)
        {
            if (src == null) return null;
            return new CharacterData
            {
                id = src.id,
                name = src.name,
                title = src.title,
                color = src.color,
                powerSystem = src.powerSystem,
                role = src.role,
                faction = src.faction,
                weapons = (WeaponData[])src.weapons?.Clone(),
                skillQ = DeepCopySkill(src.skillQ),
                skillE = DeepCopySkill(src.skillE),
                skillR = DeepCopySkill(src.skillR),
                passive = src.passive,
                lightDamage = (int[])src.lightDamage?.Clone(),
                heavyDamage = (int[])src.heavyDamage?.Clone(),
                hpRegen = src.hpRegen,
                style = src.style,
                baseHp = src.baseHp,
                baseSpeed = src.baseSpeed
            };
        }

        private SkillData DeepCopySkill(SkillData src)
        {
            if (src == null) return null;
            return new SkillData(src.name, src.icon, src.cooldown, src.range, src.damage, src.type)
            {
                stunDuration = src.stunDuration,
                dotDamage = src.dotDamage,
                dotDuration = src.dotDuration,
                hits = src.hits,
                healAmount = src.healAmount,
                duration = src.duration,
                width = src.width,
                radius = src.radius,
                hasSuperArmor = src.hasSuperArmor,
                atkBuff = src.atkBuff,
                defBuff = src.defBuff,
                slowMult = src.slowMult,
                slowDuration = src.slowDuration,
                pullDist = src.pullDist,
                fearDuration = src.fearDuration,
                requiresDot = src.requiresDot,
                turretDuration = src.turretDuration,
                scoutDuration = src.scoutDuration
            };
        }

        private Color GetQualityColor(WeaponQuality quality)
        {
            return quality switch
            {
                WeaponQuality.Black => new Color(0.53f, 0.53f, 0.53f),
                WeaponQuality.Bronze => new Color(0.27f, 0.8f, 0.27f),
                WeaponQuality.Silver => new Color(0.4f, 0.53f, 1f),
                WeaponQuality.Gold => new Color(1f, 0.84f, 0),
                _ => Color.gray
            };
        }

        #endregion
    }

    // ===== 地面武器拾取组件 =====
    public class RPGWeaponPickupItem : MonoBehaviour
    {
        public int weaponTypeIndex;
        public WeaponQuality quality;
        public string weaponName;

        private float rotateSpeed = 120f;
        private float bobSpeed = 2f;
        private float bobHeight = 0.3f;
        private Vector3 startPos;

        void Start()
        {
            startPos = transform.position;
            // 根据武器类型调整外观
            transform.localScale = GetWeaponScale();
        }

        void Update()
        {
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            var pickup = RPGWeaponPickup.Instance;
            if (pickup == null) return;

            if (pickup.PickupWeapon(other.gameObject, weaponTypeIndex, quality))
            {
                Destroy(gameObject);
            }
        }

        private Vector3 GetWeaponScale()
        {
            return weaponTypeIndex switch
            {
                0 => new Vector3(0.8f, 0.1f, 0.15f),   // 剑
                1 => new Vector3(0.5f, 0.08f, 0.8f),    // 鞭
                2 => new Vector3(0.5f, 0.08f, 0.12f),   // 匕首
                3 => new Vector3(0.3f, 0.08f, 0.7f),    // 锁链
                4 => new Vector3(0.2f, 0.2f, 1.0f),     // 长戟
                5 => new Vector3(0.7f, 0.08f, 0.15f),   // 长弓
                6 => new Vector3(0.3f, 0.05f, 0.08f),   // 飞刀
                7 => new Vector3(0.6f, 0.15f, 0.2f),    // 战斧
                8 => new Vector3(0.7f, 0.08f, 0.12f),   // 弯刀
                9 => new Vector3(0.6f, 0.6f, 0.1f),     // 盾牌
                10 => new Vector3(0.15f, 0.15f, 0.9f),  // 火铳
                11 => new Vector3(0.2f, 0.8f, 0.2f),    // 图腾
                _ => Vector3.one * 0.5f
            };
        }
    }
}
