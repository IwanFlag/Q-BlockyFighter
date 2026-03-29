using UnityEngine;
using System.Collections.Generic;

namespace QBlockyFighter.UI
{
    /// <summary>
    /// 战斗通行证系统 - 免费/付费双轨奖励
    /// 赛季制（3个月一赛季），通过经验值升级
    /// </summary>
    public class BattlePassSystem : MonoBehaviour
    {
        public static BattlePassSystem Instance { get; private set; }

        [Header("赛季信息")]
        public string seasonName = "第一章·武者觉醒";
        public int maxLevel = 100;
        public int expPerLevel = 1000;

        // 玩家进度
        public int CurrentLevel { get; private set; } = 1;
        public int CurrentExp { get; private set; }
        public bool IsPremium { get; private set; }
        public List<int> ClaimedFreeRewards { get; private set; } = new();
        public List<int> ClaimedPremiumRewards { get; private set; } = new();

        // 奖励表
        private List<BattlePassReward> freeTrack = new();
        private List<BattlePassReward> premiumTrack = new();

        // UI
        private bool visible = false;

        void Awake()
        {
            Instance = this;
            InitRewards();
        }

        void OnGUI()
        {
            if (!visible) return;

            float cx = Screen.width / 2f;
            float cy = Screen.height / 2f;
            float w = 700;
            float h = 500;

            GUI.Box(new Rect(cx - w/2, cy - h/2, w, h), "");

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = new Color(1f, 0.84f, 0f);
            GUI.Label(new Rect(cx - 200, cy - h/2 + 10, 400, 35), $"战斗通行证 - {seasonName}", titleStyle);

            // 等级进度
            float barW = 400;
            float barH = 20;
            float barX = cx - barW/2;
            float barY = cy - h/2 + 50;
            float progress = CurrentExp / (float)expPerLevel;

            GUI.Box(new Rect(barX, barY, barW, barH), "");
            GUI.DrawTexture(new Rect(barX, barY, barW * progress, barH), MakeTex(new Color(1f, 0.84f, 0f)));

            GUIStyle lvlStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            lvlStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(barX, barY, barW, barH), $"Lv.{CurrentLevel}  ({CurrentExp}/{expPerLevel} EXP)", lvlStyle);

            // 赛季时间
            GUI.Label(new Rect(barX, barY + 25, barW, 18), "赛季剩余: 89天", new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter });

            // 奖励列表
            float rewardY = barY + 55;
            float rewardH = 60;

            for (int i = 0; i < 10 && i + (scrollPage * 10) < maxLevel; i++)
            {
                int level = i + 1 + (scrollPage * 10);
                if (level > maxLevel) break;

                float ry = rewardY + i * (rewardH + 5);

                // 免费奖励
                var freeReward = freeTrack.Count >= level ? freeTrack[level - 1] : null;
                if (freeReward != null)
                {
                    float fx = cx - w/2 + 20;
                    bool claimed = ClaimedFreeRewards.Contains(level);
                    bool canClaim = level <= CurrentLevel && !claimed;

                    GUI.Box(new Rect(fx, ry, 300, rewardH), "");
                    GUI.Label(new Rect(fx + 5, ry + 2, 40, 20), $"Lv{level}", lvlStyle);
                    GUI.Label(new Rect(fx + 50, ry + 5, 40, 30), freeReward.icon, new GUIStyle(GUI.skin.label) { fontSize = 20 });
                    GUI.Label(new Rect(fx + 95, ry + 5, 150, 18), freeReward.name, new GUIStyle(GUI.skin.label) { fontSize = 12 });
                    GUI.Label(new Rect(fx + 95, ry + 25, 150, 14), freeReward.description, new GUIStyle(GUI.skin.label) { fontSize = 10 });

                    if (canClaim)
                    {
                        if (GUI.Button(new Rect(fx + 240, ry + 15, 50, 25), "领取"))
                        {
                            ClaimReward(level, false);
                        }
                    }
                    else if (claimed)
                    {
                        GUI.Label(new Rect(fx + 240, ry + 15, 50, 25), "✅", new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter });
                    }
                }

                // 付费奖励
                var premReward = premiumTrack.Count >= level ? premiumTrack[level - 1] : null;
                if (premReward != null)
                {
                    float px = cx + 20;
                    bool claimed = ClaimedPremiumRewards.Contains(level);
                    bool canClaim = IsPremium && level <= CurrentLevel && !claimed;

                    GUIStyle premStyle = new GUIStyle(GUI.skin.box);
                    premStyle.normal.textColor = new Color(1f, 0.84f, 0f);

                    GUI.Box(new Rect(px, ry, 300, rewardH), "", premStyle);
                    GUI.Label(new Rect(px + 5, ry + 2, 40, 20), $"Lv{level}", lvlStyle);
                    GUI.Label(new Rect(px + 50, ry + 5, 40, 30), premReward.icon, new GUIStyle(GUI.skin.label) { fontSize = 20 });
                    GUI.Label(new Rect(px + 95, ry + 5, 150, 18), premReward.name, new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold });
                    GUI.Label(new Rect(px + 95, ry + 25, 150, 14), premReward.description, new GUIStyle(GUI.skin.label) { fontSize = 10 });

                    if (canClaim)
                    {
                        if (GUI.Button(new Rect(px + 240, ry + 15, 50, 25), "领取"))
                        {
                            ClaimReward(level, true);
                        }
                    }
                    else if (!IsPremium)
                    {
                        GUI.Label(new Rect(px + 240, ry + 15, 50, 25), "🔒", new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter });
                    }
                    else if (claimed)
                    {
                        GUI.Label(new Rect(px + 240, ry + 15, 50, 25), "✅", new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter });
                    }
                }
            }

            // 翻页
            if (scrollPage > 0 && GUI.Button(new Rect(cx - w/2, cy + h/2 - 40, 60, 30), "◀ 上页"))
                scrollPage--;
            if ((scrollPage + 1) * 10 < maxLevel && GUI.Button(new Rect(cx + w/2 - 60, cy + h/2 - 40, 60, 30), "下页 ▶"))
                scrollPage++;

            // 升级付费
            if (!IsPremium && GUI.Button(new Rect(cx - 60, cy + h/2 - 40, 120, 30), "💎 解锁高级"))
            {
                IsPremium = true;
                Debug.Log("[BattlePass] 解锁高级通行证!");
            }

            // 关闭
            if (GUI.Button(new Rect(cx + w/2 - 30, cy - h/2 + 5, 25, 25), "X"))
                visible = false;
        }

        private int scrollPage = 0;

        public void AddExp(int amount)
        {
            CurrentExp += amount;
            while (CurrentExp >= expPerLevel && CurrentLevel < maxLevel)
            {
                CurrentExp -= expPerLevel;
                CurrentLevel++;
                Debug.Log($"[BattlePass] 升级! Lv.{CurrentLevel}");
            }
        }

        public void ClaimReward(int level, bool isPremium)
        {
            if (isPremium)
            {
                if (!ClaimedPremiumRewards.Contains(level))
                {
                    ClaimedPremiumRewards.Add(level);
                    var reward = premiumTrack[level - 1];
                    ApplyReward(reward);
                    Debug.Log($"[BattlePass] 领取高级奖励 Lv.{level}: {reward.name}");
                }
            }
            else
            {
                if (!ClaimedFreeRewards.Contains(level))
                {
                    ClaimedFreeRewards.Add(level);
                    var reward = freeTrack[level - 1];
                    ApplyReward(reward);
                    Debug.Log($"[BattlePass] 领取免费奖励 Lv.{level}: {reward.name}");
                }
            }
        }

        private void ApplyReward(BattlePassReward reward)
        {
            switch (reward.type)
            {
                case RewardType.Skin:
                    Debug.Log($"  获得皮肤: {reward.data}");
                    break;
                case RewardType.WeaponSkin:
                    Debug.Log($"  获得武器外观: {reward.data}");
                    break;
                case RewardType.Coins:
                    Debug.Log($"  获得金币: {reward.data}");
                    break;
                case RewardType.Exp:
                    AddExp(int.Parse(reward.data));
                    break;
                case RewardType.KillEffect:
                    Debug.Log($"  获得击杀特效: {reward.data}");
                    break;
                case RewardType.Emote:
                    Debug.Log($"  获得表情: {reward.data}");
                    break;
            }
        }

        private void InitRewards()
        {
            // 免费轨道
            for (int i = 0; i < maxLevel; i++)
            {
                int lv = i + 1;
                if (lv % 10 == 0)
                    freeTrack.Add(new BattlePassReward { name = $"宝箱·{lv}", icon = "🎁", type = RewardType.Coins, data = "500", description = "500金币" });
                else if (lv % 5 == 0)
                    freeTrack.Add(new BattlePassReward { name = "金币", icon = "💰", type = RewardType.Coins, data = "100", description = "100金币" });
                else
                    freeTrack.Add(new BattlePassReward { name = "经验", icon = "⭐", type = RewardType.Exp, data = "200", description = "+200 EXP" });
            }

            // 付费轨道
            for (int i = 0; i < maxLevel; i++)
            {
                int lv = i + 1;
                if (lv == 1)
                    premiumTrack.Add(new BattlePassReward { name = "武者觉醒·皮肤", icon = "🔥", type = RewardType.Skin, data = "awakening_skin", description = "赛季限定皮肤" });
                else if (lv == 50)
                    premiumTrack.Add(new BattlePassReward { name = "黄金武器外观", icon = "⚔️", type = RewardType.WeaponSkin, data = "gold_weapon", description = "传说级武器外观" });
                else if (lv == 100)
                    premiumTrack.Add(new BattlePassReward { name = "无极段位特效", icon = "✨", type = RewardType.KillEffect, data = "supreme_effect", description = "传说击杀特效" });
                else if (lv % 10 == 0)
                    premiumTrack.Add(new BattlePassReward { name = $"史诗宝箱", icon = "🎁", type = RewardType.Coins, data = "1000", description = "1000金币" });
                else if (lv % 5 == 0)
                    premiumTrack.Add(new BattlePassReward { name = "表情", icon = "😎", type = RewardType.Emote, data = $"emote_{lv}", description = "限定表情" });
                else
                    premiumTrack.Add(new BattlePassReward { name = "金币", icon = "💰", type = RewardType.Coins, data = "200", description = "200金币" });
            }
        }

        public void Toggle() => visible = !visible;

        private Texture2D tex;
        private Texture2D MakeTex(Color color)
        {
            if (tex != null) return tex;
            tex = new Texture2D(2, 2);
            Color[] pix = { color, color, color, color };
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }
    }

    // ===== 数据结构 =====

    public enum RewardType { Skin, WeaponSkin, Coins, Exp, KillEffect, Emote }

    [System.Serializable]
    public class BattlePassReward
    {
        public string name;
        public string icon;
        public RewardType type;
        public string data;
        public string description;
    }
}
