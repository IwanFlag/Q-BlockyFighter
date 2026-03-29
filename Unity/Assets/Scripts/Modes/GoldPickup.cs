using UnityEngine;

namespace QBlockyFighter.Modes
{
    /// <summary>
    /// 金币拾取物 - 击杀野怪掉落
    /// </summary>
    public class GoldPickup : MonoBehaviour
    {
        public int goldAmount = 10;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            var player = other.GetComponent<Core.PlayerController>();
            if (player == null) return;

            var brMode = FindObjectOfType<BattleRoyaleMode>();
            if (brMode != null)
            {
                brMode.AddGold(player, goldAmount);
                Debug.Log($"[拾取] {player.name} 获得 {goldAmount} 金币");
            }

            Destroy(gameObject);
        }
    }
}
