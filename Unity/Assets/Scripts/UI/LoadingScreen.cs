using UnityEngine;
using System.Collections.Generic;

namespace QBlockyFighter.UI
{
    /// <summary>
    /// 加载/过渡屏幕 - 场景切换、游戏开始倒计时
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        public static LoadingScreen Instance { get; private set; }

        private bool isLoading;
        private float loadProgress;
        private string loadTip = "";
        private float fadeAlpha = 0;
        private float fadeSpeed = 2f;
        private bool fadeIn = true;

        // 倒计时
        private int countdown = 0;
        private float countdownTimer;

        // 背景
        private Texture2D bgTex;

        // 提示列表
        private static readonly string[] Tips = {
            "💡 弹反可以完全免疫伤害并反击",
            "💡 切换武器后首击伤害+20%",
            "💡 体力不足时无法闪避和格挡",
            "💡 每个角色有独特的被动技能",
            "💡 霸体可以抵抗击飞效果",
            "💡 连招中插入重攻击可以延长连击",
            "💡 击杀敌人可以回复50%血量",
            "💡 训练场可以练习所有武器的连招",
            "💡 5V5模式中注意占领据点获得优势",
            "💡 Boss在10分钟时刷新在地图中央",
        };

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, Color.black);
            bgTex.Apply();
        }

        void Update()
        {
            if (isLoading)
            {
                loadProgress = Mathf.MoveTowards(loadProgress, 1f, Time.deltaTime * 0.5f);
                if (loadProgress >= 1f)
                {
                    isLoading = false;
                }
            }

            if (!fadeIn)
            {
                fadeAlpha = Mathf.MoveTowards(fadeAlpha, 0f, fadeSpeed * Time.deltaTime);
            }
            else
            {
                fadeAlpha = Mathf.MoveTowards(fadeAlpha, 1f, fadeSpeed * Time.deltaTime);
            }

            if (countdown > 0)
            {
                countdownTimer -= Time.deltaTime;
                if (countdownTimer <= 0)
                {
                    countdown--;
                    if (countdown > 0)
                    {
                        countdownTimer = 1f;
                    }
                }
            }
        }

        void OnGUI()
        {
            // 淡入淡出遮罩
            if (fadeAlpha > 0)
            {
                GUI.color = new Color(0, 0, 0, fadeAlpha);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), bgTex);
                GUI.color = Color.white;
            }

            // 加载界面
            if (isLoading)
            {
                GUI.color = new Color(0, 0, 0, 0.9f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), bgTex);
                GUI.color = Color.white;

                float cx = Screen.width / 2f;
                float cy = Screen.height / 2f;

                // 标题
                GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                titleStyle.normal.textColor = new Color(1f, 0.84f, 0f);
                GUI.Label(new Rect(cx - 200, cy - 80, 400, 50), "Q版方块人大乱斗", titleStyle);

                // 进度条
                float barW = 400;
                float barH = 20;
                GUI.Box(new Rect(cx - barW / 2, cy, barW, barH), "");
                GUI.DrawTexture(new Rect(cx - barW / 2, cy, barW * loadProgress, barH), MakeBarTex());

                GUIStyle percentStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
                percentStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(cx - barW / 2, cy, barW, barH), $"{Mathf.RoundToInt(loadProgress * 100)}%", percentStyle);

                // 提示
                if (!string.IsNullOrEmpty(loadTip))
                {
                    GUIStyle tipStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
                    tipStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
                    GUI.Label(new Rect(cx - 250, cy + 40, 500, 30), loadTip, tipStyle);
                }
            }

            // 倒计时
            if (countdown > 0)
            {
                float cx = Screen.width / 2f;
                float cy = Screen.height / 2f;

                GUIStyle countStyle = new GUIStyle(GUI.skin.label) { fontSize = 120, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                countStyle.normal.textColor = countdown <= 3 ? Color.red : new Color(1f, 0.84f, 0f);

                // 缩放动画效果
                float scale = 1f + (1f - (countdownTimer % 1f)) * 0.3f;
                GUI.matrix = Matrix4x4.TRS(new Vector3(cx, cy, 0), Quaternion.identity, Vector3.one * scale);
                GUI.Label(new Rect(-100, -80, 200, 160), countdown.ToString(), countStyle);
                GUI.matrix = Matrix4x4.identity;
            }
        }

        /// <summary>显示加载界面</summary>
        public void ShowLoading()
        {
            isLoading = true;
            loadProgress = 0;
            loadTip = Tips[Random.Range(0, Tips.Length)];
        }

        /// <summary>隐藏加载界面</summary>
        public void HideLoading()
        {
            isLoading = false;
            loadProgress = 1f;
        }

        /// <summary>开始倒计时</summary>
        public void StartCountdown(int seconds = 3)
        {
            countdown = seconds;
            countdownTimer = 1f;
        }

        /// <summary>淡入（变黑）</summary>
        public void FadeIn(float speed = 2f)
        {
            fadeIn = true;
            fadeSpeed = speed;
            fadeAlpha = 0;
        }

        /// <summary>淡出（变亮）</summary>
        public void FadeOut(float speed = 2f)
        {
            fadeIn = false;
            fadeSpeed = speed;
            fadeAlpha = 1f;
        }

        /// <summary>快速淡入淡出</summary>
        public void QuickFade(float duration = 0.5f)
        {
            StartCoroutine(QuickFadeCoroutine(duration));
        }

        private System.Collections.IEnumerator QuickFadeCoroutine(float duration)
        {
            FadeIn(1f / (duration / 2f));
            yield return new WaitForSeconds(duration / 2f);
            FadeOut(1f / (duration / 2f));
        }

        private Texture2D barTex;
        private Texture2D MakeBarTex()
        {
            if (barTex != null) return barTex;
            barTex = new Texture2D(2, 2);
            Color[] pix = { new Color(1f, 0.84f, 0f), new Color(1f, 0.84f, 0f), new Color(1f, 0.84f, 0f), new Color(1f, 0.84f, 0f) };
            barTex.SetPixels(pix);
            barTex.Apply();
            return barTex;
        }
    }
}
