using UnityEngine;
using System.Collections.Generic;

namespace QBlockyFighter.UI
{
    public class ShopUI : MonoBehaviour
    {
        private bool visible = false;
        private int selectedCategory = 0;
        private int playerCoins = 0;

        private string[] categories = { "皮肤", "武器外观", "击杀特效", "表情" };
        private List<ShopItem> items = new List<ShopItem>();

        public struct ShopItem
        {
            public string name;
            public string icon;
            public int price;
            public string category;
            public string rarity;
            public bool owned;
        }

        void Start()
        {
            InitShopItems();
        }

        void OnGUI()
        {
            if (!visible) return;

            float cx = Screen.width / 2f;
            float cy = Screen.height / 2f;

            // 背景
            GUI.Box(new Rect(cx - 350, cy - 250, 700, 500), "");

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = new Color(1f, 0.84f, 0f);
            GUI.Label(new Rect(cx - 150, cy - 245, 300, 35), "商城", titleStyle);

            // 金币显示
            GUIStyle coinStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleRight };
            coinStyle.normal.textColor = new Color(1f, 0.84f, 0f);
            GUI.Label(new Rect(cx + 150, cy - 242, 180, 25), $"💰 {playerCoins}", coinStyle);

            // 分类标签
            float tabX = cx - 330;
            for (int i = 0; i < categories.Length; i++)
            {
                GUIStyle tabStyle = new GUIStyle(GUI.skin.button) { fontSize = 13 };
                if (i == selectedCategory)
                {
                    tabStyle.fontStyle = FontStyle.Bold;
                    tabStyle.normal.textColor = new Color(1f, 0.84f, 0f);
                }
                if (GUI.Button(new Rect(tabX + i * 100, cy - 200, 95, 30), categories[i], tabStyle))
                {
                    selectedCategory = i;
                }
            }

            // 商品列表
            float itemY = cy - 160;
            float itemX = cx - 330;
            int col = 0;
            int row = 0;

            foreach (var item in items)
            {
                if (item.category != categories[selectedCategory]) continue;

                float ix = itemX + col * 165;
                float iy = itemY + row * 120;

                GUI.Box(new Rect(ix, iy, 155, 110), "");

                // 图标
                GUIStyle iconStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(ix, iy + 5, 155, 40), item.icon, iconStyle);

                // 名称
                GUIStyle nameStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                nameStyle.normal.textColor = GetRarityColor(item.rarity);
                GUI.Label(new Rect(ix, iy + 45, 155, 18), item.name, nameStyle);

                // 稀有度
                GUI.Label(new Rect(ix, iy + 63, 155, 14), item.rarity, new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleCenter });

                // 购买按钮
                if (item.owned)
                {
                    GUIStyle ownedStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
                    ownedStyle.normal.textColor = new Color(0.5f, 1f, 0.5f);
                    GUI.Label(new Rect(ix + 30, iy + 82, 95, 22), "已拥有", ownedStyle);
                }
                else
                {
                    if (GUI.Button(new Rect(ix + 30, iy + 82, 95, 22), $"💰 {item.price}"))
                    {
                        BuyItem(item);
                    }
                }

                col++;
                if (col >= 4) { col = 0; row++; }
            }

            // 关闭按钮
            if (GUI.Button(new Rect(cx + 300, cy - 248, 40, 28), "X"))
            {
                visible = false;
            }
        }

        private void InitShopItems()
        {
            items.Clear();
            // 皮肤
            items.Add(new ShopItem { name = "黄金武士", icon = "⚔️", price = 1680, category = "皮肤", rarity = "传说", owned = false });
            items.Add(new ShopItem { name = "暗影忍者", icon = "🥷", price = 980, category = "皮肤", rarity = "史诗", owned = false });
            items.Add(new ShopItem { name = "冰霜骑士", icon = "❄️", price = 480, category = "皮肤", rarity = "稀有", owned = false });
            items.Add(new ShopItem { name = "火焰将军", icon = "🔥", price = 480, category = "皮肤", rarity = "稀有", owned = false });

            // 武器外观
            items.Add(new ShopItem { name = "光剑", icon = "🗡️", price = 880, category = "武器外观", rarity = "史诗", owned = false });
            items.Add(new ShopItem { name = "雷神锤", icon = "🔨", price = 680, category = "武器外观", rarity = "史诗", owned = false });
            items.Add(new ShopItem { name = "冰弓", icon = "🏹", price = 380, category = "武器外观", rarity = "稀有", owned = false });

            // 击杀特效
            items.Add(new ShopItem { name = "烟花绽放", icon = "🎆", price = 580, category = "击杀特效", rarity = "史诗", owned = false });
            items.Add(new ShopItem { name = "星辰坠落", icon = "⭐", price = 380, category = "击杀特效", rarity = "稀有", owned = false });

            // 表情
            items.Add(new ShopItem { name = "胜利之舞", icon = "💃", price = 280, category = "表情", rarity = "稀有", owned = false });
            items.Add(new ShopItem { name = "嘲讽", icon = "😎", price = 180, category = "表情", rarity = "普通", owned = false });
        }

        private Color GetRarityColor(string rarity)
        {
            return rarity switch
            {
                "传说" => new Color(1f, 0.5f, 0f),
                "史诗" => new Color(0.6f, 0.2f, 1f),
                "稀有" => new Color(0.2f, 0.6f, 1f),
                _ => Color.white
            };
        }

        private void BuyItem(ShopItem item)
        {
            if (playerCoins >= item.price)
            {
                playerCoins -= item.price;
                // 标记为已拥有
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].name == item.name)
                    {
                        var updated = items[i];
                        updated.owned = true;
                        items[i] = updated;
                        break;
                    }
                }
                Debug.Log($"[Shop] 购买成功: {item.name}");
            }
            else
            {
                Debug.Log("[Shop] 金币不足!");
            }
        }

        public void Toggle()
        {
            visible = !visible;
        }

        public void Show() => visible = true;
        public void Hide() => visible = false;
    }
}
