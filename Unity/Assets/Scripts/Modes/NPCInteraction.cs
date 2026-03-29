using UnityEngine;

namespace QBlockyFighter.Modes
{
    /// <summary>
    /// NPC交互组件 - 商人和任务发布者
    /// </summary>
    public class NPCInteraction : MonoBehaviour
    {
        public NPCType npcType;
        public string npcName = "NPC";

        [Header("商人商品")]
        public int healthPotionCost = 50;
        public int randomWeaponCost = 100;
        public int epicWeaponCost = 250;
        public int specificWeaponCost = 150;

        [Header("任务配置")]
        public string[] questDescriptions = {
            "击杀3只野怪",
            "开启1个宝箱",
            "探索1个遗迹",
            "存活超过5分钟"
        };
        public int[] questGoldRewards = { 80, 60, 120, 100 };

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            Debug.Log($"[NPC] {other.name} 靠近 {npcName} - 交互提示显示");
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            Debug.Log($"[NPC] {other.name} 离开 {npcName}");
        }

        /// <summary>
        /// 商人：购买生命药水
        /// </summary>
        public bool BuyHealthPotion(Core.PlayerController player)
        {
            if (npcType != NPCType.Merchant) return false;

            var brMode = FindObjectOfType<BattleRoyaleMode>();
            if (brMode == null || !brMode.SpendGold(player, healthPotionCost)) return false;

            var hp = player.GetComponent<Core.HealthSystem>();
            if (hp != null)
            {
                hp.Heal(hp.MaxHp * 0.4f); // 回40%血
                Debug.Log($"[NPC] {player.name} 购买生命药水，回复40%血量");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 商人：购买随机武器（RPG模式 - 替换武器+技能）
        /// </summary>
        public bool BuyWeapon(Core.PlayerController player, bool epic = false)
        {
            if (npcType != NPCType.Merchant) return false;

            int cost = epic ? epicWeaponCost : randomWeaponCost;
            var brMode = FindObjectOfType<BattleRoyaleMode>();
            if (brMode == null || !brMode.SpendGold(player, cost)) return false;

            // RPG模式：随机武器类型
            var rpgPickup = Core.RPGWeaponPickup.Instance;
            if (rpgPickup != null)
            {
                int weaponType = Random.Range(0, Core.RPGWeaponPickup.GetWeaponCount());
                var quality = epic ? Core.WeaponQuality.Silver : Core.WeaponQuality.Bronze;
                rpgPickup.PickupWeapon(player.gameObject, weaponType, quality);
                Debug.Log($"[NPC] {player.name} 购买{(epic ? "精品" : "普通")}武器");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 商人：购买指定类型武器
        /// </summary>
        public bool BuySpecificWeapon(Core.PlayerController player, int weaponTypeIndex)
        {
            if (npcType != NPCType.Merchant) return false;

            var brMode = FindObjectOfType<BattleRoyaleMode>();
            if (brMode == null || !brMode.SpendGold(player, specificWeaponCost)) return false;

            var rpgPickup = Core.RPGWeaponPickup.Instance;
            if (rpgPickup != null)
            {
                rpgPickup.PickupWeapon(player.gameObject, weaponTypeIndex, Core.WeaponQuality.Bronze);
                var weaponSet = Core.RPGWeaponPickup.GetWeaponSkillSet(weaponTypeIndex);
                Debug.Log($"[NPC] {player.name} 购买了 {weaponSet.weaponName}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 任务NPC：获取随机任务
        /// </summary>
        public QuestData GetQuest()
        {
            if (npcType != NPCType.QuestGiver) return null;

            int idx = Random.Range(0, questDescriptions.Length);
            return new QuestData
            {
                description = questDescriptions[idx],
                goldReward = questGoldRewards[idx],
                questType = (QuestType)idx,
                targetCount = GetQuestTarget(idx),
                currentCount = 0,
                isCompleted = false
            };
        }

        private int GetQuestTarget(int questIndex)
        {
            return questIndex switch
            {
                0 => 3,  // 击杀3只野怪
                1 => 1,  // 开1个宝箱
                2 => 1,  // 探索1个遗迹
                3 => 1,  // 存活5分钟（用bool表示）
                _ => 1
            };
        }
    }

    /// <summary>任务类型</summary>
    public enum QuestType
    {
        KillMonsters,    // 击杀野怪
        OpenChests,      // 开宝箱
        ExploreRuins,    // 探索遗迹
        Survive          // 存活
    }

    /// <summary>任务数据</summary>
    [System.Serializable]
    public class QuestData
    {
        public string description;
        public int goldReward;
        public QuestType questType;
        public int targetCount;
        public int currentCount;
        public bool isCompleted;
    }
}
