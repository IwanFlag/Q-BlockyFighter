using UnityEngine;
using System.Collections.Generic;

namespace QBlockyFighter.UI
{
    public class ChatUI : MonoBehaviour
    {
        private bool showChat = true;
        private bool inputFocused = false;
        private string chatInput = "";
        private List<ChatMessage> messages = new List<ChatMessage>();
        private Vector2 scrollPos;
        private int maxMessages = 50;

        public struct ChatMessage
        {
            public string playerName;
            public string content;
            public float time;
            public bool isSystem;
        }

        public event System.Action<string> OnChatSent;

        void Update()
        {
            // Enter切换聊天输入
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (!inputFocused)
                {
                    inputFocused = true;
                    chatInput = "";
                }
                else if (!string.IsNullOrEmpty(chatInput.Trim()))
                {
                    SendChat();
                }
            }
            // ESC取消
            if (Input.GetKeyDown(KeyCode.Escape) && inputFocused)
            {
                inputFocused = false;
                chatInput = "";
            }
        }

        void OnGUI()
        {
            if (!showChat) return;

            float chatX = 20;
            float chatY = Screen.height - 250;
            float chatW = 350;
            float chatH = 180;

            // 聊天背景（半透明）
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.Box(new Rect(chatX, chatY, chatW, chatH), "");
            GUI.color = Color.white;

            // 消息列表
            GUIStyle msgStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true };
            GUIStyle sysStyle = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            sysStyle.normal.textColor = new Color(1f, 0.84f, 0f);

            GUILayout.BeginArea(new Rect(chatX + 5, chatY + 5, chatW - 10, chatH - 40));
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            foreach (var msg in messages)
            {
                if (msg.isSystem)
                {
                    GUILayout.Label($"<color=#ffd700>[系统] {msg.content}</color>", sysStyle);
                }
                else
                {
                    float elapsed = Time.time - msg.time;
                    float alpha = elapsed > 30f ? Mathf.Max(0.3f, 1f - (elapsed - 30f) / 30f) : 1f;
                    GUI.color = new Color(1, 1, 1, alpha);
                    GUILayout.Label($"<color=#aaa>{msg.playerName}:</color> {msg.content}", msgStyle);
                    GUI.color = Color.white;
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // 输入框
            float inputY = chatY + chatH - 30;
            GUI.SetNextControlName("ChatInput");
            chatInput = GUI.TextField(new Rect(chatX + 5, inputY, chatW - 60, 25), chatInput);

            if (inputFocused)
            {
                GUI.FocusControl("ChatInput");
            }

            if (GUI.Button(new Rect(chatX + chatW - 50, inputY, 40, 25), "发送"))
            {
                if (!string.IsNullOrEmpty(chatInput.Trim()))
                {
                    SendChat();
                }
            }
        }

        private void SendChat()
        {
            string msg = chatInput.Trim();
            chatInput = "";
            inputFocused = false;
            OnChatSent?.Invoke(msg);
        }

        public void AddMessage(string playerName, string content, bool isSystem = false)
        {
            messages.Add(new ChatMessage
            {
                playerName = playerName,
                content = content,
                time = Time.time,
                isSystem = isSystem
            });

            if (messages.Count > maxMessages)
            {
                messages.RemoveAt(0);
            }

            // 自动滚动到底部
            scrollPos = new Vector2(0, messages.Count * 20);
        }

        public void AddSystemMessage(string content)
        {
            AddMessage("", content, true);
        }

        public void Toggle()
        {
            showChat = !showChat;
        }
    }
}
