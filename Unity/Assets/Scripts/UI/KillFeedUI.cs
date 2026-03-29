using UnityEngine;
using System.Collections.Generic;

namespace QBlockyFighter.UI
{
    /// <summary>
    /// 击杀播报系统 - 屏幕上方滚动显示击杀信息
    /// </summary>
    public class KillFeedUI : MonoBehaviour
    {
        private struct KillEntry
        {
            public string killer;
            public string victim;
            public string weapon;
            public float time;
            public Color killerColor;
        }

        private List<KillEntry> entries = new List<KillEntry>();
        private int maxEntries = 5;
        private float displayDuration = 5f;

        // 团战分数显示
        private int teamAScore;
        private int teamBScore;
        private bool showTeamScore = false;

        void OnGUI()
        {
            if (entries.Count == 0 && !showTeamScore) return;

            float feedX = Screen.width / 2f - 200;
            float feedY = 10;

            // 团战分数（居中上方）
            if (showTeamScore)
            {
                GUIStyle scoreStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                scoreStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(Screen.width / 2f - 100, feedY, 200, 35), $"{teamAScore} - {teamBScore}", scoreStyle);
                feedY += 45;
            }

            // 击杀播报
            float entryH = 28;
            entries.RemoveAll(e => Time.time - e.time > displayDuration);

            for (int i = 0; i < entries.Count; i++)
            {
                float alpha = 1f - ((Time.time - entries[i].time) / displayDuration);
                alpha = Mathf.Clamp01(alpha);

                float y = feedY + i * (entryH + 4);

                // 背景
                GUI.color = new Color(0, 0, 0, 0.5f * alpha);
                GUI.Box(new Rect(feedX, y, 400, entryH), "");
                GUI.color = Color.white;

                // 击杀信息
                GUIStyle killerStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
                killerStyle.normal.textColor = new Color(entries[i].killerColor.r, entries[i].killerColor.g, entries[i].killerColor.b, alpha);

                GUIStyle victimStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleLeft };
                victimStyle.normal.textColor = new Color(1f, 0.3f, 0.3f, alpha);

                GUIStyle iconStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
                iconStyle.normal.textColor = new Color(1f, 1f, 1f, alpha);

                // 格式: [杀手] ⚔️ [武器] → [受害者]
                GUI.Label(new Rect(feedX + 10, y, 120, entryH), entries[i].killer, killerStyle);
                GUI.Label(new Rect(feedX + 135, y, 30, entryH), "⚔", iconStyle);

                string weaponText = string.IsNullOrEmpty(entries[i].weapon) ? "" : $"({entries[i].weapon})";
                GUIStyle weaponStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleCenter };
                weaponStyle.normal.textColor = new Color(1f, 1f, 0.5f, alpha * 0.7f);
                GUI.Label(new Rect(feedX + 165, y, 60, entryH), weaponText, weaponStyle);

                GUI.Label(new Rect(feedX + 230, y, 160, entryH), entries[i].victim, victimStyle);
            }
        }

        /// <summary>添加击杀播报</summary>
        public void AddKill(string killer, string victim, string weapon = "", Color killerColor = default)
        {
            if (killerColor == default) killerColor = Color.white;

            entries.Add(new KillEntry
            {
                killer = killer,
                victim = victim,
                weapon = weapon,
                time = Time.time,
                killerColor = killerColor
            });

            if (entries.Count > maxEntries)
            {
                entries.RemoveAt(0);
            }
        }

        /// <summary>更新团战分数</summary>
        public void SetTeamScore(int teamA, int teamB)
        {
            teamAScore = teamA;
            teamBScore = teamB;
            showTeamScore = true;
        }

        /// <summary>添加系统播报（如"玩家已连接""BOSS刷新"）</summary>
        public void AddSystemMessage(string message)
        {
            AddKill("[系统]", message, "", new Color(1f, 0.84f, 0f));
        }

        public void Clear()
        {
            entries.Clear();
            showTeamScore = false;
        }
    }
}
