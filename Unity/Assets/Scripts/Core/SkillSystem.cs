using System;
using System.Collections.Generic;
using UnityEngine;

namespace QBlockyFighter.Core
{
    /// <summary>
    /// Skill system for Q/E/R/Passive abilities with cooldown management.
    /// </summary>
    public class SkillSystem : MonoBehaviour
    {
        [Header("Skill Cooldowns")]
        public float qCooldownRemaining;
        public float eCooldownRemaining;
        public float rCooldownRemaining;

        // State
        public bool IsCastingSkill { get; private set; }
        public float CastTimer { get; private set; }
        public SkillData CurrentCastSkill { get; private set; }
        public bool IsStealthed { get; private set; }
        public float StealthTimer { get; private set; }
        public bool HasEnchantBuff { get; private set; }
        public float EnchantTimer { get; private set; }
        public string EnchantElement { get; private set; }
        public bool HasTransformBuff { get; private set; }
        public float TransformTimer { get; private set; }
        public float AtkBuffMult { get; private set; } = 1f;
        public float DefBuffMult { get; private set; } = 1f;

        private CharacterData _charData;
        private HealthSystem _health;
        private CombatSystem _combat;

        // DOT tracking
        private readonly Dictionary<int, DotInfo> _activeDots = new();

        // Events
        public event Action<string> OnSkillCast; // skill name
        public event Action<string> OnSkillHit;  // skill name
        public event Action<string> OnPassiveTrigger;

        private struct DotInfo
        {
            public float damage;
            public float remaining;
            public int sourcePlayerId;
        }

        public void Initialize(CharacterData data)
        {
            _charData = data;
            _health = GetComponent<HealthSystem>();
            _combat = GetComponent<CombatSystem>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Cooldowns
            if (qCooldownRemaining > 0) qCooldownRemaining -= dt;
            if (eCooldownRemaining > 0) eCooldownRemaining -= dt;
            if (rCooldownRemaining > 0) rCooldownRemaining -= dt;

            // Cast timer
            if (IsCastingSkill)
            {
                CastTimer -= dt;
                if (CastTimer <= 0)
                {
                    IsCastingSkill = false;
                }
            }

            // Stealth
            if (IsStealthed)
            {
                StealthTimer -= dt;
                if (StealthTimer <= 0)
                {
                    IsStealthed = false;
                }
            }

            // Enchant buff
            if (HasEnchantBuff)
            {
                EnchantTimer -= dt;
                if (EnchantTimer <= 0)
                {
                    HasEnchantBuff = false;
                    EnchantElement = null;
                }
            }

            // Transform buff
            if (HasTransformBuff)
            {
                TransformTimer -= dt;
                if (TransformTimer <= 0)
                {
                    HasTransformBuff = false;
                    AtkBuffMult = 1f;
                    DefBuffMult = 1f;
                }
            }

            // Tick DOTs
            TickDots(dt);
        }

        #region Skill Usage

        public bool TryUseQ()
        {
            if (_charData == null) return false;
            return TryCastSkill(_charData.skillQ, ref qCooldownRemaining);
        }

        public bool TryUseE()
        {
            if (_charData == null) return false;
            return TryCastSkill(_charData.skillE, ref eCooldownRemaining);
        }

        public bool TryUseR()
        {
            if (_charData == null) return false;
            return TryCastSkill(_charData.skillR, ref rCooldownRemaining);
        }

        private bool TryCastSkill(SkillData skill, ref float cooldown)
        {
            if (skill == null) return false;
            if (cooldown > 0) return false;
            if (IsCastingSkill) return false;

            var combat = GetComponent<CombatSystem>();
            if (combat != null && !combat.CanAct()) return false;

            // Stamina cost for skills
            if (_health != null && !_health.CanUseStamina(20)) return false;

            IsCastingSkill = true;
            CastTimer = 0.5f; // Base cast time
            CurrentCastSkill = skill;
            cooldown = skill.cooldown;

            if (_health != null) _health.UseStamina(20);

            OnSkillCast?.Invoke(skill.name);

            // Apply skill effects
            ApplySkillEffect(skill);

            return true;
        }

        private void ApplySkillEffect(SkillData skill)
        {
            switch (skill.type)
            {
                case "stealth":
                    IsStealthed = true;
                    StealthTimer = skill.duration;
                    break;

                case "enchant":
                    HasEnchantBuff = true;
                    EnchantTimer = skill.duration;
                    EnchantElement = "thunder";
                    break;

                case "transform":
                    HasTransformBuff = true;
                    TransformTimer = skill.duration;
                    AtkBuffMult = skill.atkBuff > 0 ? skill.atkBuff : 1f;
                    DefBuffMult = skill.defBuff > 0 ? skill.defBuff : 1f;
                    break;

                case "buff":
                    AtkBuffMult = skill.atkBuff > 0 ? skill.atkBuff : 1f;
                    // Buff will timeout after duration
                    Invoke(nameof(ClearBuff), skill.duration);
                    break;

                case "heal":
                    if (_health != null)
                    {
                        _health.Heal(skill.healAmount);
                    }
                    break;

                case "shield_wall":
                    DefBuffMult = skill.defBuff > 0 ? skill.defBuff : 3f;
                    Invoke(nameof(ClearDefBuff), skill.duration);
                    break;
            }
        }

        private void ClearBuff()
        {
            AtkBuffMult = 1f;
        }

        private void ClearDefBuff()
        {
            DefBuffMult = 1f;
        }

        #endregion

        #region DOT System

        public void ApplyDot(float damage, float duration, int sourcePlayerId)
        {
            if (_activeDots.TryGetValue(sourcePlayerId, out var existing))
            {
                existing.damage += damage;
                existing.remaining = Mathf.Max(existing.remaining, duration);
                _activeDots[sourcePlayerId] = existing;
            }
            else
            {
                _activeDots[sourcePlayerId] = new DotInfo
                {
                    damage = damage,
                    remaining = duration,
                    sourcePlayerId = sourcePlayerId
                };
            }
        }

        public int GetDotStacks()
        {
            int stacks = 0;
            foreach (var dot in _activeDots.Values)
            {
                stacks += (dot.remaining > 0) ? 1 : 0;
            }
            return stacks;
        }

        public bool HasDot() => GetDotStacks() > 0;

        private void TickDots(float dt)
        {
            if (_health == null) return;

            var toRemove = new List<int>();
            foreach (var kvp in _activeDots)
            {
                var dot = kvp.Value;
                if (dot.remaining <= 0)
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }

                dot.remaining -= dt;
                _health.TakeDamage(dot.damage * dt, false);
                _activeDots[kvp.Key] = dot;
            }

            foreach (var key in toRemove)
            {
                _activeDots.Remove(key);
            }
        }

        #endregion

        #region Cooldown Queries

        public float GetQCooldownPercent() => _charData != null ? qCooldownRemaining / _charData.skillQ.cooldown : 0;
        public float GetECooldownPercent() => _charData != null ? eCooldownRemaining / _charData.skillE.cooldown : 0;
        public float GetRCooldownPercent() => _charData != null ? rCooldownRemaining / _charData.skillR.cooldown : 0;

        public bool IsQReady() => qCooldownRemaining <= 0;
        public bool IsEReady() => eCooldownRemaining <= 0;
        public bool IsRReady() => rCooldownRemaining <= 0;

        /// <summary>
        /// Returns total attack multiplier including all buffs.
        /// </summary>
        public float GetTotalAtkBuff()
        {
            float buff = AtkBuffMult;

            // Passive: 不屈 - lower HP = more damage
            if (_charData != null && _charData.passive.name == "不屈" && _health != null)
            {
                float hpPercent = _health.CurrentHp / _health.MaxHp;
                if (hpPercent <= 0.3f) buff *= 1.5f;
            }

            return buff;
        }

        public float GetTotalDefBuff() => DefBuffMult;

        #endregion

        /// <summary>
        /// Reset all cooldowns (for training mode or round reset).
        /// </summary>
        public void ResetCooldowns()
        {
            qCooldownRemaining = 0;
            eCooldownRemaining = 0;
            rCooldownRemaining = 0;
            IsCastingSkill = false;
            IsStealthed = false;
            HasEnchantBuff = false;
            HasTransformBuff = false;
            AtkBuffMult = 1f;
            DefBuffMult = 1f;
            _activeDots.Clear();
        }
    }
}
