using UnityEngine;

namespace QBlockyFighter.Map
{
    public class ZoneController : MonoBehaviour
    {
        // 安全区/缩圈系统
        public float SafeZoneRadius { get; private set; } = 30f;
        public Vector3 SafeZoneCenter { get; private set; } = Vector3.zero;
        public float ShrinkSpeed = 0.5f;
        public float MinRadius = 5f;
        public float ShrinkStartDelay = 600f; // 10分钟后开始缩圈

        private float shrinkTimer;
        private bool shrinking = false;
        private float targetRadius;

        // 区域提示
        private string currentZoneName = "";
        private float zoneHintTimer;

        void Start()
        {
            targetRadius = SafeZoneRadius;
            shrinkTimer = ShrinkStartDelay;
        }

        void Update()
        {
            // 缩圈计时
            if (!shrinking)
            {
                shrinkTimer -= Time.deltaTime;
                if (shrinkTimer <= 0)
                {
                    shrinking = true;
                    targetRadius = Mathf.Max(MinRadius, SafeZoneRadius * 0.6f);
                    Debug.Log("[Zone] 安全区开始缩小!");
                }
            }

            // 执行缩圈
            if (shrinking && SafeZoneRadius > MinRadius)
            {
                SafeZoneRadius = Mathf.MoveTowards(SafeZoneRadius, targetRadius, ShrinkSpeed * Time.deltaTime);

                if (Mathf.Approximately(SafeZoneRadius, targetRadius) && targetRadius > MinRadius)
                {
                    targetRadius = Mathf.Max(MinRadius, targetRadius * 0.6f);
                }
            }

            // 区域提示
            if (zoneHintTimer > 0)
            {
                zoneHintTimer -= Time.deltaTime;
            }
        }

        // 检查玩家是否在安全区内
        public bool IsInSafeZone(Vector3 position)
        {
            return Vector3.Distance(position, SafeZoneCenter) <= SafeZoneRadius;
        }

        // 获取圈外伤害
        public float GetOutsideDamage(float timeOutside)
        {
            return 5f + timeOutside * 2f; // 每秒递增伤害
        }

        // 设置区域提示
        public void ShowZoneHint(string zoneName, float duration = 3f)
        {
            currentZoneName = zoneName;
            zoneHintTimer = duration;
        }

        void OnDrawGizmos()
        {
            // 编辑器中显示安全区范围
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(SafeZoneCenter, SafeZoneRadius);
        }

        public void SetSafeZone(float radius, Vector3 center)
        {
            SafeZoneRadius = radius;
            SafeZoneCenter = center;
        }

        public string GetCurrentZoneName() => currentZoneName;
        public bool IsShrinking() => shrinking;
    }
}
