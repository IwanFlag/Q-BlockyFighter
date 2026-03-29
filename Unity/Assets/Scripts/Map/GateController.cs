using UnityEngine;

namespace QBlockyFighter.Map
{
    /// <summary>
    /// 机关门系统 - 5V5地图封闭空间的出入口控制
    /// 玩家触碰开关 → 门缓慢关闭 → 封闭30秒 → 门缓慢打开
    /// 可用于：困敌、分割战场、战术封锁
    /// </summary>
    public class GateController : MonoBehaviour
    {
        [Header("门配置")]
        public float closeSpeed = 2f;           // 关门速度
        public float openSpeed = 1.5f;          // 开门速度
        public float lockedDuration = 30f;      // 封闭持续时间（秒）
        public float interactRadius = 3f;       // 开关交互距离
        public bool requireHold = true;         // 是否需要长按触发
        public float holdTime = 1.5f;           // 长按时间（秒）

        [Header("视觉")]
        public Color openColor = new Color(0.3f, 0.8f, 0.3f);   // 绿色=可通行
        public Color closingColor = new Color(1f, 0.8f, 0f);    // 黄色=正在关闭
        public Color closedColor = new Color(1f, 0.2f, 0.2f);   // 红色=已封锁
        public Color openingColor = new Color(0.3f, 0.6f, 1f);  // 蓝色=正在打开

        // 状态
        public enum GateState { Open, Closing, Locked, Opening }
        public GateState CurrentState { get; private set; } = GateState.Open;

        // 内部
        private float doorProgress = 1f;    // 1=全开, 0=全关
        private float lockTimer;
        private float holdTimer;
        private bool playerInRange;
        private string triggeredByTeam;
        private Renderer doorRenderer;
        private Renderer switchRenderer;

        // 门体（左右两扇）
        private Transform leftDoor;
        private Transform rightDoor;
        private Vector3 leftOpenPos;
        private Vector3 rightOpenPos;
        private Vector3 leftClosedPos;
        private Vector3 rightClosedPos;

        // 开关指示灯
        private Light switchLight;

        void Start()
        {
            InitDoorVisuals();
            UpdateVisuals();
        }

        /// <summary>初始化门体结构</summary>
        private void InitDoorVisuals()
        {
            // 门主体（从自身创建）
            doorRenderer = GetComponent<Renderer>();

            // 创建左门板
            var leftGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftGo.name = "LeftDoor";
            leftGo.transform.SetParent(transform);
            leftGo.transform.localPosition = Vector3.zero;
            leftGo.transform.localScale = new Vector3(0.5f, 4f, 5f);
            leftGo.transform.localPosition = new Vector3(-2.5f, 2f, 0);
            leftDoor = leftGo.transform;
            leftOpenPos = leftDoor.localPosition;
            leftClosedPos = new Vector3(-0.3f, 2f, 0);
            leftDoor.GetComponent<Renderer>().material.color = openColor;

            // 创建右门板
            var rightGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightGo.name = "RightDoor";
            rightGo.transform.SetParent(transform);
            rightGo.transform.localPosition = Vector3.zero;
            rightGo.transform.localScale = new Vector3(0.5f, 4f, 5f);
            rightGo.transform.localPosition = new Vector3(2.5f, 2f, 0);
            rightDoor = rightGo.transform;
            rightOpenPos = rightDoor.localPosition;
            rightClosedPos = new Vector3(0.3f, 2f, 0);
            rightDoor.GetComponent<Renderer>().material.color = openColor;

            // 创建开关（在门旁边）
            var switchGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            switchGo.name = "GateSwitch";
            switchGo.transform.SetParent(transform);
            switchGo.transform.localPosition = new Vector3(3.5f, 1.5f, 0);
            switchGo.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            switchRenderer = switchGo.GetComponent<Renderer>();
            switchRenderer.material.color = openColor;

            // 开关灯光
            switchLight = switchGo.AddComponent<Light>();
            switchLight.type = LightType.Point;
            switchLight.range = 5f;
            switchLight.intensity = 1f;
            switchLight.color = openColor;
        }

        void Update()
        {
            switch (CurrentState)
            {
                case GateState.Open:
                    UpdateOpen();
                    break;
                case GateState.Closing:
                    UpdateClosing();
                    break;
                case GateState.Locked:
                    UpdateLocked();
                    break;
                case GateState.Opening:
                    UpdateOpening();
                    break;
            }

            // 检测玩家交互
            CheckPlayerInteraction();
            UpdateVisuals();
        }

        private void UpdateOpen()
        {
            // 门全开，等待玩家触发
            doorProgress = 1f;
        }

        private void UpdateClosing()
        {
            // 缓慢关门
            doorProgress -= closeSpeed * Time.deltaTime;
            if (doorProgress <= 0)
            {
                doorProgress = 0;
                CurrentState = GateState.Locked;
                lockTimer = lockedDuration;
                Debug.Log($"[机关门] {name} 已封锁 {lockedDuration}秒");
            }

            // 移动门板
            float t = doorProgress;
            leftDoor.localPosition = Vector3.Lerp(leftClosedPos, leftOpenPos, t);
            rightDoor.localPosition = Vector3.Lerp(rightClosedPos, rightOpenPos, t);
        }

        private void UpdateLocked()
        {
            // 封闭中，倒计时
            lockTimer -= Time.deltaTime;
            if (lockTimer <= 0)
            {
                CurrentState = GateState.Opening;
                Debug.Log($"[机关门] {name} 解锁，开始开门");
            }
        }

        private void UpdateOpening()
        {
            // 缓慢开门
            doorProgress += openSpeed * Time.deltaTime;
            if (doorProgress >= 1f)
            {
                doorProgress = 1f;
                CurrentState = GateState.Open;
                Debug.Log($"[机关门] {name} 已完全打开");
            }

            // 移动门板
            float t = doorProgress;
            leftDoor.localPosition = Vector3.Lerp(leftClosedPos, leftOpenPos, t);
            rightDoor.localPosition = Vector3.Lerp(rightClosedPos, rightOpenPos, t);
        }

        private void CheckPlayerInteraction()
        {
            // 只有开门状态下才能触发关门
            if (CurrentState != GateState.Open) return;

            var colliders = Physics.OverlapSphere(
                transform.position + new Vector3(3.5f, 1.5f, 0), // 开关位置
                interactRadius
            );

            playerInRange = false;
            foreach (var col in colliders)
            {
                if (!col.CompareTag("Player")) continue;
                playerInRange = true;

                // 检测玩家交互按键（E键）
                // 实际项目中通过Input检测或网络同步
                // 这里简化：玩家在范围内自动倒计时触发
                if (requireHold)
                {
                    holdTimer += Time.deltaTime;
                    if (holdTimer >= holdTime)
                    {
                        TriggerGate("Player");
                        holdTimer = 0;
                    }
                }
                break;
            }

            if (!playerInRange) holdTimer = 0;
        }

        /// <summary>触发关门（外部调用）</summary>
        public void TriggerGate(string team)
        {
            if (CurrentState != GateState.Open) return;

            CurrentState = GateState.Closing;
            triggeredByTeam = team;
            Debug.Log($"[机关门] {name} 被 {team} 触发关门!");
        }

        /// <summary>网络同步触发</summary>
        public void TriggerGateNetwork(int playerId)
        {
            TriggerGate($"Player_{playerId}");
        }

        private void UpdateVisuals()
        {
            Color color = CurrentState switch
            {
                GateState.Open => openColor,
                GateState.Closing => closingColor,
                GateState.Locked => closedColor,
                GateState.Opening => openingColor,
                _ => openColor
            };

            // 开关颜色
            if (switchRenderer != null)
                switchRenderer.material.color = color;

            // 开关灯光
            if (switchLight != null)
            {
                switchLight.color = color;

                // 封闭时闪烁
                if (CurrentState == GateState.Locked)
                {
                    switchLight.intensity = Mathf.PingPong(Time.time * 3f, 2f);
                }
                else
                {
                    switchLight.intensity = 1f;
                }
            }

            // 门板颜色
            if (leftDoor != null)
            {
                var lr = leftDoor.GetComponent<Renderer>();
                if (lr != null) lr.material.color = Color.Lerp(closedColor, openColor, doorProgress);
            }
            if (rightDoor != null)
            {
                var rr = rightDoor.GetComponent<Renderer>();
                if (rr != null) rr.material.color = Color.Lerp(closedColor, openColor, doorProgress);
            }
        }

        /// <summary>获取封闭剩余时间</summary>
        public float GetLockRemaining()
        {
            return CurrentState == GateState.Locked ? lockTimer : 0;
        }

        /// <summary>门是否可通过</summary>
        public bool IsPassable()
        {
            return CurrentState == GateState.Open;
        }

        /// <summary>获取关门进度（用于UI显示）</summary>
        public float GetHoldProgress()
        {
            return requireHold ? holdTimer / holdTime : (playerInRange ? 1f : 0f);
        }

        void OnDrawGizmos()
        {
            // 编辑器可视化
            Gizmos.color = CurrentState switch
            {
                GateState.Open => Color.green,
                GateState.Closing => Color.yellow,
                GateState.Locked => Color.red,
                GateState.Opening => Color.blue,
                _ => Color.green
            };
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2, new Vector3(5, 4, 0.5f));

            // 开关范围
            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position + new Vector3(3.5f, 1.5f, 0), interactRadius);
        }
    }
}
