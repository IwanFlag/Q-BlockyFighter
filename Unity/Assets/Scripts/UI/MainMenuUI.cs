using UnityEngine;

namespace QBlockyFighter.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        private bool showMenu = true;
        private string playerName = "";
        private bool offlineMode = false;
        private enum MenuState { Main, Settings, Credits }
        private MenuState state = MenuState.Main;

        public Network.GameClient networkClient;

        void OnGUI()
        {
            if (!showMenu) return;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 48, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = new Color(1f, 0.84f, 0f);

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };
            btnStyle.normal.textColor = Color.white;

            float cx = Screen.width / 2f;
            float cy = Screen.height / 2f;

            GUI.Label(new Rect(cx - 250, cy - 200, 500, 60), "Q版方块人大乱斗", titleStyle);

            if (state == MenuState.Main)
            {
                // 玩家名输入
                GUI.Label(new Rect(cx - 100, cy - 120, 100, 30), "名字:");
                playerName = GUI.TextField(new Rect(cx - 20, cy - 120, 200, 30), playerName, 16);

                // 单机/联机切换
                offlineMode = GUI.Toggle(new Rect(cx - 100, cy - 85, 200, 25), offlineMode,
                    offlineMode ? "📴 单机模式 (离线)" : "🌐 联机模式 (在线)");

                if (GUI.Button(new Rect(cx - 120, cy - 45, 240, 40), "⚔ 1V1 决斗", btnStyle))
                {
                    EnterGame("1v1");
                }
                if (GUI.Button(new Rect(cx - 120, cy + 0, 240, 40), "🏰 5V5 团战", btnStyle))
                {
                    EnterGame("5v5");
                }
                if (GUI.Button(new Rect(cx - 120, cy + 45, 240, 40), "🏆 吃鸡模式", btnStyle))
                {
                    EnterGame("battleroyale");
                }
                if (GUI.Button(new Rect(cx - 120, cy + 90, 240, 40), "🎯 训练场", btnStyle))
                {
                    EnterGame("training");
                }
                if (GUI.Button(new Rect(cx - 120, cy + 145, 115, 35), "⚙ 设置", btnStyle))
                {
                    state = MenuState.Settings;
                }
                if (GUI.Button(new Rect(cx + 5, cy + 145, 115, 35), "❓ 帮助", btnStyle))
                {
                    Application.OpenURL("https://github.com/IwanFlag/Q-BlockyFighter");
                }
            }
            else if (state == MenuState.Settings)
            {
                GUI.Label(new Rect(cx - 100, cy - 80, 200, 30), "设置", titleStyle);

                GUI.Label(new Rect(cx - 100, cy - 20, 200, 30), "音量: 100%");
                GUI.HorizontalSlider(new Rect(cx - 100, cy + 10, 200, 20), 100f, 0f, 100f);

                GUI.Label(new Rect(cx - 100, cy + 40, 200, 30), "灵敏度: 50%");
                GUI.HorizontalSlider(new Rect(cx - 100, cy + 70, 200, 20), 50f, 0f, 100f);

                if (GUI.Button(new Rect(cx - 60, cy + 110, 120, 35), "返回", btnStyle))
                {
                    state = MenuState.Main;
                }
            }
        }

        private void EnterGame(string mode)
        {
            if (string.IsNullOrEmpty(playerName)) playerName = offlineMode ? "单机玩家" : "玩家";

            showMenu = false;

            if (offlineMode)
            {
                // 单机模式：不连服务器，直接开始
                var gm = GameManager.Instance;
                if (gm != null)
                {
                    gm.StartGame(mode, true);
                }
                return;
            }

            // 联机模式
            if (networkClient != null && networkClient.IsConnected)
            {
                networkClient.SetName(playerName);
                if (mode == "training")
                    networkClient.EnterTraining();
                else
                    networkClient.RequestMatchmake();
            }

            // 加载角色选择界面
            var charSelect = GetComponent<CharacterSelectUI>();
            if (charSelect != null) charSelect.Show(mode);
        }

        public void Show()
        {
            showMenu = true;
            state = MenuState.Main;
        }

        public void Hide()
        {
            showMenu = false;
        }
    }
}
