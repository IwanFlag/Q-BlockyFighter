using UnityEngine;
using System.Collections.Generic;

namespace QBlockyFighter.UI
{
    public class LobbyUI : MonoBehaviour
    {
        public Network.GameClient networkClient;
        private bool showLobby = false;
        private string roomIdInput = "";
        private List<RoomInfo> rooms = new List<RoomInfo>();
        private float refreshTimer;

        public struct RoomInfo
        {
            public int id;
            public string mode;
            public int playerCount;
            public int maxPlayers;
            public string state;
        }

        void Update()
        {
            if (!showLobby) return;
            refreshTimer -= Time.deltaTime;
            if (refreshTimer <= 0)
            {
                RefreshRoomList();
                refreshTimer = 3f;
            }
        }

        void OnGUI()
        {
            if (!showLobby) return;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = new Color(1f, 0.84f, 0f);

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 14 };
            GUIStyle smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            smallStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);

            float cx = Screen.width / 2f;
            float cy = Screen.height / 2f;

            // 背景
            GUI.Box(new Rect(cx - 350, cy - 250, 700, 500), "");

            GUI.Label(new Rect(cx - 150, cy - 240, 300, 40), "大厅", titleStyle);

            // 快速匹配
            if (GUI.Button(new Rect(cx - 320, cy - 180, 150, 40), "快速匹配", btnStyle))
            {
                networkClient?.RequestMatchmake();
                showLobby = false;
            }

            // 创建房间
            if (GUI.Button(new Rect(cx - 155, cy - 180, 150, 40), "创建房间", btnStyle))
            {
                networkClient?.CreateRoom("1v1");
                showLobby = false;
            }

            // 输入房间号加入
            GUI.Label(new Rect(cx + 15, cy - 180, 60, 40), "房间号:");
            roomIdInput = GUI.TextField(new Rect(cx + 70, cy - 175, 80, 30), roomIdInput);
            if (GUI.Button(new Rect(cx + 160, cy - 178, 80, 36), "加入", btnStyle))
            {
                if (int.TryParse(roomIdInput, out int rid))
                {
                    networkClient?.JoinRoom(rid);
                    showLobby = false;
                }
            }

            // 房间列表
            GUI.Label(new Rect(cx - 320, cy - 120, 200, 25), "在线房间:", smallStyle);
            GUI.Label(new Rect(cx + 200, cy - 120, 150, 25), $"在线: ???", smallStyle);

            float listY = cy - 90;
            foreach (var room in rooms)
            {
                GUI.Box(new Rect(cx - 320, listY, 640, 45), "");
                GUI.Label(new Rect(cx - 300, listY + 5, 200, 20), $"房间 #{room.id} [{room.mode}]");
                GUI.Label(new Rect(cx - 300, listY + 25, 200, 15), $"玩家: {room.playerCount}/{room.maxPlayers}", smallStyle);

                string statusText = room.state == "waiting" ? "等待中" : "游戏中";
                GUI.Label(new Rect(cx + 50, listY + 12, 80, 20), statusText);

                if (room.state == "waiting" && GUI.Button(new Rect(cx + 200, listY + 8, 80, 30), "加入", btnStyle))
                {
                    networkClient?.JoinRoom(room.id);
                    showLobby = false;
                }

                if (GUI.Button(new Rect(cx + 290, listY + 8, 80, 30), "观战", btnStyle))
                {
                    networkClient?.Spectate(room.id);
                    showLobby = false;
                }

                listY += 50;
            }

            // 返回
            if (GUI.Button(new Rect(cx - 60, cy + 210, 120, 35), "返回", btnStyle))
            {
                showLobby = false;
            }
        }

        public void Show()
        {
            showLobby = true;
            RefreshRoomList();
        }

        public void Hide()
        {
            showLobby = false;
        }

        private async void RefreshRoomList()
        {
            if (networkClient == null) return;
            var data = await networkClient.GetRoomList();
            // 解析房间列表（简化）
            Debug.Log("[Lobby] 刷新房间列表");
        }

        public void UpdateRooms(List<RoomInfo> newRooms)
        {
            rooms = newRooms;
        }
    }
}
