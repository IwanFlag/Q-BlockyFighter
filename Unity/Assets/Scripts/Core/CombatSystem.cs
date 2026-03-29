using System;
using System.Collections.Generic;
using UnityEngine;

namespace QBlockyFighter.Core
{
    /// <summary>
    /// Core combat system handling attack, combo, parry, dodge, block, super armor, air combo, and recovery.
    /// </summary>
    public class CombatSystem : MonoBehaviour
    {
        [Header("Combat Config")]
        [SerializeField] private float comboResetTime = 1.0f;
        [SerializeField] private float parryWindow = 0.15f;
        [SerializeField] private float dodgeInvulnDuration = 0.3f;
        [SerializeField] private float dodgeCooldown = 0.5f;
        [SerializeField] private float blockDamageReduction = 0.7f;
        [SerializeField] private float superArmorDamageReduction = 0.3f;
        [SerializeField] private float attackFreezeDuration = 0.05f;

        // State
        public bool IsAttacking { get; private set; }
        public bool IsBlocking { get; private set; }
        public bool IsDodging { get; private set; }
        public bool IsInvulnerable { get; private set; }
        public bool HasSuperArmor { get; set; }
        public bool IsStunned { get; private set; }
        public bool IsInAir { get; set; }
        public bool IsKnockedDown { get; private set; }
        public int CurrentCombo { get; private set; }
        public int MaxCombo { get; private set; }
        public int LightAttackPhase { get; private set; }
        public int HeavyAttackPhase { get; private set; }
        public float StunTimer { get; private set; }
        public float KnockdownTimer { get; private set; }
        public bool ParrySuccess { get; private set; }
        public float ParryRiposteTimer { get; private set; }
        public bool InParryRiposteWindow => ParryRiposteTimer > 0;

        // Attack state
        private float _attackTimer;
        private float _attackDuration;
        private bool _isHeavyAttack;
        private float _comboResetTimer;
        private float _dodgeTimer;
        private float _lastParryTime;
        private int _poisonStacks;

        // References
        private HealthSystem _health;
        private CharacterData _charData;

        // Events
        public event Action<int, float, bool> OnDealDamage;       // targetId, damage, isCritical
        public event Action<float, bool> OnTakeDamage;             // damage, isParry
        public event Action OnParrySuccess;
        public event Action OnDodge;
        public event Action OnComboEnd;
        public event Action<int> OnComboChanged;

        public void Initialize(CharacterData data)
        {
            _charData = data;
            _health = GetComponent<HealthSystem>();
        }

        private void Update()
        {
            UpdateTimers(Time.deltaTime);
        }

        private void UpdateTimers(float dt)
        {
            // Attack timer
            if (IsAttacking)
            {
                _attackTimer -= dt;
                if (_attackTimer <= 0)
                {
                    IsAttacking = false;
                    HasSuperArmor = false;
                }
            }

            // Combo reset
            if (CurrentCombo > 0)
            {
                _comboResetTimer -= dt;
                if (_comboResetTimer <= 0)
                {
                    ResetCombo();
                }
            }

            // Stun timer
            if (IsStunned)
            {
                StunTimer -= dt;
                if (StunTimer <= 0)
                {
                    IsStunned = false;
                }
            }

            // Knockdown / recovery timer
            if (IsKnockedDown)
            {
                KnockdownTimer -= dt;
                if (KnockdownTimer <= 0)
                {
                    IsKnockedDown = false;
                }
            }

            // Dodge timer
            if (IsDodging)
            {
                _dodgeTimer -= dt;
                if (_dodgeTimer <= 0)
                {
                    IsDodging = false;
                    IsInvulnerable = false;
                }
            }

            // Parry riposte window
            if (ParryRiposteTimer > 0)
            {
                ParryRiposteTimer -= dt;
            }

            // Parry success flag reset
            if (ParrySuccess && Time.time - _lastParryTime > 0.5f)
            {
                ParrySuccess = false;
            }
        }

        #region Light Attack

        public bool TryLightAttack()
        {
            if (!CanAct()) return false;
            if (IsAttacking && !_isHeavyAttack && _attackTimer > 0.1f) return false;

            IsAttacking = true;
            _isHeavyAttack = false;

            // Advance combo phase
            if (_charData != null)
            {
                LightAttackPhase = LightAttackPhase % _charData.lightDamage.Length;
                float damage = _charData.lightDamage[LightAttackPhase];
                float attackTime = 0.3f + LightAttackPhase * 0.05f;

                _attackTimer = attackTime;
                _attackDuration = attackTime;
                LightAttackPhase++;

                // After parry bonus
                if (InParryRiposteWindow)
                {
                    damage *= 1.5f;
                    ParryRiposteTimer = 0;
                }

                // Increment combo
                CurrentCombo++;
                _comboResetTimer = comboResetTime;
                if (CurrentCombo > MaxCombo) MaxCombo = CurrentCombo;
                OnComboChanged?.Invoke(CurrentCombo);

                // Deal damage via attack detection (will be handled by HitBox system)
                BroadcastAttack(damage, false);
            }

            return true;
        }

        #endregion

        #region Heavy Attack

        public bool TryHeavyAttack()
        {
            if (!CanAct()) return false;
            if (IsAttacking && _attackTimer > 0.1f) return false;

            IsAttacking = true;
            _isHeavyAttack = true;

            if (_charData != null)
            {
                HeavyAttackPhase = HeavyAttackPhase % _charData.heavyDamage.Length;
                float damage = _charData.heavyDamage[HeavyAttackPhase];
                float attackTime = 0.5f + HeavyAttackPhase * 0.1f;

                _attackTimer = attackTime;
                _attackDuration = attackTime;
                HeavyAttackPhase++;

                // Super armor on heavy attack for certain characters
                if (_charData.passive.name == "武圣" || _charData.style == "tank")
                {
                    HasSuperArmor = true;
                }

                // After parry bonus
                if (InParryRiposteWindow)
                {
                    damage *= 1.5f;
                    ParryRiposteTimer = 0;
                }

                CurrentCombo++;
                _comboResetTimer = comboResetTime;
                if (CurrentCombo > MaxCombo) MaxCombo = CurrentCombo;
                OnComboChanged?.Invoke(CurrentCombo);

                BroadcastAttack(damage, true);
            }

            return true;
        }

        #endregion

        #region Dodge

        public bool TryDodge(Vector3 direction)
        {
            if (!CanDodge()) return false;

            IsDodging = true;
            IsInvulnerable = true;
            _dodgeTimer = dodgeInvulnDuration;

            // Apply dodge movement via PlayerController
            var controller = GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.ApplyDodge(direction);
            }

            OnDodge?.Invoke();
            return true;
        }

        public bool CanDodge()
        {
            return !IsStunned && !IsKnockedDown && !IsDodging &&
                   _health != null && _health.CanUseStamina(25);
        }

        #endregion

        #region Block & Parry

        public bool TryBlock()
        {
            if (!CanBlock()) return false;
            IsBlocking = true;
            return true;
        }

        public void StopBlock()
        {
            IsBlocking = false;
        }

        public bool CanBlock()
        {
            return !IsStunned && !IsKnockedDown && !IsDodging && !IsAttacking &&
                   _health != null && _health.CanUseStamina(5);
        }

        /// <summary>
        /// Called when this character receives an attack while blocking.
        /// </summary>
        public float ProcessIncomingDamage(float damage, bool isHeavy)
        {
            // Check parry (perfect block timing)
            if (IsBlocking && Time.time - _lastParryTime < parryWindow)
            {
                // Successful parry
                ParrySuccess = true;
                _lastParryTime = Time.time;
                ParryRiposteTimer = 1.0f; // 1 second riposte window
                OnParrySuccess?.Invoke();
                OnTakeDamage?.Invoke(0, true);

                // Stagger attacker handled externally
                if (_health != null) _health.UseStamina(10);
                return 0;
            }

            // Normal block
            if (IsBlocking)
            {
                float reducedDamage = damage * (1f - blockDamageReduction);
                if (_health != null) _health.UseStamina(damage * 0.1f);
                OnTakeDamage?.Invoke(reducedDamage, false);
                return reducedDamage;
            }

            // Super armor - don't stagger but take full damage
            if (HasSuperArmor)
            {
                float reducedDamage = damage * (1f - superArmorDamageReduction);
                OnTakeDamage?.Invoke(reducedDamage, false);
                return reducedDamage;
            }

            // Full damage
            OnTakeDamage?.Invoke(damage, false);
            return damage;
        }

        #endregion

        #region Stun & Knockdown

        public void ApplyStun(float duration)
        {
            if (HasSuperArmor) return;
            IsStunned = true;
            StunTimer = duration;
            IsAttacking = false;
            IsBlocking = false;
            ResetCombo();
        }

        public void ApplyKnockdown(float duration)
        {
            if (HasSuperArmor) return;
            IsKnockedDown = true;
            KnockdownTimer = duration;
            IsAttacking = false;
            IsBlocking = false;
            IsStunned = false;
            ResetCombo();
        }

        /// <summary>
        /// Called when player attempts recovery (受身) during knockdown.
        /// </summary>
        public bool TryRecovery()
        {
            if (!IsKnockedDown || KnockdownTimer <= 0) return false;
            // Can recover early (reduce knockdown time)
            KnockdownTimer = Mathf.Min(KnockdownTimer, 0.3f);
            return true;
        }

        #endregion

        #region Air Combo

        public void ApplyLaunch(float height)
        {
            if (HasSuperArmor) return;
            IsInAir = true;
            var controller = GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.ApplyLaunch(height);
            }
        }

        public void OnLand()
        {
            IsInAir = false;
        }

        #endregion

        #region Poison DOT

        public void ApplyPoison(int stacks)
        {
            _poisonStacks = Mathf.Min(_poisonStacks + stacks, 5);
        }

        public int GetPoisonStacks() => _poisonStacks;

        public void TickPoison(float dt)
        {
            if (_poisonStacks <= 0 || _health == null) return;
            float poisonDmg = _poisonStacks * 5f * dt;
            _health.TakeDamage(poisonDmg, false);
        }

        #endregion

        #region Helpers

        public bool CanAct()
        {
            return !IsStunned && !IsKnockedDown;
        }

        public void ResetCombo()
        {
            CurrentCombo = 0;
            LightAttackPhase = 0;
            HeavyAttackPhase = 0;
            OnComboEnd?.Invoke();
            OnComboChanged?.Invoke(0);
        }

        /// <summary>
        /// Begin block parry check (call when block input received right before hit).
        /// </summary>
        public void BeginParryCheck()
        {
            _lastParryTime = Time.time;
        }

        /// <summary>
        /// Get attack range for current weapon.
        /// </summary>
        public float GetAttackRange()
        {
            var ws = GetComponent<WeaponSystem>();
            if (ws != null) return ws.CurrentWeaponData?.range ?? 3f;
            return 3f;
        }

        /// <summary>
        /// Get attack angle for current weapon.
        /// </summary>
        public float GetAttackAngle()
        {
            var ws = GetComponent<WeaponSystem>();
            if (ws != null) return ws.CurrentWeaponData?.attackAngle ?? (Mathf.PI / 3);
            return Mathf.PI / 3;
        }

        private void BroadcastAttack(float damage, bool isHeavy)
        {
            // This will be picked up by HitBox components
            // Actual hit detection happens in HitBox/AttackArea trigger checks
        }

        #endregion
    }
}
