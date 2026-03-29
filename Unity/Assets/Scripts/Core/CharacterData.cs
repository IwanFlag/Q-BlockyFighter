using System;
using System.Collections.Generic;
using UnityEngine;

namespace QBlockyFighter.Core
{
    /// <summary>
    /// Character power system types with rock-paper-scissors relationships.
    /// </summary>
    public enum PowerSystem
    {
        Wushu,   // 武术 -> beats Magic (close range interrupts casting)
        Wushu2,  // 巫术 -> beats Xianshu (curse weakens divine)
        Magic,   // 魔法 -> beats Wushu2 (ranged purification)
        Xianshu  // 仙术 -> beats Wushu (shield tanks melee)
    }

    public enum CharacterRole
    {
        Assassin,   // 刺客
        Warrior,    // 战士
        Tank,       // 坦克
        Ranger,     // 远程
        Support     // 辅助
    }

    public enum CharacterFaction
    {
        Dawn,   // 破晓
        Dusk    // 黄昏
    }

    public enum WeaponQuality
    {
        Black,  // 黑铁
        Bronze, // 青铜
        Silver, // 白银
        Gold    // 黄金
    }

    [Serializable]
    public class WeaponData
    {
        public string name;
        public string icon;
        public float range;
        public float attackAngle;
        public string type;

        public WeaponData(string name, string icon, float range, float attackAngle, string type)
        {
            this.name = name;
            this.icon = icon;
            this.range = range;
            this.attackAngle = attackAngle;
            this.type = type;
        }
    }

    [Serializable]
    public class SkillData
    {
        public string name;
        public string icon;
        public float cooldown;
        public float range;
        public float damage;
        public string type;
        public float stunDuration;
        public float dotDamage;
        public float dotDuration;
        public int hits;
        public float healAmount;
        public float duration;
        public float width;
        public float radius;
        public bool hasSuperArmor;
        public float atkBuff;
        public float defBuff;
        public float slowMult;
        public float slowDuration;
        public float pullDist;
        public float fearDuration;
        public bool requiresDot;
        public float turretDuration;
        public float scoutDuration;

        public SkillData(string name, string icon, float cooldown, float range, float damage, string type)
        {
            this.name = name;
            this.icon = icon;
            this.cooldown = cooldown;
            this.range = range;
            this.damage = damage;
            this.type = type;
        }
    }

    [Serializable]
    public class PassiveData
    {
        public string name;
        public string icon;
        public string description;

        public PassiveData(string name, string icon, string desc)
        {
            this.name = name;
            this.icon = icon;
            this.description = desc;
        }
    }

    [Serializable]
    public class CharacterData
    {
        public string id;
        public string name;
        public string title;
        public Color color;
        public Color emissive;
        public string portrait;
        public PowerSystem powerSystem;
        public string powerSystemName;
        public CharacterRole role;
        public CharacterFaction faction;
        public string bodyColorHex;
        public string accentColorHex;
        public WeaponData[] weapons;
        public SkillData skillQ;
        public SkillData skillE;
        public SkillData skillR;
        public PassiveData passive;
        public int[] lightDamage;   // [hit1, hit2, hit3, hit4]
        public int[] heavyDamage;   // [hit1, hit2, hit3]
        public int hpRegen;
        public string style;
        public int baseHp;
        public float baseSpeed;

        /// <summary>
        /// Returns elemental advantage multiplier against target's power system.
        /// </summary>
        public float GetAdvantageMultiplier(PowerSystem targetSystem)
        {
            // Wushu > Magic > Wushu2 > Xianshu > Wushu
            if (powerSystem == PowerSystem.Wushu && targetSystem == PowerSystem.Magic) return 1.15f;
            if (powerSystem == PowerSystem.Magic && targetSystem == PowerSystem.Wushu2) return 1.15f;
            if (powerSystem == PowerSystem.Wushu2 && targetSystem == PowerSystem.Xianshu) return 1.15f;
            if (powerSystem == PowerSystem.Xianshu && targetSystem == PowerSystem.Wushu) return 1.15f;
            return 1.0f;
        }
    }

    /// <summary>
    /// Static database of all 12 characters.
    /// </summary>
    public static class CharacterDatabase
    {
        private static List<CharacterData> _characters;

        public static List<CharacterData> Characters
        {
            get
            {
                if (_characters == null) InitCharacters();
                return _characters;
            }
        }

        public static CharacterData GetCharacter(int index)
        {
            if (_characters == null) InitCharacters();
            return index >= 0 && index < _characters.Count ? _characters[index] : _characters[0];
        }

        public static CharacterData FindById(string id)
        {
            if (_characters == null) InitCharacters();
            return _characters.Find(c => c.id == id) ?? _characters[0];
        }

        private static void InitCharacters()
        {
            _characters = new List<CharacterData>
            {
                // 1. 冷月·荆如 - 刺客·武术·破晓
                new CharacterData
                {
                    id = "jingru", name = "冷月·荆如", title = "刺客·武术",
                    color = new Color(0.13f, 0.27f, 0.67f), emissive = new Color(0.07f, 0.13f, 1f),
                    portrait = "🗡️", powerSystem = PowerSystem.Wushu, powerSystemName = "武术",
                    role = CharacterRole.Assassin, faction = CharacterFaction.Dawn,
                    bodyColorHex = "#2244aa", accentColorHex = "#4488ff",
                    weapons = new[]
                    {
                        new WeaponData("鱼肠短剑", "🗡️", 3f, Mathf.PI / 3, "sword"),
                        new WeaponData("软剑", "🪢", 4.5f, Mathf.PI / 4, "whip")
                    },
                    skillQ = new SkillData("剑气突刺", "💨", 8, 6, 120, "dash"),
                    skillE = new SkillData("缠绕", "🪢", 12, 8, 60, "stun") { stunDuration = 1.0f },
                    skillR = new SkillData("月影斩", "🌀", 30, 10, 250, "aoe"),
                    passive = new PassiveData("剑意", "⚔️", "弹反成功后下一击伤害+50%"),
                    lightDamage = new[] { 30, 35, 40, 55 }, heavyDamage = new[] { 80, 90, 100 },
                    hpRegen = 20, style = "balanced", baseHp = 1000, baseSpeed = 6f
                },
                // 2. 影忍·雾隐 - 刺客·巫术·黄昏
                new CharacterData
                {
                    id = "wuyin", name = "影忍·雾隐", title = "刺客·巫术",
                    color = new Color(0.33f, 0.13f, 0.53f), emissive = new Color(0.67f, 0, 1f),
                    portrait = "🥷", powerSystem = PowerSystem.Wushu2, powerSystemName = "巫术",
                    role = CharacterRole.Assassin, faction = CharacterFaction.Dusk,
                    bodyColorHex = "#442266", accentColorHex = "#aa44ff",
                    weapons = new[]
                    {
                        new WeaponData("苦无", "🔪", 2.5f, Mathf.PI / 4, "dagger"),
                        new WeaponData("锁镰", "⛓️", 5f, Mathf.PI / 6, "chain")
                    },
                    skillQ = new SkillData("影遁", "👻", 10, 0, 0, "stealth") { duration = 2f },
                    skillE = new SkillData("毒雾", "☠️", 14, 6, 40, "dot") { dotDamage = 10, dotDuration = 5 },
                    skillR = new SkillData("暗影千刃", "🔪", 28, 8, 200, "multi") { hits = 5 },
                    passive = new PassiveData("影毒", "☠️", "攻击附带中毒效果，每秒掉血"),
                    lightDamage = new[] { 25, 28, 32, 45 }, heavyDamage = new[] { 65, 75, 85 },
                    hpRegen = 10, style = "assassin", baseHp = 900, baseSpeed = 7f
                },
                // 3. 傲骨·关云 - 战士·武术·破晓
                new CharacterData
                {
                    id = "guanyun", name = "傲骨·关云", title = "战士·武术",
                    color = new Color(0.13f, 0.53f, 0.13f), emissive = new Color(0.27f, 1f, 0.27f),
                    portrait = "⚔️", powerSystem = PowerSystem.Wushu, powerSystemName = "武术",
                    role = CharacterRole.Warrior, faction = CharacterFaction.Dawn,
                    bodyColorHex = "#1a6622", accentColorHex = "#44cc44",
                    weapons = new[]
                    {
                        new WeaponData("青龙偃月刀", "🔪", 4f, Mathf.PI / 2, "halberd"),
                        new WeaponData("短刀", "🗡️", 2.5f, Mathf.PI / 3, "knife")
                    },
                    skillQ = new SkillData("龙斩", "🐉", 8, 6, 150, "line") { width = 2 },
                    skillE = new SkillData("武圣气场", "💪", 15, 5, 80, "buff") { atkBuff = 1.3f, duration = 5 },
                    skillR = new SkillData("青龙怒", "🐲", 35, 12, 350, "aoe") { hasSuperArmor = true },
                    passive = new PassiveData("武圣", "👊", "重攻击自带霸体，不被打断"),
                    lightDamage = new[] { 35, 40, 48, 65 }, heavyDamage = new[] { 100, 120, 140 },
                    hpRegen = 20, style = "berserker", baseHp = 1200, baseSpeed = 5f
                },
                // 4. 追风·花木兰 - 远程·仙术·破晓
                new CharacterData
                {
                    id = "mulan", name = "追风·花木兰", title = "远程·仙术",
                    color = new Color(0.8f, 0.53f, 0), emissive = new Color(1f, 0.67f, 0),
                    portrait = "🏹", powerSystem = PowerSystem.Xianshu, powerSystemName = "仙术",
                    role = CharacterRole.Ranger, faction = CharacterFaction.Dawn,
                    bodyColorHex = "#886600", accentColorHex = "#ffcc44",
                    weapons = new[]
                    {
                        new WeaponData("长弓", "🏹", 12f, Mathf.PI / 8, "bow"),
                        new WeaponData("袖箭", "🪃", 5f, Mathf.PI / 4, "dart")
                    },
                    skillQ = new SkillData("天眼", "👁️", 10, 15, 100, "snipe"),
                    skillE = new SkillData("仙阵", "✨", 12, 8, 50, "trap") { slowDuration = 3 },
                    skillR = new SkillData("箭雨", "🌧️", 25, 12, 180, "rain") { hits = 8 },
                    passive = new PassiveData("追风", "💨", "远程攻击有几率减速敌人"),
                    lightDamage = new[] { 20, 24, 28, 35 }, heavyDamage = new[] { 60, 70, 90 },
                    hpRegen = 20, style = "ranger", baseHp = 850, baseSpeed = 6.5f
                },
                // 5. 毒蝎·哈桑 - 远程·巫术·黄昏
                new CharacterData
                {
                    id = "hasang", name = "毒蝎·哈桑", title = "远程·巫术",
                    color = new Color(0.27f, 0.53f, 0), emissive = new Color(0.53f, 1f, 0),
                    portrait = "🦂", powerSystem = PowerSystem.Wushu2, powerSystemName = "巫术",
                    role = CharacterRole.Ranger, faction = CharacterFaction.Dusk,
                    bodyColorHex = "#334400", accentColorHex = "#aaff00",
                    weapons = new[]
                    {
                        new WeaponData("飞刀", "🔪", 10f, Mathf.PI / 6, "throw"),
                        new WeaponData("毒镖", "☠️", 8f, Mathf.PI / 5, "poison_dart")
                    },
                    skillQ = new SkillData("蛊毒", "🦂", 8, 10, 60, "dot") { dotDamage = 15, dotDuration = 6 },
                    skillE = new SkillData("暗影步", "👻", 12, 8, 40, "blink"),
                    skillR = new SkillData("蛊爆", "💥", 28, 8, 250, "aoe") { requiresDot = true },
                    passive = new PassiveData("剧毒", "☠️", "攻击附带中毒，叠层后触发蛊爆伤害+100%"),
                    lightDamage = new[] { 22, 26, 30, 40 }, heavyDamage = new[] { 55, 65, 80 },
                    hpRegen = 10, style = "ranger", baseHp = 850, baseSpeed = 6.5f
                },
                // 6. 磐石·奥拉夫 - 坦克·仙术·破晓
                new CharacterData
                {
                    id = "aolafu", name = "磐石·奥拉夫", title = "坦克·仙术",
                    color = new Color(0.53f, 0.4f, 0.27f), emissive = new Color(1f, 0.8f, 0),
                    portrait = "🪓", powerSystem = PowerSystem.Xianshu, powerSystemName = "仙术",
                    role = CharacterRole.Tank, faction = CharacterFaction.Dawn,
                    bodyColorHex = "#664422", accentColorHex = "#ffaa00",
                    weapons = new[]
                    {
                        new WeaponData("双手战斧", "🪓", 4.5f, Mathf.PI * 0.6f, "axe"),
                        new WeaponData("飞斧", "🎯", 8f, Mathf.PI / 6, "thrown_axe")
                    },
                    skillQ = new SkillData("狂战冲锋", "💪", 8, 8, 100, "charge") { hasSuperArmor = true },
                    skillE = new SkillData("战嚎", "🗣️", 14, 6, 60, "fear") { fearDuration = 1.5f },
                    skillR = new SkillData("神降", "⚡", 35, 0, 0, "transform") { atkBuff = 2f, defBuff = 2f, duration = 8 },
                    passive = new PassiveData("不屈", "💪", "血量越低攻击力越高，30%血以下攻击+50%"),
                    lightDamage = new[] { 40, 48, 55, 70 }, heavyDamage = new[] { 110, 130, 160 },
                    hpRegen = 30, style = "tank", baseHp = 1500, baseSpeed = 4.5f
                },
                // 7. 雷鸣·卡利 - 战士·魔法·黄昏
                new CharacterData
                {
                    id = "kali", name = "雷鸣·卡利", title = "战士·魔法",
                    color = new Color(0, 0.4f, 0.8f), emissive = new Color(0, 0.8f, 1f),
                    portrait = "⚡", powerSystem = PowerSystem.Magic, powerSystemName = "魔法",
                    role = CharacterRole.Warrior, faction = CharacterFaction.Dusk,
                    bodyColorHex = "#003366", accentColorHex = "#00aaff",
                    weapons = new[]
                    {
                        new WeaponData("双弯刀·查克拉", "🪢", 3.5f, Mathf.PI * 0.7f, "chakra"),
                        new WeaponData("投掷弯刀", "🪃", 9f, Mathf.PI / 6, "boomerang")
                    },
                    skillQ = new SkillData("雷电附魔", "⚡", 10, 0, 0, "enchant") { duration = 6 },
                    skillE = new SkillData("雷暴旋转", "🌪️", 12, 5, 120, "spin") { hits = 4 },
                    skillR = new SkillData("天雷怒", "🌩️", 30, 10, 300, "aoe") { stunDuration = 1.5f },
                    passive = new PassiveData("雷鸣", "⚡", "旋转攻击时生成电弧，对周围敌人造成额外伤害"),
                    lightDamage = new[] { 32, 38, 44, 58 }, heavyDamage = new[] { 85, 100, 120 },
                    hpRegen = 15, style = "balanced", baseHp = 1050, baseSpeed = 5.5f
                },
                // 8. 铁骑·哲别 - 战士·武术·黄昏
                new CharacterData
                {
                    id = "zhebie", name = "铁骑·哲别", title = "战士·武术",
                    color = new Color(0.53f, 0.27f, 0.13f), emissive = new Color(1f, 0.4f, 0),
                    portrait = "🐎", powerSystem = PowerSystem.Wushu, powerSystemName = "武术",
                    role = CharacterRole.Warrior, faction = CharacterFaction.Dusk,
                    bodyColorHex = "#663311", accentColorHex = "#ff8833",
                    weapons = new[]
                    {
                        new WeaponData("弯刀", "🪓", 3.5f, Mathf.PI / 2, "saber"),
                        new WeaponData("套马索", "🪢", 8f, Mathf.PI / 6, "lasso")
                    },
                    skillQ = new SkillData("冲锋", "🏇", 8, 10, 120, "charge") { hasSuperArmor = true },
                    skillE = new SkillData("套索", "🪢", 12, 8, 50, "hook") { pullDist = 5 },
                    skillR = new SkillData("万马奔腾", "🐎", 30, 12, 280, "line") { width = 3, hits = 3 },
                    passive = new PassiveData("骑术", "🏇", "移动速度+15%，冲锋伤害+20%"),
                    lightDamage = new[] { 30, 35, 42, 55 }, heavyDamage = new[] { 80, 95, 115 },
                    hpRegen = 20, style = "balanced", baseHp = 1050, baseSpeed = 6.9f
                },
                // 9. 铁壁·马克西 - 坦克·武术·黄昏
                new CharacterData
                {
                    id = "makexi", name = "铁壁·马克西", title = "坦克·武术",
                    color = new Color(0.53f, 0.27f, 0), emissive = new Color(1f, 0.67f, 0.27f),
                    portrait = "🛡️", powerSystem = PowerSystem.Wushu, powerSystemName = "武术",
                    role = CharacterRole.Tank, faction = CharacterFaction.Dusk,
                    bodyColorHex = "#663300", accentColorHex = "#ff8800",
                    weapons = new[]
                    {
                        new WeaponData("短剑", "🗡️", 2.5f, Mathf.PI / 3, "gladius"),
                        new WeaponData("方盾", "🛡️", 2f, Mathf.PI, "shield")
                    },
                    skillQ = new SkillData("盾击", "🛡️", 8, 3, 80, "stun") { stunDuration = 1.2f },
                    skillE = new SkillData("盾墙", "🧱", 15, 0, 0, "shield_wall") { defBuff = 3f, duration = 4 },
                    skillR = new SkillData("罗马阵列", "⚔️", 30, 8, 200, "aoe") { defBuff = 2f, duration = 6 },
                    passive = new PassiveData("角斗士", "⚔️", "格挡成功后反击伤害+30%，完美格挡触发气劲反击"),
                    lightDamage = new[] { 28, 33, 38, 50 }, heavyDamage = new[] { 70, 85, 100 },
                    hpRegen = 30, style = "tank", baseHp = 1500, baseSpeed = 4.5f
                },
                // 10. 破军·李舜臣 - 远程·魔法·破晓
                new CharacterData
                {
                    id = "lishunchen", name = "破军·李舜臣", title = "远程·魔法",
                    color = new Color(0.13f, 0.27f, 0.53f), emissive = new Color(0.27f, 0.53f, 1f),
                    portrait = "🔫", powerSystem = PowerSystem.Magic, powerSystemName = "魔法",
                    role = CharacterRole.Ranger, faction = CharacterFaction.Dawn,
                    bodyColorHex = "#113355", accentColorHex = "#3366cc",
                    weapons = new[]
                    {
                        new WeaponData("火铳", "🔫", 12f, Mathf.PI / 8, "musket"),
                        new WeaponData("弩炮", "💣", 10f, Mathf.PI / 6, "cannon")
                    },
                    skillQ = new SkillData("架设炮台", "🧱", 10, 8, 60, "turret") { turretDuration = 10 },
                    skillE = new SkillData("炮火覆盖", "💥", 14, 10, 150, "aoe") { radius = 4 },
                    skillR = new SkillData("龟船炮阵", "🚢", 35, 12, 350, "aoe") { radius = 6, hits = 5 },
                    passive = new PassiveData("水军统帅", "⚓", "炮台攻击附带减速，多个炮台同时攻击伤害+25%"),
                    lightDamage = new[] { 22, 26, 32, 42 }, heavyDamage = new[] { 65, 78, 95 },
                    hpRegen = 15, style = "ranger", baseHp = 850, baseSpeed = 6f
                },
                // 11. 圣歌·阿玛尼 - 辅助·巫术·破晓
                new CharacterData
                {
                    id = "amani", name = "圣歌·阿玛尼", title = "辅助·巫术",
                    color = new Color(0.53f, 0.4f, 0.27f), emissive = new Color(1f, 0.8f, 0.53f),
                    portrait = "🎵", powerSystem = PowerSystem.Wushu2, powerSystemName = "巫术",
                    role = CharacterRole.Support, faction = CharacterFaction.Dawn,
                    bodyColorHex = "#554422", accentColorHex = "#cc9955",
                    weapons = new[]
                    {
                        new WeaponData("图腾法杖", "🎵", 6f, Mathf.PI / 4, "totem"),
                        new WeaponData("骨铃", "🔔", 8f, Mathf.PI / 6, "bell")
                    },
                    skillQ = new SkillData("祖灵祝福", "🙏", 10, 8, 0, "heal") { healAmount = 200 },
                    skillE = new SkillData("诅咒之铃", "🔔", 12, 8, 60, "debuff") { slowMult = 0.5f, duration = 3 },
                    skillR = new SkillData("祖灵降临", "✨", 35, 10, 0, "resurrect") { healAmount = 500, atkBuff = 1.5f, duration = 8 },
                    passive = new PassiveData("祖灵守护", "✨", "附近队友每秒回复1%最大生命值，自身回复翻倍"),
                    lightDamage = new[] { 18, 22, 26, 35 }, heavyDamage = new[] { 50, 60, 75 },
                    hpRegen = 30, style = "support", baseHp = 950, baseSpeed = 5.5f
                },
                // 12. 天机·达芬奇 - 辅助·魔法·黄昏
                new CharacterData
                {
                    id = "dafenqi", name = "天机·达芬奇", title = "辅助·魔法",
                    color = new Color(0.4f, 0.27f, 0.53f), emissive = new Color(0.67f, 0.4f, 1f),
                    portrait = "🔧", powerSystem = PowerSystem.Magic, powerSystemName = "魔法",
                    role = CharacterRole.Support, faction = CharacterFaction.Dusk,
                    bodyColorHex = "#442266", accentColorHex = "#8844cc",
                    weapons = new[]
                    {
                        new WeaponData("机关弩", "🏹", 10f, Mathf.PI / 6, "crossbow"),
                        new WeaponData("傀儡", "🪆", 8f, Mathf.PI / 4, "puppet")
                    },
                    skillQ = new SkillData("符文陷阱", "✨", 8, 6, 80, "trap") { stunDuration = 1.5f },
                    skillE = new SkillData("傀儡探路", "🪆", 12, 15, 40, "scout") { scoutDuration = 8 },
                    skillR = new SkillData("符文炮台", "💥", 30, 10, 200, "turret") { turretDuration = 12, hits = 6 },
                    passive = new PassiveData("机关大师", "🔧", "陷阱和炮台冷却-20%，傀儡可探测隐身单位"),
                    lightDamage = new[] { 20, 24, 28, 38 }, heavyDamage = new[] { 55, 68, 85 },
                    hpRegen = 20, style = "support", baseHp = 900, baseSpeed = 5.5f
                }
            };
        }
    }
}
