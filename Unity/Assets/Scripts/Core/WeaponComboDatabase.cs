using System.Collections.Generic;
using UnityEngine;

namespace QBlockyFighter.Core
{
    /// <summary>
    /// 武器连招数据库 - 12种武器的完整连招链定义
    /// 每种武器：轻攻击3-4段 + 重攻击2-3段 + 切换武器特殊技
    /// </summary>
    public static class WeaponComboDatabase
    {
        public struct ComboStep
        {
            public string name;           // 动作名
            public float damageMul;       // 伤害倍率（基于角色攻击力）
            public float duration;        // 动画时长
            public float range;           // 攻击距离
            public float angle;           // 扇形角度
            public float stunDuration;    // 硬直时间
            public float knockback;       // 击退距离
            public bool launches;         // 是否击飞
            public string animTrigger;    // 动画触发器
            public string vfxName;        // 特效名
        }

        // ===== 轻攻击连招 =====

        public static List<ComboStep> GetLightCombo(int weaponType)
        {
            return weaponType switch
            {
                0 => KnifeLight(),         // 匕首
                1 => SwordLight(),         // 剑
                2 => SpearLight(),         // 枪
                3 => GreatswordLight(),    // 大刀
                4 => HammerLight(),        // 锤
                5 => ShieldSwordLight(),   // 盾剑
                6 => BowLight(),           // 弓
                7 => ThrowingLight(),      // 暗器
                8 => StaffLight(),         // 法杖
                9 => WhipLight(),          // 鞭子
                10 => FanLight(),          // 扇子
                11 => WandLight(),         // 魔杖
                _ => KnifeLight()
            };
        }

        // ===== 重攻击 =====

        public static List<ComboStep> GetHeavyCombo(int weaponType)
        {
            return weaponType switch
            {
                0 => KnifeHeavy(),
                1 => SwordHeavy(),
                2 => SpearHeavy(),
                3 => GreatswordHeavy(),
                4 => HammerHeavy(),
                5 => ShieldSwordHeavy(),
                6 => BowHeavy(),
                7 => ThrowingHeavy(),
                8 => StaffHeavy(),
                9 => WhipHeavy(),
                10 => FanHeavy(),
                11 => WandHeavy(),
                _ => KnifeHeavy()
            };
        }

        // ===== 切换武器特殊技 =====

        public static ComboStep GetSwitchAttack(int weaponType)
        {
            return weaponType switch
            {
                0 => new ComboStep { name = "瞬刺", damageMul = 1.2f, duration = 0.2f, range = 4f, angle = 30f, stunDuration = 0.3f, knockback = 1f, launches = false, animTrigger = "switch_stab", vfxName = "vfx_knife_switch" },
                1 => new ComboStep { name = "横扫", damageMul = 1.4f, duration = 0.4f, range = 3f, angle = 180f, stunDuration = 0.5f, knockback = 2f, launches = false, animTrigger = "switch_sweep", vfxName = "vfx_sword_sweep" },
                2 => new ComboStep { name = "挑飞", damageMul = 1.0f, duration = 0.3f, range = 4f, angle = 60f, stunDuration = 0.2f, knockback = 0f, launches = true, animTrigger = "switch_launch", vfxName = "vfx_spear_launch" },
                3 => new ComboStep { name = "劈山", damageMul = 1.8f, duration = 0.6f, range = 3f, angle = 90f, stunDuration = 0.8f, knockback = 4f, launches = true, animTrigger = "switch_slam", vfxName = "vfx_gs_slam" },
                4 => new ComboStep { name = "地震", damageMul = 1.5f, duration = 0.5f, range = 5f, angle = 360f, stunDuration = 1.0f, knockback = 3f, launches = false, animTrigger = "switch_quake", vfxName = "vfx_hammer_quake" },
                5 => new ComboStep { name = "盾击", damageMul = 0.8f, duration = 0.3f, range = 2f, angle = 60f, stunDuration = 1.0f, knockback = 5f, launches = false, animTrigger = "switch_bash", vfxName = "vfx_shield_bash" },
                6 => new ComboStep { name = "穿透箭", damageMul = 1.6f, duration = 0.5f, range = 15f, angle = 10f, stunDuration = 0.3f, knockback = 2f, launches = false, animTrigger = "switch_pierce", vfxName = "vfx_arrow_pierce" },
                7 => new ComboStep { name = "三连飞刀", damageMul = 0.6f, duration = 0.4f, range = 10f, angle = 30f, stunDuration = 0.2f, knockback = 1f, launches = false, animTrigger = "switch_triple", vfxName = "vfx_triple_throw" },
                8 => new ComboStep { name = "冰冻波", damageMul = 1.0f, duration = 0.4f, range = 6f, angle = 120f, stunDuration = 1.5f, knockback = 2f, launches = false, animTrigger = "switch_frost", vfxName = "vfx_frost_wave" },
                9 => new ComboStep { name = "缠绕", damageMul = 0.5f, duration = 0.3f, range = 6f, angle = 60f, stunDuration = 2.0f, knockback = 0f, launches = false, animTrigger = "switch_bind", vfxName = "vfx_whip_bind" },
                10 => new ComboStep { name = "旋风", damageMul = 1.2f, duration = 0.5f, range = 4f, angle = 360f, stunDuration = 0.5f, knockback = 3f, launches = true, animTrigger = "switch_cyclone", vfxName = "vfx_fan_cyclone" },
                11 => new ComboStep { name = "元素爆发", damageMul = 2.0f, duration = 0.6f, range = 5f, angle = 90f, stunDuration = 0.8f, knockback = 4f, launches = true, animTrigger = "switch_burst", vfxName = "vfx_wand_burst" },
                _ => default
            };
        }

        // ===== 具体武器定义 =====

        // 0: 匕首 - 快速连击，伤害低
        private static List<ComboStep> KnifeLight() => new()
        {
            new() { name = "突刺1", damageMul = 0.6f, duration = 0.15f, range = 2.5f, angle = 45f, stunDuration = 0.1f, knockback = 0.5f, animTrigger = "light1", vfxName = "vfx_stab" },
            new() { name = "突刺2", damageMul = 0.7f, duration = 0.15f, range = 2.5f, angle = 45f, stunDuration = 0.1f, knockback = 0.5f, animTrigger = "light2", vfxName = "vfx_stab" },
            new() { name = "突刺3", damageMul = 0.8f, duration = 0.2f, range = 3f, angle = 60f, stunDuration = 0.15f, knockback = 1f, animTrigger = "light3", vfxName = "vfx_stab" },
            new() { name = "终结刺", damageMul = 1.2f, duration = 0.3f, range = 3.5f, angle = 30f, stunDuration = 0.3f, knockback = 2f, launches = true, animTrigger = "light4", vfxName = "vfx_stab_finish" },
        };

        private static List<ComboStep> KnifeHeavy() => new()
        {
            new() { name = "回旋斩", damageMul = 1.5f, duration = 0.4f, range = 3f, angle = 360f, stunDuration = 0.5f, knockback = 2f, animTrigger = "heavy1", vfxName = "vfx_spin" },
            new() { name = "致命一击", damageMul = 2.0f, duration = 0.5f, range = 2.5f, angle = 60f, stunDuration = 0.8f, knockback = 3f, animTrigger = "heavy2", vfxName = "vfx_execute" },
        };

        // 1: 剑 - 平衡型
        private static List<ComboStep> SwordLight() => new()
        {
            new() { name = "左斩", damageMul = 0.8f, duration = 0.2f, range = 3f, angle = 90f, stunDuration = 0.15f, knockback = 1f, animTrigger = "light1", vfxName = "vfx_slash" },
            new() { name = "右斩", damageMul = 0.8f, duration = 0.2f, range = 3f, angle = 90f, stunDuration = 0.15f, knockback = 1f, animTrigger = "light2", vfxName = "vfx_slash" },
            new() { name = "上挑", damageMul = 1.0f, duration = 0.25f, range = 3f, angle = 60f, stunDuration = 0.2f, knockback = 1.5f, animTrigger = "light3", vfxName = "vfx_upper" },
            new() { name = "十字斩", damageMul = 1.5f, duration = 0.35f, range = 3.5f, angle = 120f, stunDuration = 0.4f, knockback = 2f, launches = true, animTrigger = "light4", vfxName = "vfx_cross" },
        };

        private static List<ComboStep> SwordHeavy() => new()
        {
            new() { name = "蓄力斩", damageMul = 1.8f, duration = 0.5f, range = 4f, angle = 90f, stunDuration = 0.6f, knockback = 3f, animTrigger = "heavy1", vfxName = "vfx_charge" },
            new() { name = "居合", damageMul = 2.5f, duration = 0.6f, range = 6f, angle = 30f, stunDuration = 1.0f, knockback = 5f, launches = true, animTrigger = "heavy2", vfxName = "vfx_iai" },
        };

        // 2: 枪 - 突进型
        private static List<ComboStep> SpearLight() => new()
        {
            new() { name = "刺击", damageMul = 0.7f, duration = 0.2f, range = 4f, angle = 30f, stunDuration = 0.1f, knockback = 0.5f, animTrigger = "light1", vfxName = "vfx_thrust" },
            new() { name = "二段刺", damageMul = 0.8f, duration = 0.2f, range = 4f, angle = 30f, stunDuration = 0.1f, knockback = 0.5f, animTrigger = "light2", vfxName = "vfx_thrust" },
            new() { name = "横扫", damageMul = 1.0f, duration = 0.3f, range = 4f, angle = 180f, stunDuration = 0.2f, knockback = 1.5f, animTrigger = "light3", vfxName = "vfx_sweep" },
        };

        private static List<ComboStep> SpearHeavy() => new()
        {
            new() { name = "突进刺", damageMul = 1.5f, duration = 0.4f, range = 8f, angle = 30f, stunDuration = 0.5f, knockback = 4f, animTrigger = "heavy1", vfxName = "vfx_dash_thrust" },
            new() { name = "回马枪", damageMul = 2.0f, duration = 0.5f, range = 5f, angle = 60f, stunDuration = 0.8f, knockback = 3f, launches = true, animTrigger = "heavy2", vfxName = "vfx_return_thrust" },
        };

        // 3: 大刀 - AOE
        private static List<ComboStep> GreatswordLight() => new()
        {
            new() { name = "横劈", damageMul = 1.0f, duration = 0.3f, range = 3.5f, angle = 120f, stunDuration = 0.2f, knockback = 2f, animTrigger = "light1", vfxName = "vfx_chop" },
            new() { name = "竖斩", damageMul = 1.2f, duration = 0.3f, range = 3f, angle = 60f, stunDuration = 0.25f, knockback = 1.5f, animTrigger = "light2", vfxName = "vfx_chop" },
            new() { name = "旋风斩", damageMul = 1.5f, duration = 0.5f, range = 4f, angle = 360f, stunDuration = 0.4f, knockback = 3f, animTrigger = "light3", vfxName = "vfx_spin_chop" },
        };

        private static List<ComboStep> GreatswordHeavy() => new()
        {
            new() { name = "蓄力劈", damageMul = 2.5f, duration = 0.8f, range = 5f, angle = 90f, stunDuration = 1.0f, knockback = 5f, launches = true, animTrigger = "heavy1", vfxName = "vfx_overhead" },
        };

        // 4: 锤 - 控制
        private static List<ComboStep> HammerLight() => new()
        {
            new() { name = "敲击", damageMul = 1.0f, duration = 0.3f, range = 2.5f, angle = 60f, stunDuration = 0.3f, knockback = 2f, animTrigger = "light1", vfxName = "vfx_smash" },
            new() { name = "二连敲", damageMul = 1.2f, duration = 0.35f, range = 2.5f, angle = 60f, stunDuration = 0.4f, knockback = 2.5f, animTrigger = "light2", vfxName = "vfx_smash" },
            new() { name = "旋转锤", damageMul = 1.5f, duration = 0.5f, range = 3f, angle = 360f, stunDuration = 0.6f, knockback = 3f, launches = true, animTrigger = "light3", vfxName = "vfx_spin_smash" },
        };

        private static List<ComboStep> HammerHeavy() => new()
        {
            new() { name = "地裂", damageMul = 2.0f, duration = 0.6f, range = 5f, angle = 360f, stunDuration = 1.5f, knockback = 4f, launches = true, animTrigger = "heavy1", vfxName = "vfx_earthquake" },
        };

        // 5: 盾剑 - 防御反击
        private static List<ComboStep> ShieldSwordLight() => new()
        {
            new() { name = "快刺", damageMul = 0.6f, duration = 0.15f, range = 2.5f, angle = 30f, stunDuration = 0.1f, knockback = 0.5f, animTrigger = "light1", vfxName = "vfx_quick_stab" },
            new() { name = "快刺2", damageMul = 0.6f, duration = 0.15f, range = 2.5f, angle = 30f, stunDuration = 0.1f, knockback = 0.5f, animTrigger = "light2", vfxName = "vfx_quick_stab" },
            new() { name = "盾猛", damageMul = 0.8f, duration = 0.3f, range = 2f, angle = 60f, stunDuration = 0.8f, knockback = 3f, animTrigger = "light3", vfxName = "vfx_shield_smash" },
        };

        private static List<ComboStep> ShieldSwordHeavy() => new()
        {
            new() { name = "盾牌冲锋", damageMul = 1.2f, duration = 0.5f, range = 6f, angle = 45f, stunDuration = 1.0f, knockback = 5f, animTrigger = "heavy1", vfxName = "vfx_shield_charge" },
        };

        // 6: 弓 - 远程
        private static List<ComboStep> BowLight() => new()
        {
            new() { name = "速射", damageMul = 0.7f, duration = 0.2f, range = 15f, angle = 10f, stunDuration = 0.1f, knockback = 0.5f, animTrigger = "light1", vfxName = "vfx_arrow" },
            new() { name = "二连射", damageMul = 0.8f, duration = 0.25f, range = 15f, angle = 10f, stunDuration = 0.1f, knockback = 0.5f, animTrigger = "light2", vfxName = "vfx_arrow" },
            new() { name = "蓄力射", damageMul = 1.2f, duration = 0.4f, range = 20f, angle = 5f, stunDuration = 0.2f, knockback = 1f, animTrigger = "light3", vfxName = "vfx_charged_arrow" },
        };

        private static List<ComboStep> BowHeavy() => new()
        {
            new() { name = "散射", damageMul = 0.8f, duration = 0.4f, range = 10f, angle = 60f, stunDuration = 0.3f, knockback = 1f, animTrigger = "heavy1", vfxName = "vfx_scatter" },
            new() { name = "狙击", damageMul = 2.5f, duration = 0.8f, range = 30f, angle = 5f, stunDuration = 0.5f, knockback = 3f, launches = true, animTrigger = "heavy2", vfxName = "vfx_snipe" },
        };

        // 7: 暗器 - DOT/风筝
        private static List<ComboStep> ThrowingLight() => new()
        {
            new() { name = "飞刀", damageMul = 0.5f, duration = 0.15f, range = 12f, angle = 10f, stunDuration = 0.05f, knockback = 0.3f, animTrigger = "light1", vfxName = "vfx_knife_throw" },
            new() { name = "毒镖", damageMul = 0.4f, duration = 0.2f, range = 12f, angle = 10f, stunDuration = 0.1f, knockback = 0.3f, animTrigger = "light2", vfxName = "vfx_poison_dart" },
            new() { name = "手里剑", damageMul = 0.6f, duration = 0.25f, range = 10f, angle = 30f, stunDuration = 0.1f, knockback = 0.5f, animTrigger = "light3", vfxName = "vfx_shuriken" },
        };

        private static List<ComboStep> ThrowingHeavy() => new()
        {
            new() { name = "毒雾弹", damageMul = 0.3f, duration = 0.4f, range = 8f, angle = 360f, stunDuration = 0f, knockback = 0f, animTrigger = "heavy1", vfxName = "vfx_poison_cloud" },
            new() { name = "烟幕弹", damageMul = 0f, duration = 0.3f, range = 5f, angle = 360f, stunDuration = 0f, knockback = 0f, animTrigger = "heavy2", vfxName = "vfx_smoke" },
        };

        // 8: 法杖 - AOE魔法
        private static List<ComboStep> StaffLight() => new()
        {
            new() { name = "火球", damageMul = 0.9f, duration = 0.3f, range = 10f, angle = 15f, stunDuration = 0.15f, knockback = 1f, animTrigger = "light1", vfxName = "vfx_fireball" },
            new() { name = "冰弹", damageMul = 0.8f, duration = 0.3f, range = 10f, angle = 15f, stunDuration = 0.3f, knockback = 0.5f, animTrigger = "light2", vfxName = "vfx_ice_bolt" },
            new() { name = "闪电", damageMul = 1.0f, duration = 0.35f, range = 8f, angle = 20f, stunDuration = 0.2f, knockback = 0.5f, animTrigger = "light3", vfxName = "vfx_lightning" },
        };

        private static List<ComboStep> StaffHeavy() => new()
        {
            new() { name = "陨石", damageMul = 2.0f, duration = 0.8f, range = 8f, angle = 360f, stunDuration = 1.0f, knockback = 3f, launches = true, animTrigger = "heavy1", vfxName = "vfx_meteor" },
        };

        // 9: 鞭子 - 缠绕/减速
        private static List<ComboStep> WhipLight() => new()
        {
            new() { name = "抽击", damageMul = 0.7f, duration = 0.2f, range = 5f, angle = 60f, stunDuration = 0.1f, knockback = 0.5f, animTrigger = "light1", vfxName = "vfx_whip_crack" },
            new() { name = "连抽", damageMul = 0.8f, duration = 0.25f, range = 5f, angle = 60f, stunDuration = 0.15f, knockback = 0.5f, animTrigger = "light2", vfxName = "vfx_whip_crack" },
            new() { name = "扫击", damageMul = 1.0f, duration = 0.3f, range = 5f, angle = 120f, stunDuration = 0.2f, knockback = 1f, animTrigger = "light3", vfxName = "vfx_whip_sweep" },
        };

        private static List<ComboStep> WhipHeavy() => new()
        {
            new() { name = "缠绕", damageMul = 0.5f, duration = 0.4f, range = 6f, angle = 30f, stunDuration = 2.0f, knockback = 0f, animTrigger = "heavy1", vfxName = "vfx_whip_bind" },
            new() { name = "鞭挞", damageMul = 1.8f, duration = 0.5f, range = 6f, angle = 45f, stunDuration = 0.5f, knockback = 3f, animTrigger = "heavy2", vfxName = "vfx_whip_lash" },
        };

        // 10: 扇子 - 范围控制
        private static List<ComboStep> FanLight() => new()
        {
            new() { name = "扇击", damageMul = 0.8f, duration = 0.2f, range = 3f, angle = 90f, stunDuration = 0.1f, knockback = 1f, animTrigger = "light1", vfxName = "vfx_fan_slash" },
            new() { name = "二连扇", damageMul = 0.9f, duration = 0.25f, range = 3f, angle = 90f, stunDuration = 0.15f, knockback = 1f, animTrigger = "light2", vfxName = "vfx_fan_slash" },
            new() { name = "回旋扇", damageMul = 1.2f, duration = 0.4f, range = 4f, angle = 360f, stunDuration = 0.3f, knockback = 2f, animTrigger = "light3", vfxName = "vfx_fan_spin" },
        };

        private static List<ComboStep> FanHeavy() => new()
        {
            new() { name = "风刃", damageMul = 1.5f, duration = 0.4f, range = 10f, angle = 30f, stunDuration = 0.5f, knockback = 3f, animTrigger = "heavy1", vfxName = "vfx_wind_blade" },
            new() { name = "龙卷", damageMul = 1.8f, duration = 0.6f, range = 5f, angle = 360f, stunDuration = 1.0f, knockback = 4f, launches = true, animTrigger = "heavy2", vfxName = "vfx_tornado" },
        };

        // 11: 魔杖 - Buff/Debuff
        private static List<ComboStep> WandLight() => new()
        {
            new() { name = "魔弹", damageMul = 0.7f, duration = 0.2f, range = 8f, angle = 15f, stunDuration = 0.1f, knockback = 0.5f, animTrigger = "light1", vfxName = "vfx_magic_missile" },
            new() { name = "魔弹2", damageMul = 0.8f, duration = 0.2f, range = 8f, angle = 15f, stunDuration = 0.1f, knockback = 0.5f, animTrigger = "light2", vfxName = "vfx_magic_missile" },
            new() { name = "奥术射线", damageMul = 1.0f, duration = 0.4f, range = 10f, angle = 10f, stunDuration = 0.2f, knockback = 0.5f, animTrigger = "light3", vfxName = "vfx_arcane_beam" },
        };

        private static List<ComboStep> WandHeavy() => new()
        {
            new() { name = "时空减速", damageMul = 0.3f, duration = 0.4f, range = 8f, angle = 360f, stunDuration = 2.0f, knockback = 0f, animTrigger = "heavy1", vfxName = "vfx_time_slow" },
            new() { name = "能量爆发", damageMul = 2.0f, duration = 0.6f, range = 6f, angle = 360f, stunDuration = 0.8f, knockback = 4f, launches = true, animTrigger = "heavy2", vfxName = "vfx_energy_burst" },
        };
    }
}
