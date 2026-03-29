using UnityEngine;

namespace QBlockyFighter.Modes
{
    /// <summary>
    /// 宝箱拾取物 - 野怪/Boss掉落
    /// 普通宝箱：随机武器 + 金币
    /// 传说宝箱：高品质武器 + 大量金币 + 满血
    /// </summary>
    public class TreasureChestPickup : MonoBehaviour
    {
        public bool isLegendary = false;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            var player = other.GetComponent<Core.PlayerController>();
            if (player == null) return;

            if (isLegendary)
            {
                OpenLegendaryChest(player, other.gameObject);
            }
            else
            {
                OpenNormalChest(player, other.gameObject);
            }

            Destroy(gameObject);
        }

        private void OpenNormalChest(Core.PlayerController player, GameObject playerObj)
        {
            var brMode = FindObjectOfType<BattleRoyaleMode>();
            if (brMode != null)
            {
                int gold = Random.Range(50, 150);
                brMode.AddGold(player, gold);
            }

            // RPG模式：随机武器类型 + 品质，直接替换玩家武器和技能
            var rpgPickup = Core.RPGWeaponPickup.Instance;
            if (rpgPickup != null)
            {
                int weaponType = Random.Range(0, Core.RPGWeaponPickup.GetWeaponCount());
                var quality = (Core.WeaponQuality)Random.Range(1, 3); // 青铜~白银
                rpgPickup.PickupWeapon(playerObj, weaponType, quality);
            }

            Debug.Log($"[宝箱] {player.name} 开启普通宝箱");
        }

        private void OpenLegendaryChest(Core.PlayerController player, GameObject playerObj)
        {
            var brMode = FindObjectOfType<BattleRoyaleMode>();
            if (brMode != null)
            {
                int gold = Random.Range(200, 500);
                brMode.AddGold(player, gold);
            }

            // 传说宝箱：保底白银品质武器
            var rpgPickup = Core.RPGWeaponPickup.Instance;
            if (rpgPickup != null)
            {
                int weaponType = Random.Range(0, Core.RPGWeaponPickup.GetWeaponCount());
                var quality = Random.value < 0.3f ? Core.WeaponQuality.Gold : Core.WeaponQuality.Silver;
                rpgPickup.PickupWeapon(playerObj, weaponType, quality);
            }

            // 回满血
            var hp = player.GetComponent<Core.HealthSystem>();
            if (hp != null) hp.Heal(hp.MaxHp);

            Debug.Log($"[宝箱] {player.name} 开启传说宝箱! 高品质武器+满血!");
        }
    }
}
