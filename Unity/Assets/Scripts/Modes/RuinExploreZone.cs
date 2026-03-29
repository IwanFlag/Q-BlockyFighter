using UnityEngine;

namespace QBlockyFighter.Modes
{
    /// <summary>
    /// 遗迹探索区域 - 玩家进入后长按探索获取奖励
    /// </summary>
    public class RuinExploreZone : MonoBehaviour
    {
        public RuinSite ruin;
        public float exploreTime = 5f;

        private Core.PlayerController currentExplorer;
        private float exploreTimer;
        private bool isExploring;

        void Update()
        {
            if (!isExploring || currentExplorer == null) return;

            // 检查玩家是否还站在区域内
            if (ruin.isExplored)
            {
                isExploring = false;
                return;
            }

            exploreTimer += Time.deltaTime;
            ruin.exploreProgress = exploreTimer / exploreTime;

            if (exploreTimer >= exploreTime)
            {
                CompleteExploration(currentExplorer);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (ruin.isExplored) return;

            var player = other.GetComponent<Core.PlayerController>();
            if (player == null) return;

            currentExplorer = player;
            isExploring = true;
            exploreTimer = ruin.exploreProgress * exploreTime; // 继续上次进度

            Debug.Log($"[遗迹] {player.name} 进入遗迹 {ruin.ruinName} 开始探索...");
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            var player = other.GetComponent<Core.PlayerController>();
            if (player == currentExplorer)
            {
                isExploring = false;
                // 进度保留，不重置
                Debug.Log($"[遗迹] {player.name} 暂停探索 {ruin.ruinName} (进度:{ruin.exploreProgress:P0})");
            }
        }

        private void CompleteExploration(Core.PlayerController player)
        {
            ruin.isExplored = true;
            ruin.isBeingExplored = false;
            ruin.exploreProgress = 1f;
            isExploring = false;

            // 遗迹奖励
            var brMode = FindObjectOfType<BattleRoyaleMode>();
            if (brMode != null)
            {
                // 大量金币
                int gold = Random.Range(100, 300);
                brMode.AddGold(player, gold);

                // 队友也获得奖励
                var teammate = brMode.GetTeammate(player);
                if (teammate != null)
                {
                    brMode.AddGold(teammate, gold / 2);
                }
            }

            // 随机武器
            var inventory = player.GetComponent<Core.InventorySystem>();
            if (inventory != null)
            {
                var item = Core.InventorySystem.GenerateWeaponDrop(0);
                // 遗迹奖励至少银色品质
                if (item.quality < Core.WeaponQuality.Silver)
                    item.quality = Core.WeaponQuality.Silver;
                inventory.AddItem(item);
            }

            // 回复状态
            var hp = player.GetComponent<Core.HealthSystem>();
            if (hp != null)
            {
                hp.Heal(hp.MaxHp * 0.5f); // 回50%血
            }

            // 变更遗迹外观（变为灰暗）
            if (ruin.rootObject != null)
            {
                var renderers = ruin.rootObject.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    var c = r.material.color;
                    r.material.color = new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, c.a);
                }
            }

            Debug.Log($"[遗迹] {player.name} 完成探索 {ruin.ruinName}! 获得大量奖励!");
        }
    }
}
