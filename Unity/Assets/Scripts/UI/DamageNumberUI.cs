using UnityEngine;
using System.Collections.Generic;

namespace QBlockyFighter.UI
{
    /// <summary>
    /// 伤害飘字系统 - 攻击时显示伤害数字，暴击/弹反特殊样式
    /// 从被打者头顶飘出，向上浮动渐隐消失
    /// </summary>
    public class DamageNumberUI : MonoBehaviour
    {
        public static DamageNumberUI Instance { get; private set; }

        private struct DamageNumber
        {
            public Vector3 worldPos;
            public string text;
            public Color color;
            public float fontSize;
            public float time;
            public float duration;
            public Vector3 velocity;
            public bool isCritical;
            public bool isParry;
        }

        private List<DamageNumber> numbers = new List<DamageNumber>();
        private Camera mainCam;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            mainCam = Camera.main;
        }

        void Update()
        {
            // 更新位置
            for (int i = 0; i < numbers.Count; i++)
            {
                var n = numbers[i];
                n.worldPos += n.velocity * Time.deltaTime;
                n.velocity.y *= 0.95f; // 减速
                n.time += Time.deltaTime;
                numbers[i] = n;
            }

            // 移除过期的
            numbers.RemoveAll(n => n.time >= n.duration);
        }

        void OnGUI()
        {
            if (mainCam == null) return;

            foreach (var n in numbers)
            {
                // 世界坐标转屏幕坐标
                Vector3 screenPos = mainCam.WorldToScreenPoint(n.worldPos + Vector3.up * 2f);
                if (screenPos.z < 0) continue;

                float alpha = 1f - (n.time / n.duration);
                float scale = n.isCritical ? (1f + Mathf.Sin(n.time * 20f) * 0.1f) : 1f;

                GUIStyle style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(n.fontSize * scale),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                Color textColor = n.color;
                textColor.a = alpha;
                style.normal.textColor = textColor;

                // 阴影
                GUIStyle shadowStyle = new GUIStyle(style);
                shadowStyle.normal.textColor = new Color(0, 0, 0, alpha * 0.5f);

                float x = screenPos.x;
                float y = Screen.height - screenPos.y;

                // 描边效果（4方向阴影）
                GUI.Label(new Rect(x - 45, y - 2, 100, 30), n.text, shadowStyle);
                GUI.Label(new Rect(x - 45, y + 2, 100, 30), n.text, shadowStyle);
                GUI.Label(new Rect(x - 49, y, 100, 30), n.text, shadowStyle);
                GUI.Label(new Rect(x - 41, y, 100, 30), n.text, shadowStyle);

                // 主文字
                GUI.Label(new Rect(x - 45, y, 100, 30), n.text, style);
            }
        }

        /// <summary>显示普通伤害</summary>
        public void ShowDamage(Vector3 worldPos, float damage, bool isCritical = false)
        {
            var n = new DamageNumber
            {
                worldPos = worldPos + new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f)),
                text = isCritical ? $"{Mathf.RoundToInt(damage)}!" : Mathf.RoundToInt(damage).ToString(),
                color = isCritical ? new Color(1f, 0.3f, 0f) : Color.white,
                fontSize = isCritical ? 28 : 20,
                time = 0,
                duration = 1.2f,
                velocity = new Vector3(Random.Range(-1f, 1f), 5f, 0),
                isCritical = isCritical,
                isParry = false
            };
            numbers.Add(n);
        }

        /// <summary>显示弹反</summary>
        public void ShowParry(Vector3 worldPos)
        {
            var n = new DamageNumber
            {
                worldPos = worldPos,
                text = "弹反!",
                color = Color.cyan,
                fontSize = 32,
                time = 0,
                duration = 1.5f,
                velocity = new Vector3(0, 6f, 0),
                isCritical = false,
                isParry = true
            };
            numbers.Add(n);
        }

        /// <summary>显示格挡</summary>
        public void ShowBlock(Vector3 worldPos, float reducedDamage)
        {
            var n = new DamageNumber
            {
                worldPos = worldPos,
                text = $"格挡 {Mathf.RoundToInt(reducedDamage)}",
                new Color(0.5f, 0.8f, 1f),
                fontSize = 16,
                time = 0,
                duration = 1f,
                velocity = new Vector3(0, 3f, 0),
                isCritical = false,
                isParry = false
            };
            numbers.Add(n);
        }

        /// <summary>显示治疗</summary>
        public void ShowHeal(Vector3 worldPos, float healAmount)
        {
            var n = new DamageNumber
            {
                worldPos = worldPos,
                text = $"+{Mathf.RoundToInt(healAmount)}",
                color = Color.green,
                fontSize = 20,
                time = 0,
                duration = 1.2f,
                velocity = new Vector3(0, 4f, 0),
                isCritical = false,
                isParry = false
            };
            numbers.Add(n);
        }

        /// <summary>显示击杀</summary>
        public void ShowKill(Vector3 worldPos)
        {
            var n = new DamageNumber
            {
                worldPos = worldPos + Vector3.up,
                text = "击杀!",
                color = new Color(1f, 0.84f, 0f),
                fontSize = 36,
                time = 0,
                duration = 2f,
                velocity = new Vector3(0, 4f, 0),
                isCritical = true,
                isParry = false
            };
            numbers.Add(n);
        }

        /// <summary>显示连击</summary>
        public void ShowCombo(Vector3 worldPos, int comboCount)
        {
            var n = new DamageNumber
            {
                worldPos = worldPos + Vector3.up * 3,
                text = $"{comboCount}连击!",
                color = new Color(1f, 0.5f, 0f),
                fontSize = 24,
                time = 0,
                duration = 1f,
                velocity = new Vector3(0, 3f, 0),
                isCritical = true,
                isParry = false
            };
            numbers.Add(n);
        }
    }
}
