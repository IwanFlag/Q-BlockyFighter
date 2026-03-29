using System;
using System.Collections.Generic;
using UnityEngine;

namespace QBlockyFighter.Core
{
    /// <summary>
    /// 缴械系统 - 连续命中敌人达到一定次数后，有概率打落对方武器
    /// 
    /// 规则：
    /// - 每次攻击命中计数+1（2秒内未命中则重置）
    /// - 连击3次起有缴械概率，连击越高概率越高
    /// - 品质越高的武器越难被打落
    /// - 徒手攻击缴械概率有加成
    /// - 被缴械者获得短暂加速buff逃生
    /// </summary>
    public class DisarmSystem : MonoBehaviour
    {
        [Header("缴械配置")]
        [Tooltip("连击计数重置时间（秒）")]
        public float comboResetTime = 2.0f;

        [Tooltip("缴械起始连击数")]
        public int minHitsForDisarm = 3;

        [Tooltip("基础缴械概率（每次额外连击）")]
        public float baseDisarmChance = 0.08f;

        [Tooltip("最大缴械概率")]
        public float maxDisarmChance = 0.45f;

        [Tooltip("被缴械后加速buff持续时间")]
        public float disarmedSpeedBuffDuration = 2.0f;

        [Tooltip("被缴械后加速倍率")]
        public float disarmedSpeedBuffMultiplier = 1.3f;

        [Tooltip("缴械冷却时间（防止连续缴械）")]
        public float disarmCooldown = 5.0f;

        // ===== 连击追踪 =====
        // key: 被攻击者ID, value: 连击数据
        private Dictionary<int, HitTracker> hitTrackers = new Dictionary<int, HitTracker>();
        // key: 被攻击者ID, value: 上次被缴械时间
        private Dictionary<int, float> lastDisarmTime = new Dictionary<int, float>();

        // Events
        public event Action<GameObject, GameObject> OnDisarm; // attacker, victim

        private class HitTracker
        {
            public int consecutiveHits;
            public float lastHitTime;
            public int lastAttackerId;
        }

        void Update()
        {
            // 清理过期的连击记录
            var expired = new List<int>();
            foreach (var kvp in hitTrackers)
            {
                if (Time.time - kvp.Value.lastHitTime > comboResetTime)
                {
                    expired.Add(kvp.Key);
                }
            }
            foreach (var id in expired)
            {
                hitTrackers.Remove(id);
            }
        }

        /// <summary>
        /// 记录一次命中 - 在攻击命中敌人时调用
        /// </summary>
        /// <param name="attacker">攻击者</param>
        /// <param name="victim">被击中者</param>
        /// <returns>是否触发缴械</returns>
        public bool RegisterHit(GameObject attacker, GameObject victim)
        {
            if (attacker == null || victim == null) return false;

            int victimId = victim.GetInstanceID();
            int attackerId = attacker.GetInstanceID();

            // 更新连击计数
            if (!hitTrackers.ContainsKey(victimId))
            {
                hitTrackers[victimId] = new HitTracker();
            }

            var tracker = hitTrackers[victimId];

            if (tracker.lastAttackerId == attackerId &&
                Time.time - tracker.lastHitTime < comboResetTime)
            {
                // 同一攻击者继续连击
                tracker.consecutiveHits++;
            }
            else
            {
                // 新连击开始
                tracker.consecutiveHits = 1;
                tracker.lastAttackerId = attackerId;
            }

            tracker.lastHitTime = Time.time;

            // 检查缴械
            if (tracker.consecutiveHits >= minHitsForDisarm)
            {
                return TryDisarm(attacker, victim, tracker.consecutiveHits);
            }

            return false;
        }

        /// <summary>
        /// 尝试缴械
        /// </summary>
        private bool TryDisarm(GameObject attacker, GameObject victim, int hitCount)
        {
            int victimId = victim.GetInstanceID();

            // 检查缴械冷却
            if (lastDisarmTime.ContainsKey(victimId) &&
                Time.time - lastDisarmTime[victimId] < disarmCooldown)
            {
                return false;
            }

            // 计算缴械概率
            float chance = CalculateDisarmChance(attacker, victim, hitCount);

            // 概率判定
            if (UnityEngine.Random.value <= chance)
            {
                ExecuteDisarm(attacker, victim);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 计算缴械概率
        /// </summary>
        private float CalculateDisarmChance(GameObject attacker, GameObject victim, int hitCount)
        {
            // 基础概率 = (连击数 - 起始数 + 1) * 每次概率
            float chance = (hitCount - minHitsForDisarm + 1) * baseDisarmChance;

            // 攻击者武器品质加成（品质越高缴械越容易）
            var attackerWeapon = attacker.GetComponent<WeaponSystem>();
            if (attackerWeapon != null)
            {
                float qualityBonus = attackerWeapon.Quality switch
                {
                    WeaponQuality.Gold => 0.08f,
                    WeaponQuality.Silver => 0.04f,
                    WeaponQuality.Bronze => 0.02f,
                    _ => 0f
                };
                chance += qualityBonus;
            }

            // 受害者武器品质抗性（品质越高越难被打落）
            var victimWeapon = victim.GetComponent<WeaponSystem>();
            if (victimWeapon != null)
            {
                float qualityResist = victimWeapon.Quality switch
                {
                    WeaponQuality.Gold => -0.12f,
                    WeaponQuality.Silver => -0.08f,
                    WeaponQuality.Bronze => -0.04f,
                    _ => 0f
                };
                chance += qualityResist;
            }

            // 攻击者是否徒手 - 徒手有缴械加成
            var attackerRPG = RPGWeaponPickup.Instance;
            if (attackerRPG != null && attackerRPG.GetCurrentWeapon(attacker) == -1)
            {
                chance *= UnarmedCombat.GetUnarmedDisarmBonus();
            }

            // 受害者是否正在格挡 - 格挡中缴械概率降低
            var victimCombat = victim.GetComponent<CombatSystem>();
            if (victimCombat != null && victimCombat.IsBlocking)
            {
                chance *= 0.5f;
            }

            // 受害者是否在霸体中 - 霸体免疫缴械
            if (victimCombat != null && victimCombat.HasSuperArmor)
            {
                chance = 0f;
            }

            // 受害者是否徒手 - 徒手不会被打落武器
            if (attackerRPG != null && attackerRPG.GetCurrentWeapon(victim) == -1)
            {
                chance = 0f;
            }

            return Mathf.Clamp(chance, 0f, maxDisarmChance);
        }

        /// <summary>
        /// 执行缴械
        /// </summary>
        private void ExecuteDisarm(GameObject attacker, GameObject victim)
        {
            int victimId = victim.GetInstanceID();

            // 记录缴械时间
            lastDisarmTime[victimId] = Time.time;

            // 重置连击计数
            if (hitTrackers.ContainsKey(victimId))
            {
                hitTrackers[victimId].consecutiveHits = 0;
            }

            // 执行缴械 - 武器掉落
            var rpgPickup = RPGWeaponPickup.Instance;
            if (rpgPickup != null)
            {
                rpgPickup.DisarmPlayer(victim);
            }

            // 给被缴械者加速buff（逃生机制）
            ApplyDisarmSpeedBuff(victim);

            // 给攻击者金币奖励
            var brMode = FindObjectOfType<Modes.BattleRoyaleMode>();
            if (brMode != null)
            {
                var attackerCtrl = attacker.GetComponent<PlayerController>();
                if (attackerCtrl != null)
                {
                    brMode.AddGold(attackerCtrl, 30);
                }
            }

            OnDisarm?.Invoke(attacker, victim);
            Debug.Log($"[缴械] {attacker.name} 缴械了 {victim.name}! (加速buff {disarmedSpeedBuffDuration}秒)");
        }

        /// <summary>
        /// 给被缴械者短暂加速buff
        /// </summary>
        private void ApplyDisarmSpeedBuff(GameObject victim)
        {
            var buff = victim.GetComponent<DisarmSpeedBuff>();
            if (buff == null)
            {
                buff = victim.AddComponent<DisarmSpeedBuff>();
            }
            buff.Activate(disarmedSpeedBuffDuration, disarmedSpeedBuffMultiplier);
        }

        /// <summary>
        /// 获取目标当前被连击数
        /// </summary>
        public int GetConsecutiveHits(GameObject target)
        {
            int targetId = target.GetInstanceID();
            return hitTrackers.ContainsKey(targetId) ? hitTrackers[targetId].consecutiveHits : 0;
        }

        /// <summary>
        /// 获取缴械概率（UI显示用）
        /// </summary>
        public float GetDisarmChancePreview(GameObject attacker, GameObject victim)
        {
            int victimId = victim.GetInstanceID();
            if (!hitTrackers.ContainsKey(victimId)) return 0f;
            int hits = hitTrackers[victimId].consecutiveHits;
            if (hits < minHitsForDisarm) return 0f;
            return CalculateDisarmChance(attacker, victim, hits);
        }

        /// <summary>
        /// 重置所有数据
        /// </summary>
        public void ResetAll()
        {
            hitTrackers.Clear();
            lastDisarmTime.Clear();
        }
    }

    /// <summary>
    /// 被缴械后的加速buff
    /// </summary>
    public class DisarmSpeedBuff : MonoBehaviour
    {
        private float duration;
        private float multiplier;
        private float timer;
        private bool isActive;
        private float originalSpeed;
        private PlayerController playerCtrl;

        public void Activate(float dur, float mult)
        {
            duration = dur;
            multiplier = mult;
            timer = duration;
            isActive = true;

            playerCtrl = GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                originalSpeed = playerCtrl.CurrentSpeed;
            }
        }

        void Update()
        {
            if (!isActive) return;

            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                isActive = false;
                // 速度会在PlayerController的Update中自然恢复
                Debug.Log($"[缴械Buff] {name} 加速buff结束");
            }
        }

        public bool IsActive => isActive;
        public float SpeedMultiplier => isActive ? multiplier : 1f;
    }
}
