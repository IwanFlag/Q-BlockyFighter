using UnityEngine;
using QBlockyFighter.Core;

namespace QBlockyFighter.UI
{
    public class HUDManager : MonoBehaviour
    {
        private HealthSystem playerHealth;
        private SkillSystem playerSkills;
        private WeaponSystem playerWeapons;
        private CombatSystem playerCombat;

        private int comboCount;
        private float comboTimer;
        private bool showParryFlash;
        private float parryFlashTimer;
        private string statusMessage = "";
        private float statusTimer;

        public void Initialize(HealthSystem hp, SkillSystem skills, WeaponSystem weapons, CombatSystem combat)
        {
            playerHealth = hp;
            playerSkills = skills;
            playerWeapons = weapons;
            playerCombat = combat;
        }

        void Update()
        {
            if (comboTimer > 0)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0) comboCount = 0;
            }
            if (parryFlashTimer > 0) parryFlashTimer -= Time.deltaTime;
            if (statusTimer > 0) statusTimer -= Time.deltaTime;
        }

        void OnGUI()
        {
            if (playerHealth == null) return;

            GUIStyle hpLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            hpLabelStyle.normal.textColor = Color.white;

            // === 左下角：血条/体力条 ===
            float barX = 30;
            float barY = Screen.height - 80;

            // 角色头像框
            GUI.Box(new Rect(barX, barY - 10, 50, 50), "");
            GUIStyle iconStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(barX, barY - 10, 50, 50), "⚔", iconStyle);

            // 血条
            float barW = 220;
            float barH = 18;
            float barX2 = barX + 60;

            DrawBar(barX2, barY, barW, barH, playerHealth.HpPercent, new Color(1f, 0.27f, 0.27f), $"HP: {Mathf.CeilToInt(playerHealth.CurrentHp)}/{Mathf.CeilToInt(playerHealth.MaxHp)}");

            // 体力条
            float stamina = playerCombat != null ? playerCombat.StaminaPercent : 1f;
            DrawBar(barX2, barY + 22, barW, barH, stamina, new Color(0.27f, 0.67f, 0.27f), $"体力: {Mathf.CeilToInt(stamina * 100)}%");

            // 体力不足警告
            if (stamina < 0.2f)
            {
                GUIStyle warnStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
                warnStyle.normal.textColor = Color.red;
                GUI.Label(new Rect(barX2, barY + 44, barW, 15), "⚠ 体力不足!", warnStyle);
            }

            // === 右下角：技能栏 ===
            if (playerSkills != null)
            {
                float skillX = Screen.width - 260;
                float skillY = Screen.height - 80;
                string[] keys = { "Q", "E", "R", "P" };
                string[] icons = { "💥", "🌀", "⚡", "✨" };

                for (int i = 0; i < 4; i++)
                {
                    float sx = skillX + i * 60;
                    bool onCD = playerSkills.IsOnCooldown(i);
                    float cd = playerSkills.GetCooldownRemaining(i);

                    GUI.Box(new Rect(sx, skillY, 52, 52), "");
                    GUI.Label(new Rect(sx + 2, skillY + 2, 15, 15), keys[i], hpLabelStyle);
                    GUIStyle skillIcon = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
                    GUI.Label(new Rect(sx, skillY + 5, 52, 45), icons[i], skillIcon);

                    if (onCD)
                    {
                        GUI.Box(new Rect(sx, skillY, 52, 52), "");
                        GUIStyle cdStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                        cdStyle.normal.textColor = Color.white;
                        GUI.Label(new Rect(sx, skillY, 52, 52), $"{cd:F1}s", cdStyle);
                    }
                }
            }

            // === 底部：武器栏 ===
            if (playerWeapons != null)
            {
                float wpnX = Screen.width / 2f - 70;
                float wpnY = Screen.height - 130;
                string[] wpnIcons = { "🔪", "⚔" };

                for (int i = 0; i < 2; i++)
                {
                    bool active = i == playerWeapons.CurrentWeaponIndex;
                    GUIStyle wpnStyle = new GUIStyle(GUI.skin.box);
                    if (active)
                        wpnStyle.normal.textColor = new Color(1f, 0.84f, 0f);

                    GUI.Box(new Rect(wpnX + i * 70, wpnY, 64, 48), "");
                    GUIStyle wpnIconStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
                    GUI.Label(new Rect(wpnX + i * 70, wpnY, 64, 35), wpnIcons[i], wpnIconStyle);
                    GUIStyle wpnNameStyle = new GUIStyle(GUI.skin.label) { fontSize = 9, alignment = TextAnchor.MiddleCenter };
                    wpnNameStyle.normal.textColor = active ? new Color(1f, 0.84f, 0f) : new Color(1f, 1f, 1f, 0.5f);
                    GUI.Label(new Rect(wpnX + i * 70, wpnY + 32, 64, 12), active ? "主武器" : "副武器", wpnNameStyle);
                }
            }

            // === 连击显示 ===
            if (comboCount > 1)
            {
                GUIStyle comboStyle = new GUIStyle(GUI.skin.label) { fontSize = 48, alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Bold };
                comboStyle.normal.textColor = new Color(1f, 0.84f, 0f);
                GUI.Label(new Rect(Screen.width - 200, Screen.height / 2f - 30, 170, 60), $"{comboCount}", comboStyle);
                GUIStyle comboLabel = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleRight };
                comboLabel.normal.textColor = new Color(1f, 0.84f, 0f, 0.7f);
                GUI.Label(new Rect(Screen.width - 200, Screen.height / 2f + 20, 170, 20), "连击!", comboLabel);
            }

            // === 弹反闪光 ===
            if (showParryFlash && parryFlashTimer > 0)
            {
                GUIStyle parryStyle = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                parryStyle.normal.textColor = Color.cyan;
                GUI.Label(new Rect(Screen.width / 2f - 100, Screen.height / 2f - 20, 200, 40), "弹反!", parryStyle);
            }

            // === 状态消息 ===
            if (statusTimer > 0 && !string.IsNullOrEmpty(statusMessage))
            {
                GUIStyle statusStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                statusStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(Screen.width / 2f - 200, Screen.height / 2f - 20, 400, 40), statusMessage, statusStyle);
            }

            // === 左上角：小地图 ===
            GUI.Box(new Rect(20, 20, 140, 140), "");
            GUIStyle minimapLabel = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleCenter };
            minimapLabel.normal.textColor = new Color(1f, 1f, 1f, 0.3f);
            GUI.Label(new Rect(20, 80, 140, 20), "小地图", minimapLabel);

            // === 中上：击杀播报 ===
            // kill feed 通过 ShowKillFeed 方法管理

            // === 右上角：操作帮助 ===
            if (Input.GetKey(KeyCode.F1))
            {
                GUI.Box(new Rect(Screen.width - 200, 20, 180, 160), "");
                GUIStyle helpTitle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
                helpTitle.normal.textColor = new Color(1f, 0.84f, 0f);
                GUI.Label(new Rect(Screen.width - 190, 25, 160, 20), "操作帮助", helpTitle);
                GUI.Label(new Rect(Screen.width - 190, 50, 160, 120),
                    "WASD - 移动\n鼠标 - 视角\n左键 - 轻攻击\n右键 - 重攻击\nShift - 闪避\n空格 - 格挡\nQ/E/R - 技能\n1/2 - 切换武器");
            }
        }

        private void DrawBar(float x, float y, float w, float h, float percent, Color color, string text)
        {
            // 背景
            GUI.Box(new Rect(x, y, w, h), "");
            // 填充
            GUI.DrawTexture(new Rect(x, y, w * Mathf.Clamp01(percent), h), MakeTex(2, 2, color));
            // 文字
            GUIStyle textStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            textStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y, w, h), text, textStyle);
        }

        private Texture2D tex;
        private Texture2D MakeTex(int width, int height, Color color)
        {
            if (tex != null) return tex;
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = color;
            tex = new Texture2D(width, height);
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        public void AddCombo()
        {
            comboCount++;
            comboTimer = 2f;
        }

        public void ResetCombo()
        {
            comboCount = 0;
        }

        public void ShowParryFlash()
        {
            showParryFlash = true;
            parryFlashTimer = 1f;
        }

        public void ShowStatus(string msg, float duration = 2f)
        {
            statusMessage = msg;
            statusTimer = duration;
        }
    }
}
