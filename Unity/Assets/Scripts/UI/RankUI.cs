using UnityEngine;

namespace QBlockyFighter.UI
{
    public class RankUI : MonoBehaviour
    {
        private bool visible = false;

        private static readonly string[] TierNames = { "初悟", "凝心", "破障", "通玄", "化境", "归真", "无极" };
        private static readonly string[] TierIcons = { "🌱", "💎", "⚔️", "🔮", "👑", "🌟", "♾️" };

        private int currentTier = 0;
        private int currentPoints = 0;
        private int wins = 0;
        private int losses = 0;
        private int winStreak = 0;

        void OnGUI()
        {
            if (!visible) return;

            float cx = Screen.width / 2f;
            float cy = Screen.height / 2f;

            GUI.Box(new Rect(cx - 250, cy - 200, 500, 400), "");

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = new Color(1f, 0.84f, 0f);
            GUI.Label(new Rect(cx - 150, cy - 195, 300, 35), "排位信息", titleStyle);

            // 当前段位
            GUIStyle tierIconStyle = new GUIStyle(GUI.skin.label) { fontSize = 48, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(cx - 30, cy - 140, 60, 60), TierIcons[Mathf.Min(currentTier, 6)], tierIconStyle);

            GUIStyle tierNameStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            tierNameStyle.normal.textColor = GetTierColor(currentTier);
            GUI.Label(new Rect(cx - 100, cy - 75, 200, 35), TierNames[Mathf.Min(currentTier, 6)], tierNameStyle);

            // 分数
            GUIStyle pointsStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            pointsStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
            GUI.Label(new Rect(cx - 100, cy - 40, 200, 25), $"{currentPoints} LP", pointsStyle);

            // 进度条
            float barX = cx - 150;
            float barY = cy - 10;
            float barW = 300;
            float barH = 12;
            GUI.Box(new Rect(barX, barY, barW, barH), "");
            float progress = currentPoints / 100f;
            GUI.DrawTexture(new Rect(barX, barY, barW * progress, barH), MakeBarTex());

            // 段位区间
            GUI.Label(new Rect(barX, barY + 15, 50, 15), TierNames[Mathf.Min(currentTier, 6)]);
            if (currentTier < 6)
                GUI.Label(new Rect(barX + barW - 50, barY + 15, 50, 15), TierNames[currentTier + 1]);

            // 战绩统计
            float statY = cy + 40;
            GUIStyle statLabel = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            statLabel.normal.textColor = new Color(1f, 1f, 1f, 0.8f);

            GUI.Label(new Rect(cx - 120, statY, 120, 25), "胜场:", statLabel);
            GUI.Label(new Rect(cx, statY, 80, 25), $"{wins}", statLabel);

            GUI.Label(new Rect(cx - 120, statY + 30, 120, 25), "败场:", statLabel);
            GUI.Label(new Rect(cx, statY + 30, 80, 25), $"{losses}", statLabel);

            GUI.Label(new Rect(cx - 120, statY + 60, 120, 25), "胜率:", statLabel);
            float totalGames = wins + losses;
            float winRate = totalGames > 0 ? wins / totalGames * 100f : 0;
            GUI.Label(new Rect(cx, statY + 60, 80, 25), $"{winRate:F1}%", statLabel);

            GUI.Label(new Rect(cx - 120, statY + 90, 120, 25), "连胜:", statLabel);
            GUIStyle streakStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            streakStyle.normal.textColor = winStreak >= 3 ? new Color(1f, 0.5f, 0f) : Color.white;
            GUI.Label(new Rect(cx, statY + 90, 80, 25), $"{winStreak}", streakStyle);

            // 关闭
            if (GUI.Button(new Rect(cx - 50, cy + 160, 100, 30), "关闭"))
            {
                visible = false;
            }
        }

        private Color GetTierColor(int tier)
        {
            return tier switch
            {
                0 => new Color(0.7f, 0.7f, 0.7f),
                1 => new Color(0.5f, 0.8f, 0.5f),
                2 => new Color(0.3f, 0.7f, 1f),
                3 => new Color(0.6f, 0.3f, 1f),
                4 => new Color(1f, 0.3f, 0.3f),
                5 => new Color(1f, 0.84f, 0f),
                6 => new Color(1f, 0.5f, 0f),
                _ => Color.white
            };
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

        public void UpdateRank(int tier, int points, int w, int l, int streak)
        {
            currentTier = tier;
            currentPoints = points;
            wins = w;
            losses = l;
            winStreak = streak;
        }

        public void UpdateAfterMatch(bool won)
        {
            if (won)
            {
                wins++;
                winStreak++;
                int gain = 25 + (winStreak >= 3 ? 5 : 0);
                currentPoints += gain;
                if (currentPoints >= 100 && currentTier < 6)
                {
                    currentPoints -= 100;
                    currentTier++;
                }
            }
            else
            {
                losses++;
                winStreak = 0;
                currentPoints = Mathf.Max(0, currentPoints - 20);
                if (currentPoints == 0 && currentTier > 0)
                {
                    currentTier--;
                    currentPoints = 80;
                }
            }
        }

        public void Toggle() => visible = !visible;
        public void Show() => visible = true;
        public string GetTierName() => TierNames[Mathf.Min(currentTier, 6)];
    }
}
