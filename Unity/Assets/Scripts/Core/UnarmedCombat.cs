using UnityEngine;

namespace QBlockyFighter.Core
{
    /// <summary>
    /// 徒手战斗系统 - 无武器时的拳脚技能
    /// 捡到任何武器后自动切换为该武器的战斗风格
    /// </summary>
    public static class UnarmedCombat
    {
        /// <summary>
        /// 徒手技能组 - 拳、脚、摔
        /// </summary>
        public static RPGWeaponPickup.WeaponSkillSet GetUnarmedSkillSet()
        {
            return new RPGWeaponPickup.WeaponSkillSet
            {
                weaponName = "徒手",
                icon = "👊",
                weaponType = "unarmed",
                range = 2.5f,
                attackAngle = Mathf.PI / 3,
                lightDamage = new[] { 15, 18, 22, 30 },  // 拳击连段
                heavyDamage = new[] { 40, 50, 60 },       // 腿踢
                skillQ = new SkillData("冲拳", "👊", 5, 3, 60, "dash")
                {
                    stunDuration = 0.3f,
                    hasSuperArmor = false
                },
                skillE = new SkillData("旋风腿", "🦵", 8, 4, 80, "spin")
                {
                    hits = 3,
                    stunDuration = 0.5f
                },
                skillR = new SkillData("过肩摔", "🤸", 20, 3, 120, "launch")
                {
                    launches = true,
                    stunDuration = 1.0f,
                    hasSuperArmor = true
                },
            };
        }

        /// <summary>
        /// 徒手被动 - 被缴械后获得短暂加速（逃生机制）
        /// </summary>
        public static float GetDisarmSpeedBoost()
        {
            return 1.3f; // 30%加速
        }

        /// <summary>
        /// 徒手被动 - 徒手攻击缴械概率加成
        /// </summary>
        public static float GetUnarmedDisarmBonus()
        {
            return 1.5f; // 缴械概率 x1.5
        }
    }
}
