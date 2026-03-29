using System;
using UnityEngine;

namespace QBlockyFighter.Core
{
    /// <summary>
    /// Dual weapon system with switching, quality tiers, and combo integration.
    /// </summary>
    public class WeaponSystem : MonoBehaviour
    {
        [Header("Weapons")]
        [SerializeField] private int currentWeaponIndex = 0;
        [SerializeField] private float switchDuration = 0.3f;
        [SerializeField] private float switchDamageBonus = 1.2f;
        [SerializeField] private float switchDamageBonusDuration = 2f;

        // State
        public int CurrentWeaponIndex => currentWeaponIndex;
        public bool IsSwitching { get; private set; }
        public bool HasSwitchBonus { get; private set; }
        public WeaponQuality Quality { get; private set; }

        private WeaponData[] _weapons;
        private float _switchTimer;
        private float _switchBonusTimer;
        private int _kills = 0;

        // Events
        public event Action<int, int> OnWeaponSwitched; // oldIndex, newIndex
        public event Action<WeaponQuality> OnQualityChanged;
        public event Action OnSwitchBonusApplied;

        public WeaponData CurrentWeaponData =>
            _weapons != null && currentWeaponIndex < _weapons.Length
                ? _weapons[currentWeaponIndex]
                : null;

        public WeaponData SecondaryWeaponData =>
            _weapons != null && _weapons.Length > 1
                ? _weapons[1 - currentWeaponIndex]
                : null;

        public void Initialize(WeaponData[] weapons)
        {
            _weapons = weapons;
            currentWeaponIndex = 0;
            Quality = WeaponQuality.Black;
        }

        private void Update()
        {
            // Switch timer
            if (IsSwitching)
            {
                _switchTimer -= Time.deltaTime;
                if (_switchTimer <= 0)
                {
                    IsSwitching = false;
                }
            }

            // Switch damage bonus
            if (HasSwitchBonus)
            {
                _switchBonusTimer -= Time.deltaTime;
                if (_switchBonusTimer <= 0)
                {
                    HasSwitchBonus = false;
                }
            }
        }

        #region Weapon Switching

        public bool TrySwitchWeapon()
        {
            if (IsSwitching || _weapons == null || _weapons.Length < 2) return false;

            // Check if combat allows switching
            var combat = GetComponent<CombatSystem>();
            if (combat != null && (combat.IsStunned || combat.IsKnockedDown)) return false;

            int oldIndex = currentWeaponIndex;
            currentWeaponIndex = 1 - currentWeaponIndex;
            IsSwitching = true;
            _switchTimer = switchDuration;

            // Apply switch damage bonus
            HasSwitchBonus = true;
            _switchBonusTimer = switchDamageBonusDuration;

            OnWeaponSwitched?.Invoke(oldIndex, currentWeaponIndex);
            OnSwitchBonusApplied?.Invoke();

            return true;
        }

        public void SetWeaponIndex(int index)
        {
            if (_weapons == null || index < 0 || index >= _weapons.Length) return;
            int oldIndex = currentWeaponIndex;
            currentWeaponIndex = index;
            OnWeaponSwitched?.Invoke(oldIndex, currentWeaponIndex);
        }

        #endregion

        #region Quality System

        /// <summary>
        /// Get damage multiplier based on weapon quality.
        /// </summary>
        public float GetQualityMultiplier()
        {
            return Quality switch
            {
                WeaponQuality.Black => 1.0f,
                WeaponQuality.Bronze => 1.1f,
                WeaponQuality.Silver => 1.2f,
                WeaponQuality.Gold => 1.35f,
                _ => 1.0f
            };
        }

        /// <summary>
        /// Get switch damage bonus multiplier.
        /// </summary>
        public float GetSwitchBonus()
        {
            return HasSwitchBonus ? switchDamageBonus : 1.0f;
        }

        /// <summary>
        /// Advance quality based on kills.
        /// </summary>
        public void OnGetKill()
        {
            _kills++;
            WeaponQuality newQuality = _kills switch
            {
                < 3 => WeaponQuality.Black,
                < 6 => WeaponQuality.Bronze,
                < 10 => WeaponQuality.Silver,
                _ => WeaponQuality.Gold
            };

            if (newQuality != Quality)
            {
                Quality = newQuality;
                OnQualityChanged?.Invoke(Quality);
            }
        }

        public void ResetQuality()
        {
            _kills = 0;
            Quality = WeaponQuality.Black;
            OnQualityChanged?.Invoke(Quality);
        }

        public string GetQualityName()
        {
            return Quality switch
            {
                WeaponQuality.Black => "黑铁",
                WeaponQuality.Bronze => "青铜",
                WeaponQuality.Silver => "白银",
                WeaponQuality.Gold => "黄金",
                _ => "黑铁"
            };
        }

        public Color GetQualityColor()
        {
            return Quality switch
            {
                WeaponQuality.Black => new Color(0.53f, 0.53f, 0.53f),
                WeaponQuality.Bronze => new Color(0.27f, 0.8f, 0.27f),
                WeaponQuality.Silver => new Color(0.4f, 0.53f, 1f),
                WeaponQuality.Gold => new Color(1f, 0.84f, 0),
                _ => Color.gray
            };
        }

        #endregion

        #region Combo Data

        /// <summary>
        /// Get the combo attack data for current weapon's light attack sequence.
        /// </summary>
        public (float damage, float range, float angle) GetLightAttackData(int phase, CharacterData charData)
        {
            if (charData == null) return (30, 3, Mathf.PI / 3);

            float baseDmg = charData.lightDamage[Mathf.Min(phase, charData.lightDamage.Length - 1)];
            float range = CurrentWeaponData?.range ?? 3f;
            float angle = CurrentWeaponData?.attackAngle ?? (Mathf.PI / 3);

            float totalMult = GetQualityMultiplier() * GetSwitchBonus();
            return (baseDmg * totalMult, range, angle);
        }

        /// <summary>
        /// Get the combo attack data for current weapon's heavy attack sequence.
        /// </summary>
        public (float damage, float range, float angle) GetHeavyAttackData(int phase, CharacterData charData)
        {
            if (charData == null) return (80, 3, Mathf.PI / 3);

            float baseDmg = charData.heavyDamage[Mathf.Min(phase, charData.heavyDamage.Length - 1)];
            float range = CurrentWeaponData?.range ?? 3f;
            float angle = CurrentWeaponData?.attackAngle ?? (Mathf.PI / 3);

            float totalMult = GetQualityMultiplier() * GetSwitchBonus();
            return (baseDmg * totalMult, range, angle);
        }

        #endregion

        /// <summary>
        /// 12 weapon types available in the game.
        /// </summary>
        public static readonly string[] AllWeaponTypes = new[]
        {
            "sword", "whip", "dagger", "chain", "halberd", "bow",
            "throw", "axe", "saber", "shield", "musket", "totem"
        };
    }
}
