using UnityEngine;

namespace QBlockyFighter.UI
{
    /// <summary>
    /// 移动端触控UI - 虚拟摇杆 + 触控按钮
    /// 适配手机/平板操作
    /// </summary>
    public class MobileInputUI : MonoBehaviour
    {
        [Header("摇杆配置")]
        public float joystickRadius = 80f;
        public float joystickDeadZone = 0.15f;

        // 摇杆状态
        private Vector2 joystickInput;
        private bool joystickActive;
        private int joystickTouchId = -1;
        private Vector2 joystickCenter;

        // 按钮状态
        private bool attackPressed;
        private bool heavyPressed;
        private bool dodgePressed;
        private bool blockPressed;
        private bool skillQPressed;
        private bool skillEPressed;
        private bool skillRPressed;
        private bool switchWeaponPressed;

        // UI布局
        private float screenW;
        private float screenH;
        private bool showMobileUI;

        // 按钮区域
        private Rect attackBtn;
        private Rect heavyBtn;
        private Rect dodgeBtn;
        private Rect blockBtn;
        private Rect skillQBtn;
        private Rect skillEBtn;
        private Rect skillRBtn;
        private Rect switchBtn;
        private Rect joystickArea;

        void Start()
        {
            // 检测是否为移动平台
            showMobileUI = Application.platform == RuntimePlatform.Android ||
                          Application.platform == RuntimePlatform.IPhonePlayer ||
                          Input.touchSupported;

            UpdateLayout();
        }

        void Update()
        {
            if (!showMobileUI) return;

            screenW = Screen.width;
            screenH = Screen.height;
            UpdateLayout();

            // 处理触控
            ProcessTouches();

            // 重置单帧按下状态
            attackPressed = false;
            heavyPressed = false;
            dodgePressed = false;
            blockPressed = false;
            skillQPressed = false;
            skillEPressed = false;
            skillRPressed = false;
            switchWeaponPressed = false;
        }

        private void UpdateLayout()
        {
            float btnSize = 65f;
            float btnGap = 10f;
            float rightMargin = 20f;
            float bottomMargin = 20f;

            // 右侧技能按钮（3x2布局）
            float baseX = screenW - rightMargin - btnSize;
            float baseY = screenH - bottomMargin - btnSize;

            attackBtn = new Rect(baseX, baseY, btnSize, btnSize);
            heavyBtn = new Rect(baseX - btnSize - btnGap, baseY, btnSize, btnSize);
            dodgeBtn = new Rect(baseX, baseY - btnSize - btnGap, btnSize, btnSize);
            blockBtn = new Rect(baseX - btnSize - btnGap, baseY - btnSize - btnGap, btnSize, btnSize);

            // 技能按钮（上方）
            skillQBtn = new Rect(baseX - (btnSize + btnGap) * 2, baseY, btnSize * 0.8f, btnSize * 0.8f);
            skillEBtn = new Rect(baseX - (btnSize + btnGap) * 2, baseY - btnSize * 0.9f, btnSize * 0.8f, btnSize * 0.8f);
            skillRBtn = new Rect(baseX - (btnSize + btnGap) * 2, baseY - btnSize * 1.8f, btnSize * 0.8f, btnSize * 0.8f);

            // 切换武器按钮
            switchBtn = new Rect(baseX - btnSize - btnGap, baseY - (btnSize + btnGap) * 2, btnSize * 0.7f, btnSize * 0.7f);

            // 摇杆区域（左下角）
            joystickArea = new Rect(30, screenH - joystickRadius * 2 - 30, joystickRadius * 2, joystickRadius * 2);
            joystickCenter = new Vector2(joystickArea.x + joystickRadius, joystickArea.y + joystickRadius);
        }

        private void ProcessTouches()
        {
            foreach (Touch touch in Input.touches)
            {
                Vector2 pos = touch.position;
                pos.y = screenH - pos.y; // GUI坐标系翻转

                if (touch.phase == TouchPhase.Began)
                {
                    // 摇杆区域检测
                    if (Vector2.Distance(pos, joystickCenter) < joystickRadius * 1.5f)
                    {
                        joystickActive = true;
                        joystickTouchId = touch.fingerId;
                    }
                    // 按钮检测
                    else
                    {
                        CheckButtonPress(pos);
                    }
                }

                if (touch.fingerId == joystickTouchId)
                {
                    if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    {
                        Vector2 delta = pos - joystickCenter;
                        delta = Vector2.ClampMagnitude(delta, joystickRadius) / joystickRadius;
                        if (delta.magnitude < joystickDeadZone) delta = Vector2.zero;
                        joystickInput = delta;
                    }

                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        joystickActive = false;
                        joystickTouchId = -1;
                        joystickInput = Vector2.zero;
                    }
                }
            }
        }

        private void CheckButtonPress(Vector2 pos)
        {
            if (attackBtn.Contains(pos)) attackPressed = true;
            else if (heavyBtn.Contains(pos)) heavyPressed = true;
            else if (dodgeBtn.Contains(pos)) dodgePressed = true;
            else if (blockBtn.Contains(pos)) blockPressed = true;
            else if (skillQBtn.Contains(pos)) skillQPressed = true;
            else if (skillEBtn.Contains(pos)) skillEPressed = true;
            else if (skillRBtn.Contains(pos)) skillRPressed = true;
            else if (switchBtn.Contains(pos)) switchWeaponPressed = true;
        }

        void OnGUI()
        {
            if (!showMobileUI) return;

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold };
            GUIStyle smallBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 12 };

            // 摇杆背景
            GUI.color = new Color(1, 1, 1, 0.3f);
            GUI.DrawTexture(new Rect(joystickCenter.x - joystickRadius, joystickCenter.y - joystickRadius,
                joystickRadius * 2, joystickRadius * 2), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // 摇杆手柄
            float handleX = joystickCenter.x + joystickInput.x * joystickRadius - 25;
            float handleY = joystickCenter.y - joystickInput.y * joystickRadius - 25;
            GUI.color = new Color(1, 1, 1, 0.6f);
            GUI.Box(new Rect(handleX, handleY, 50, 50), "", GUI.skin.button);
            GUI.color = Color.white;

            // 攻击按钮
            GUI.Button(attackBtn, "⚔\n攻击", btnStyle);
            GUI.Button(heavyBtn, "💥\n重击", btnStyle);
            GUI.Button(dodgeBtn, "💨\n闪避", btnStyle);
            GUI.Button(blockBtn, "🛡\n格挡", btnStyle);

            // 技能按钮
            GUI.color = new Color(0.5f, 0.8f, 1f);
            GUI.Button(skillQBtn, "Q", smallBtnStyle);
            GUI.color = new Color(0.5f, 1f, 0.5f);
            GUI.Button(skillEBtn, "E", smallBtnStyle);
            GUI.color = new Color(1f, 0.5f, 0.5f);
            GUI.Button(skillRBtn, "R", smallBtnStyle);
            GUI.color = Color.white;

            // 切换武器
            GUI.Button(switchBtn, "🔄", smallBtnStyle);
        }

        // ===== 公开接口 =====
        public Vector2 GetJoystickInput() => joystickInput;
        public bool IsAttackPressed() => attackPressed;
        public bool IsHeavyPressed() => heavyPressed;
        public bool IsDodgePressed() => dodgePressed;
        public bool IsBlockPressed() => blockPressed;
        public bool IsSkillQPressed() => skillQPressed;
        public bool IsSkillEPressed() => skillEPressed;
        public bool IsSkillRPressed() => skillRPressed;
        public bool IsSwitchWeaponPressed() => switchWeaponPressed;
        public bool IsMobilePlatform() => showMobileUI;
    }
}
