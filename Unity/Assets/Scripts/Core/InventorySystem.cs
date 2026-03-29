using System;
using System.Collections.Generic;
using UnityEngine;

namespace QBlockyFighter.Core
{
    /// <summary>
    /// Inventory system for weapon drops, pickups, and item management.
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        [Header("Inventory")]
        [SerializeField] private int maxSlots = 6;
        [SerializeField] private float pickupRange = 3f;

        public List<InventoryItem> Items { get; private set; } = new();

        private WeaponSystem _weaponSystem;

        public event Action<InventoryItem> OnItemPickedUp;
        public event Action<InventoryItem> OnItemDropped;
        public event Action<int> OnSlotChanged;

        private void Awake()
        {
            _weaponSystem = GetComponent<WeaponSystem>();
        }

        #region Item Management

        public bool AddItem(InventoryItem item)
        {
            if (Items.Count >= maxSlots) return false;
            Items.Add(item);
            OnItemPickedUp?.Invoke(item);
            return true;
        }

        public bool RemoveItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= Items.Count) return false;
            var item = Items[slotIndex];
            Items.RemoveAt(slotIndex);
            OnItemDropped?.Invoke(item);
            return true;
        }

        public InventoryItem GetItem(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < Items.Count ? Items[slotIndex] : null;
        }

        public bool UseItem(int slotIndex)
        {
            var item = GetItem(slotIndex);
            if (item == null) return false;

            switch (item.itemType)
            {
                case ItemType.HealthPotion:
                    var health = GetComponent<HealthSystem>();
                    if (health != null)
                    {
                        health.Heal(item.value);
                        Items.RemoveAt(slotIndex);
                        OnSlotChanged?.Invoke(slotIndex);
                        return true;
                    }
                    break;

                case ItemType.StaminaPotion:
                    var hs = GetComponent<HealthSystem>();
                    if (hs != null && hs.UseStamina(-item.value)) // negative = restore
                    {
                        Items.RemoveAt(slotIndex);
                        OnSlotChanged?.Invoke(slotIndex);
                        return true;
                    }
                    break;
            }

            return false;
        }

        #endregion

        #region Weapon Drops

        /// <summary>
        /// Generate a random weapon drop with quality based on probability.
        /// </summary>
        public static InventoryItem GenerateWeaponDrop(int characterWeaponCount)
        {
            float roll = UnityEngine.Random.value;
            WeaponQuality quality;
            if (roll < 0.02f)
                quality = WeaponQuality.Gold;
            else if (roll < 0.10f)
                quality = WeaponQuality.Silver;
            else if (roll < 0.30f)
                quality = WeaponQuality.Bronze;
            else
                quality = WeaponQuality.Black;

            int weaponIndex = UnityEngine.Random.Range(0, WeaponSystem.AllWeaponTypes.Length);
            string weaponType = WeaponSystem.AllWeaponTypes[weaponIndex];

            return new InventoryItem
            {
                itemType = ItemType.Weapon,
                name = GetWeaponName(weaponType),
                quality = quality,
                weaponType = weaponType,
                value = GetQualityValue(quality)
            };
        }

        private static string GetWeaponName(string type)
        {
            return type switch
            {
                "sword" => "短剑",
                "whip" => "软剑",
                "dagger" => "匕首",
                "chain" => "锁链",
                "halberd" => "长戟",
                "bow" => "长弓",
                "throw" => "飞刀",
                "axe" => "战斧",
                "saber" => "弯刀",
                "shield" => "盾牌",
                "musket" => "火铳",
                "totem" => "图腾",
                _ => "武器"
            };
        }

        private static float GetQualityValue(WeaponQuality quality)
        {
            return quality switch
            {
                WeaponQuality.Black => 1.0f,
                WeaponQuality.Bronze => 1.1f,
                WeaponQuality.Silver => 1.2f,
                WeaponQuality.Gold => 1.35f,
                _ => 1.0f
            };
        }

        #endregion

        #region Pickup

        /// <summary>
        /// Try to pick up a dropped item in range.
        /// </summary>
        public bool TryPickup(DroppedItem droppedItem)
        {
            if (droppedItem == null) return false;
            float dist = Vector3.Distance(transform.position, droppedItem.transform.position);
            if (dist > pickupRange) return false;

            return AddItem(droppedItem.ItemData);
        }

        #endregion
    }

    #region Item Types

    [Serializable]
    public class InventoryItem
    {
        public ItemType itemType;
        public string name;
        public WeaponQuality quality;
        public string weaponType;
        public float value;
        public string icon;
    }

    public enum ItemType
    {
        Weapon,
        HealthPotion,
        StaminaPotion,
        Material,
        Quest
    }

    /// <summary>
    /// Dropped item in the world that can be picked up.
    /// </summary>
    public class DroppedItem : MonoBehaviour
    {
        public InventoryItem ItemData;
        public float Lifetime = 30f;

        private void Update()
        {
            Lifetime -= Time.deltaTime;
            if (Lifetime <= 0) Destroy(gameObject);
        }

        public static DroppedItem Create(Vector3 position, InventoryItem item, GameObject prefab = null)
        {
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, position, Quaternion.identity);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = position + Vector3.up * 0.5f;
                go.transform.localScale = Vector3.one * 0.3f;
                Destroy(go.GetComponent<Collider>());
                var col = go.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 1f;
            }

            var dropped = go.AddComponent<DroppedItem>();
            dropped.ItemData = item;

            // Color based on quality
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = item.quality switch
                {
                    WeaponQuality.Bronze => new Color(0.27f, 0.8f, 0.27f),
                    WeaponQuality.Silver => new Color(0.4f, 0.53f, 1f),
                    WeaponQuality.Gold => new Color(1f, 0.84f, 0),
                    _ => new Color(0.53f, 0.53f, 0.53f)
                };
            }

            return dropped;
        }
    }

    #endregion
}
