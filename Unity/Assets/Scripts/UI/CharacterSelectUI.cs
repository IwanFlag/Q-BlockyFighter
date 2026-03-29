using UnityEngine;
using QBlockyFighter.Core;

namespace QBlockyFighter.UI
{
    public class CharacterSelectUI : MonoBehaviour
    {
        private bool visible = false;
        private string gameMode = "";
        private int selectedChar = 0;
        private int selectedMap = 0;
        private bool confirmed = false;

        public event System.Action<int, int> OnCharacterConfirmed; // charIndex, mapIndex

        // 角色信息（与CharacterData一致）
        private static readonly string[] CharNames = {
            "影忍·雾隐", "冷月·荆如", "铁骑·哲别", "傲骨·关云",
            "雷鸣·卡利", "磐石·奥拉夫", "铁壁·马克西", "追风·花木兰",
            "毒蝎·哈桑", "破军·李舜臣", "圣歌·阿玛尼", "天机·达芬奇"
        };

        private static readonly string[] CharRoles = {
            "刺客", "刺客", "战士", "战士", "战士", "坦克",
            "坦克", "远程", "远程", "远程", "辅助", "辅助"
        };

        private static readonly string[] CharSystems = {
            "巫术", "武术", "武术", "武术", "魔法", "仙术",
            "武术", "仙术", "巫术", "魔法", "巫术", "魔法"
        };

        private static readonly string[] CharIcons = {
            "🥷", "🗡️", "🏇", "🐉", "⚡", "🛡️",
            "🏛️", "🏹", "🦂", "💣", "🎵", "🔧"
        };

        private static readonly string[] MapNames = {
            "龙城演武", "罗马竞技场", "竹林幽径",
            "北海冰原", "长安城", "波斯花园"
        };

        private static readonly string[] SystemClasses = {
            "system-wushu2", "system-wushu", "system-wushu", "system-wushu",
            "system-magic", "system-xianshu", "system-wushu", "system-xianshu",
            "system-wushu2", "system-magic", "system-wushu2", "system-magic"
        };

        public void Show(string mode)
        {
            gameMode = mode;
            visible = true;
            confirmed = false;
            selectedChar = 0;
            selectedMap = 0;
        }

        public void Hide()
        {
            visible = false;
        }

        void OnGUI()
        {
            if (!visible) return;

            // 全屏黑色背景
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = new Color(1f, 0.84f, 0f);

            GUIStyle charNameStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            charNameStyle.normal.textColor = Color.white;

            GUIStyle charRoleStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter };
            charRoleStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
            btnStyle.normal.textColor = new Color(1f, 0.84f, 0f);

            float cx = Screen.width / 2f;
            float cy = Screen.height / 2f;

            GUI.Label(new Rect(cx - 200, 30, 400, 50), "选择你的武者", titleStyle);
            GUI.Label(new Rect(cx - 100, 80, 200, 20), $"模式: {gameMode}");

            // 角色网格 4列x3行
            float gridStartX = cx - 400;
            float gridStartY = 110;
            float cardW = 185;
            float cardH = 150;
            float gap = 12;

            for (int i = 0; i < 12; i++)
            {
                int col = i % 4;
                int row = i / 4;
                float x = gridStartX + col * (cardW + gap);
                float y = gridStartY + row * (cardH + gap);

                bool isSelected = i == selectedChar;
                GUIStyle cardStyle = new GUIStyle(GUI.skin.box);
                if (isSelected)
                {
                    cardStyle.normal.background = MakeTex(2, 2, new Color(1f, 0.84f, 0f, 0.2f));
                }

                GUI.Box(new Rect(x, y, cardW, cardH), "", cardStyle);

                if (isSelected)
                {
                    GUI.DrawTexture(new Rect(x, y, cardW, cardH), MakeBorder(new Color(1f, 0.84f, 0f, 0.8f)));
                }

                // 角色图标
                GUIStyle iconStyle = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(x, y + 5, cardW, 50), CharIcons[i], iconStyle);

                // 名称
                GUI.Label(new Rect(x, y + 55, cardW, 20), CharNames[i], charNameStyle);

                // 定位
                GUI.Label(new Rect(x, y + 75, cardW, 15), CharRoles[i], charRoleStyle);

                // 力量体系
                GUIStyle sysStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleCenter };
                sysStyle.normal.textColor = GetSystemColor(CharSystems[i]);
                GUI.Label(new Rect(x, y + 92, cardW, 15), CharSystems[i], sysStyle);

                // 描述
                string desc = GetCharDesc(i);
                GUIStyle descStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.UpperCenter, wordWrap = true };
                descStyle.normal.textColor = new Color(1f, 1f, 1f, 0.4f);
                GUI.Label(new Rect(x + 5, y + 108, cardW - 10, 40), desc, descStyle);

                // 点击检测
                if (GUI.Button(new Rect(x, y, cardW, cardH), "", GUIStyle.none))
                {
                    selectedChar = i;
                }
            }

            // 地图选择（1V1和5V5不同）
            float mapY = gridStartY + 3 * (cardH + gap) + 15;
            GUI.Label(new Rect(cx - 100, mapY, 200, 25), "选择地图", charNameStyle);

            int mapCount = gameMode == "5v5" ? 3 : 3;
            string[] maps = gameMode == "5v5"
                ? new[] { "北海冰原", "长安城", "波斯花园" }
                : new[] { "龙城演武", "罗马竞技场", "竹林幽径" };

            float mapBtnW = 150;
            float mapStartX = cx - (mapCount * (mapBtnW + 8)) / 2f;
            for (int i = 0; i < mapCount; i++)
            {
                bool mapSelected = i == selectedMap;
                GUIStyle mapBtn = new GUIStyle(GUI.skin.button) { fontSize = 13 };
                if (mapSelected)
                {
                    mapBtn.normal.textColor = new Color(1f, 0.84f, 0f);
                    mapBtn.fontStyle = FontStyle.Bold;
                }
                if (GUI.Button(new Rect(mapStartX + i * (mapBtnW + 8), mapY + 30, mapBtnW, 35), maps[i], mapBtn))
                {
                    selectedMap = i;
                }
            }

            // 确认按钮
            float confirmY = mapY + 80;
            GUIStyle confirmStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold };
            confirmStyle.normal.textColor = new Color(1f, 0.84f, 0f);
            if (GUI.Button(new Rect(cx - 100, confirmY, 200, 45), "开战!", confirmStyle))
            {
                confirmed = true;
                visible = false;
                OnCharacterConfirmed?.Invoke(selectedChar, selectedMap);
                Debug.Log($"[CharSelect] 角色:{CharNames[selectedChar]} 地图:{selectedMap} 模式:{gameMode}");
            }
        }

        private Color GetSystemColor(string system)
        {
            return system switch
            {
                "武术" => new Color(1f, 0.5f, 0.2f),
                "巫术" => new Color(0.5f, 0.8f, 0.2f),
                "魔法" => new Color(0.3f, 0.5f, 1f),
                "仙术" => new Color(1f, 0.84f, 0f),
                _ => Color.white
            };
        }

        private string GetCharDesc(int i)
        {
            return i switch
            {
                0 => "隐身突袭，烟雾位移，多段暗杀",
                1 => "近身缠斗，反制连招，以弱胜强",
                2 => "骑马作战，冲锋拉扯，游走收割",
                3 => "大开大合，横扫千军，AOE核心",
                4 => "旋转攻击，持续输出，越战越勇",
                5 => "无畏冲锋，伤害转血量，正面硬刚",
                6 => "盾牌格挡，控制反击，保护队友",
                7 => "远距离狙击，标记增伤，陷阱布置",
                8 => "DOT毒伤，减速，风筝消耗",
                9 => "架设炮台，区域封锁，火力压制",
                10 => "治疗光环，群体增益，复活",
                11 => "机关布置，傀儡探视野，减速控制",
                _ => ""
            };
        }

        private Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = color;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private Texture2D MakeBorder(Color color)
        {
            int w = 4, h = 4;
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = color;
            Texture2D result = new Texture2D(w, h);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
