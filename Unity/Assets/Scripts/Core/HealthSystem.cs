using System;
using UnityEngine;

namespace QBlockyFighter.Core
{
    /// <summary>
    /// HP and Stamina system with regen, exhaustion, and damage handling.
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        [Header("HP")]
        [SerializeField] private float maxHp = 1000f;
        [SerializeField] private float currentHp;
        [SerializeField] private float hpRegenRate = 20f;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina;
        [SerializeField] private float staminaRegenRate = 15f;
        [SerializeField] private float staminaRegenDelay = 1f;
        [SerializeField] private float exhaustedDuration = 3f;

        // State
        public float MaxHp => maxHp;
        public float CurrentHp => currentHp;
        public float HpPercent => currentHp / maxHp;
        public float MaxStamina => maxStamina;
        public float CurrentStamina => currentStamina;
        public float StaminaPercent => currentStamina / maxStamina;
        public bool IsDead { get; private set; }
        public bool IsExhausted { get; private set; }
        public int Level { get; private set; } = 1;
        public int Exp { get; private set; }
        public int Kills { get; private set; }

        private float _staminaRegenTimer;
        private float _exhaustedTimer;
        private SkillSystem _skillSystem;
        private CombatSystem _combat;

        // Events
        public event Action<float, float> OnHpChanged;         // current, max
        public event Action<float, float> OnStaminaChanged;     // current, max
        public event Action<float> OnDamageTaken;               // damage amount
        public event Action<float> OnHealed;                    // heal amount
        public event Action OnDeath;
        public event Action OnExhausted;
        public event Action OnExhaustedEnd;
        public event Action<int> OnLevelUp;

        public void Initialize(CharacterData data)
        {
            maxHp = data.baseHp;
            currentHp = maxHp;
            hpRegenRate = data.hpRegen;
            currentStamina = maxStamina;
            _skillSystem = GetComponent<SkillSystem>();
            _combat = GetComponent<CombatSystem>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (IsDead) return;

            // HP regen
            if (currentHp < maxHp && currentHp > 0)
            {
                float regen = hpRegenRate * dt;
                // Passive: 祖灵守护 - doubled self regen
                if (_skillSystem != null && _skillSystem.HasTransformBuff)
                {
                    regen *= 2f;
                }
                currentHp = Mathf.Min(currentHp + regen, maxHp);
                OnHpChanged?.Invoke(currentHp, maxHp);
            }

            // Stamina regen
            _staminaRegenTimer -= dt;
            if (_staminaRegenTimer <= 0 && currentStamina < maxStamina && !IsExhausted)
            {
                float regen = staminaRegenRate * dt;
                currentStamina = Mathf.Min(currentStamina + regen, maxStamina);
                OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            }

            // Exhausted timer
            if (IsExhausted)
            {
                _exhaustedTimer -= dt;
                if (_exhaustedTimer <= 0)
                {
                    IsExhausted = false;
                    currentStamina = maxStamina * 0.3f;
                    OnExhaustedEnd?.Invoke();
                    OnStaminaChanged?.Invoke(currentStamina, maxStamina);
                }
            }

            // Poison tick
            if (_combat != null)
            {
                _combat.TickPoison(dt);
            }
        }

        #region Damage

        public void TakeDamage(float damage, bool triggerOnHit = true)
        {
            if (IsDead || damage <= 0) return;

            // Apply defense buff
            if (_skillSystem != null)
            {
                damage /= _skillSystem.GetTotalDefBuff();
            }

            // Passive: 祖灵守护 nearby allies - reduced self damage handled externally
            damage = Mathf.Max(0, damage);
            currentHp -= damage;
            currentHp = Mathf.Max(0, currentHp);

            if (triggerOnHit) OnDamageTaken?.Invoke(damage);
            OnHpChanged?.Invoke(currentHp, maxHp);

            if (currentHp <= 0)
            {
                Die();
            }
        }

        /// <summary>带伤害来源的重载（AI和游戏模式使用）</summary>
        public void TakeDamage(float damage, GameObject source) => TakeDamage(damage, true);

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0) return;
            currentHp = Mathf.Min(currentHp + amount, maxHp);
            OnHealed?.Invoke(amount);
            OnHpChanged?.Invoke(currentHp, maxHp);
        }

        #endregion

        #region Stamina

        public bool CanUseStamina(float amount)
        {
            return currentStamina >= amount && !IsExhausted;
        }

        public bool UseStamina(float amount)
        {
            if (!CanUseStamina(amount)) return false;

            currentStamina -= amount;
            _staminaRegenTimer = staminaRegenDelay;

            if (currentStamina <= 0)
            {
                currentStamina = 0;
                EnterExhausted();
            }

            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            return true;
        }

        private void EnterExhausted()
        {
            IsExhausted = true;
            _exhaustedTimer = exhaustedDuration;
            OnExhausted?.Invoke();
        }

        #endregion

        #region Death & Respawn

        private void Die()
        {
            IsDead = true;
            currentHp = 0;
            OnDeath?.Invoke();
        }

        public void Respawn()
        {
            IsDead = false;
            currentHp = maxHp;
            currentStamina = maxStamina;
            IsExhausted = false;
            OnHpChanged?.Invoke(currentHp, maxHp);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        public void ResetState()
        {
            IsDead = false;
            currentHp = maxHp;
            currentStamina = maxStamina;
            IsExhausted = false;
            OnHpChanged?.Invoke(currentHp, maxHp);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        #endregion

        #region Level & Exp

        public void AddExp(int amount)
        {
            Exp += amount;
            int expNeeded = Level * 100;
            while (Exp >= expNeeded)
            {
                Exp -= expNeeded;
                Level++;
                OnLevelUp?.Invoke(Level);
                expNeeded = Level * 100;
            }
        }

        public void AddKill()
        {
            Kills++;
            AddExp(50);
        }

        #endregion

        /// <summary>
        /// Set max HP based on character data.
        /// </summary>
        public void SetMaxHp(float hp)
        {
            maxHp = hp;
            currentHp = Mathf.Min(currentHp, maxHp);
            OnHpChanged?.Invoke(currentHp, maxHp);
        }
    }
}
