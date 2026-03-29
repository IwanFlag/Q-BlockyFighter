using UnityEngine;
using System.Collections.Generic;
using QBlockyFighter.Core;

namespace QBlockyFighter
{
    /// <summary>
    /// 观战系统 - 自由视角/跟随视角/击杀回放
    /// </summary>
    public class SpectatorController : MonoBehaviour
    {
        public enum SpectateMode { Free, Follow, Auto }

        public SpectateMode CurrentMode { get; private set; } = SpectateMode.Auto;
        public bool IsActive { get; private set; }
        public Transform FollowTarget { get; private set; }

        [Header("自由视角")]
        public float freeMoveSpeed = 15f;
        public float freeRotateSpeed = 3f;
        public float freeZoomSpeed = 10f;

        [Header("跟随视角")]
        public Vector3 followOffset = new Vector3(0, 8, -6);
        public float followSmooth = 5f;

        private Camera specCamera;
        private List<PlayerController> players = new List<PlayerController>();
        private int followIndex = 0;
        private float autoSwitchTimer;
        private float autoSwitchInterval = 5f;

        // 自由视角状态
        private Vector3 freePosition;
        private float freeYaw;
        private float freePitch;

        // UI
        private bool showUI = true;
        private List<string> killFeed = new List<string>();

        public void Activate(List<PlayerController> playerList)
        {
            players = playerList;
            IsActive = true;

            // 创建观战相机
            if (specCamera == null)
            {
                var camGo = new GameObject("SpectatorCamera");
                specCamera = camGo.AddComponent<Camera>();
                specCamera.depth = 10;
            }

            freePosition = new Vector3(0, 15, 0);
            freeYaw = 0;
            freePitch = 45;

            SetMode(SpectateMode.Auto);
            Debug.Log("[观战] 观战模式启动");
        }

        public void Deactivate()
        {
            IsActive = false;
            if (specCamera != null) Destroy(specCamera.gameObject);
            Debug.Log("[观战] 观战模式关闭");
        }

        void Update()
        {
            if (!IsActive) return;

            // 切换模式
            if (Input.GetKeyDown(KeyCode.F)) SetMode(SpectateMode.Free);
            if (Input.GetKeyDown(KeyCode.G)) SetMode(SpectateMode.Follow);
            if (Input.GetKeyDown(KeyCode.H)) SetMode(SpectateMode.Auto);

            // Tab切换跟随目标
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (CurrentMode == SpectateMode.Follow || CurrentMode == SpectateMode.Auto)
                {
                    followIndex = (followIndex + 1) % players.Count;
                    SetFollowTarget(players[followIndex].transform);
                }
            }

            // 数字键直接选择玩家
            for (int i = 0; i < 9 && i < players.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    followIndex = i;
                    SetFollowTarget(players[i].transform);
                    SetMode(SpectateMode.Follow);
                }
            }

            switch (CurrentMode)
            {
                case SpectateMode.Free:
                    UpdateFreeCamera();
                    break;
                case SpectateMode.Follow:
                    UpdateFollowCamera();
                    break;
                case SpectateMode.Auto:
                    UpdateAutoCamera();
                    break;
            }

            // UI
            if (Input.GetKeyDown(KeyCode.U)) showUI = !showUI;
        }

        private void UpdateFreeCamera()
        {
            // WASD移动
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            freeYaw += Input.GetAxis("Mouse X") * freeRotateSpeed;
            freePitch -= Input.GetAxis("Mouse Y") * freeRotateSpeed;
            freePitch = Mathf.Clamp(freePitch, 10, 80);

            Quaternion rotation = Quaternion.Euler(freePitch, freeYaw, 0);
            Vector3 moveDir = rotation * new Vector3(h, 0, v);
            moveDir.y = 0;
            moveDir = moveDir.normalized * freeMoveSpeed * Time.deltaTime;

            freePosition += moveDir;
            freePosition.y += scroll * freeZoomSpeed;
            freePosition.y = Mathf.Clamp(freePosition.y, 3, 50);

            specCamera.transform.position = freePosition;
            specCamera.transform.rotation = rotation;
        }

        private void UpdateFollowCamera()
        {
            if (FollowTarget == null)
            {
                if (players.Count > 0) SetFollowTarget(players[0].transform);
                return;
            }

            Vector3 targetPos = FollowTarget.position + followOffset;
            specCamera.transform.position = Vector3.Lerp(specCamera.transform.position, targetPos, followSmooth * Time.deltaTime);
            specCamera.transform.LookAt(FollowTarget.position);
        }

        private void UpdateAutoCamera()
        {
            autoSwitchTimer += Time.deltaTime;

            // 自动切换到战斗最激烈的地方
            if (autoSwitchTimer >= autoSwitchInterval)
            {
                autoSwitchTimer = 0;
                FindMostActivePlayer();
            }

            UpdateFollowCamera();
        }

        private void FindMostActivePlayer()
        {
            if (players.Count == 0) return;

            // 简化：找血量最低的（最接近战斗）
            float minHP = float.MaxValue;
            int targetIdx = 0;
            for (int i = 0; i < players.Count; i++)
            {
                var hp = players[i].GetComponent<HealthSystem>();
                if (hp != null && hp.CurrentHp < minHP && hp.CurrentHp > 0)
                {
                    minHP = hp.CurrentHp;
                    targetIdx = i;
                }
            }

            followIndex = targetIdx;
            SetFollowTarget(players[targetIdx].transform);
        }

        public void SetMode(SpectateMode mode)
        {
            CurrentMode = mode;
            Debug.Log($"[观战] 切换到 {mode} 模式");
        }

        public void SetFollowTarget(Transform target)
        {
            FollowTarget = target;
            if (target != null)
                Debug.Log($"[观战] 跟随: {target.name}");
        }

        public void AddKillFeed(string message)
        {
            killFeed.Insert(0, message);
            if (killFeed.Count > 5) killFeed.RemoveAt(5);
        }

        void OnGUI()
        {
            if (!IsActive || !showUI) return;

            // 观战信息
            GUIStyle modeStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            modeStyle.normal.textColor = new Color(1f, 0.84f, 0f);
            GUI.Label(new Rect(20, 20, 200, 25), $"观战模式: {CurrentMode}", modeStyle);

            // 操作提示
            GUI.Label(new Rect(20, 45, 300, 60), "[F]自由视角 [G]跟随视角 [H]自动\n[Tab]切换目标 [1-9]选择玩家 [U]隐藏UI");

            // 玩家列表（右侧）
            float listX = Screen.width - 250;
            float listY = 20;
            GUI.Box(new Rect(listX, listY, 230, 30 + players.Count * 30), "");

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            headerStyle.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            GUI.Label(new Rect(listX + 10, listY + 5, 210, 18), "玩家列表", headerStyle);

            for (int i = 0; i < players.Count; i++)
            {
                float py = listY + 25 + i * 30;
                var hp = players[i].GetComponent<HealthSystem>();
                string hpText = hp != null ? $"{Mathf.CeilToInt(hp.CurrentHp)}/{Mathf.CeilToInt(hp.MaxHp)}" : "N/A";
                bool isFollowed = players[i].transform == FollowTarget;

                GUIStyle playerStyle = new GUIStyle(GUI.skin.label) { fontSize = 11 };
                if (isFollowed) playerStyle.normal.textColor = new Color(1f, 0.84f, 0f);

                GUI.Label(new Rect(listX + 10, py, 20, 18), $"[{i + 1}]", playerStyle);
                GUI.Label(new Rect(listX + 35, py, 100, 18), players[i].name, playerStyle);
                GUI.Label(new Rect(listX + 140, py, 80, 18), hpText, playerStyle);
            }

            // 击杀播报（左上）
            float feedY = 80;
            GUIStyle feedStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            for (int i = 0; i < killFeed.Count; i++)
            {
                float alpha = 1f - (i * 0.15f);
                feedStyle.normal.textColor = new Color(1, 1, 1, alpha);
                GUI.Label(new Rect(20, feedY + i * 22, 400, 20), killFeed[i], feedStyle);
            }

            // 跟随目标信息
            if (FollowTarget != null && (CurrentMode == SpectateMode.Follow || CurrentMode == SpectateMode.Auto))
            {
                float infoX = Screen.width / 2f - 150;
                float infoY = Screen.height - 120;
                GUI.Box(new Rect(infoX, infoY, 300, 80), "");

                var hp2 = FollowTarget.GetComponent<HealthSystem>();
                var combat = FollowTarget.GetComponent<CombatSystem>();

                GUI.Label(new Rect(infoX + 10, infoY + 5, 280, 20), FollowTarget.name, modeStyle);

                if (hp2 != null)
                {
                    float barW = 280;
                    GUI.Box(new Rect(infoX + 10, infoY + 30, barW, 14), "");
                    GUI.DrawTexture(new Rect(infoX + 10, infoY + 30, barW * hp2.HpPercent, 14),
                        MakeTex(new Color(1f, 0.27f, 0.27f)));
                    GUI.Label(new Rect(infoX + 10, infoY + 30, barW, 14),
                        $"HP: {Mathf.CeilToInt(hp2.CurrentHp)}/{Mathf.CeilToInt(hp2.MaxHp)}",
                        new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleCenter });
                }

                if (combat != null)
                {
                    GUI.Label(new Rect(infoX + 10, infoY + 50, 140, 18),
                        $"连击: {combat.CurrentCombo}",
                        new GUIStyle(GUI.skin.label) { fontSize = 11 });
                }
            }
        }

        private Texture2D MakeTex(Color color)
        {
            var tex = new Texture2D(2, 2);
            Color[] pix = { color, color, color, color };
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }
    }
}
