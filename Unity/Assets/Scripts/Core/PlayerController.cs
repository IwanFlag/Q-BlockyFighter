using System;
using UnityEngine;

namespace QBlockyFighter.Core
{
    /// <summary>
    /// Player movement controller with dodge, launch, and physics integration.
    /// Supports frame-sync deterministic movement.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float doubleJumpForce = 6f;
        [SerializeField] private float airControlFactor = 0.5f;

        [Header("Dodge")]
        [SerializeField] private float dodgeSpeed = 15f;
        [SerializeField] private float dodgeDuration = 0.3f;

        // State
        public bool IsGrounded { get; private set; }
        public bool CanDoubleJump { get; private set; }
        public bool IsDodgeMoving { get; private set; }
        public Vector3 Velocity { get; private set; }
        public float CurrentSpeed => _currentSpeed;

        private CharacterController _cc;
        private CombatSystem _combat;
        private HealthSystem _health;
        private CharacterData _charData;

        private Vector3 _moveDir;
        private Vector3 _dodgeDir;
        private float _dodgeTimer;
        private float _verticalVelocity;
        private float _currentSpeed;
        private bool _jumpRequested;
        private bool _doubleJumpRequested;

        // Camera
        private Transform _cameraTransform;
        private float _camYaw;
        private float _camPitch;

        // Frame-sync input
        private float _inputH;
        private float _inputV;
        private bool _inputJump;
        private bool _inputDodge;

        public event Action OnJump;
        public event Action OnDoubleJump;
        public event Action OnLand;

        public void Initialize(CharacterData data, Transform cameraTransform)
        {
            _charData = data;
            _cameraTransform = cameraTransform;
            moveSpeed = data.baseSpeed;

            _combat = GetComponent<CombatSystem>();
            _health = GetComponent<HealthSystem>();
        }

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Dodge movement
            if (IsDodgeMoving)
            {
                _dodgeTimer -= dt;
                if (_dodgeTimer <= 0)
                {
                    IsDodgeMoving = false;
                }
                else
                {
                    _cc.Move(_dodgeDir * dodgeSpeed * dt);
                }
            }

            // Ground check
            bool wasGrounded = IsGrounded;
            IsGrounded = _cc.isGrounded;
            if (IsGrounded && !wasGrounded)
            {
                _verticalVelocity = -1f;
                CanDoubleJump = true;
                _combat?.OnLand();
                OnLand?.Invoke();
            }

            // Gravity
            if (!IsGrounded)
            {
                _verticalVelocity += gravity * dt;
            }

            // Jump
            if (_jumpRequested)
            {
                if (IsGrounded)
                {
                    _verticalVelocity = jumpForce;
                    IsGrounded = false;
                    CanDoubleJump = true;
                    OnJump?.Invoke();
                }
                else if (CanDoubleJump)
                {
                    _verticalVelocity = doubleJumpForce;
                    CanDoubleJump = false;
                    OnDoubleJump?.Invoke();
                }
                _jumpRequested = false;
            }

            // Apply movement
            Vector3 move = _moveDir * (_currentSpeed * dt);
            move.y = _verticalVelocity * dt;

            if (_combat != null && (_combat.IsStunned || _combat.IsKnockedDown))
            {
                move.x = 0;
                move.z = 0;
            }

            if (!IsDodgeMoving)
            {
                _cc.Move(move);
            }

            // Rotation
            if (_moveDir.sqrMagnitude > 0.01f && !IsDodgeMoving)
            {
                Quaternion targetRot = Quaternion.LookRotation(_moveDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRot, rotationSpeed * dt);
            }
        }

        /// <summary>
        /// Process input for this frame (called from frame sync or local input).
        /// </summary>
        public void ProcessInput(float h, float v, bool jump, bool dodge)
        {
            _inputH = h;
            _inputV = v;

            // Calculate move direction relative to camera
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            if (_cameraTransform != null)
            {
                forward = _cameraTransform.forward;
                forward.y = 0;
                forward.Normalize();
                right = _cameraTransform.right;
                right.y = 0;
                right.Normalize();
            }

            _moveDir = (right * h + forward * v).normalized;

            // Speed adjustment
            float speedMult = 1f;
            if (_charData != null && _charData.id == "zhebie") speedMult = 1.15f; // 骑术
            if (_combat != null && _combat.IsInAir) speedMult *= airControlFactor;
            if (_health != null && _health.IsExhausted) speedMult *= 0.5f;

            _currentSpeed = moveSpeed * speedMult;

            // Jump
            if (jump && !_inputJump)
            {
                _jumpRequested = true;
            }
            _inputJump = jump;

            // Dodge
            if (dodge && !_inputDodge)
            {
                TryDodge();
            }
            _inputDodge = dodge;
        }

        public void SetCameraRotation(float yaw, float pitch)
        {
            _camYaw = yaw;
            _camPitch = pitch;
        }

        public void ApplyDodge(Vector3 direction)
        {
            IsDodgeMoving = true;
            _dodgeTimer = dodgeDuration;
            _dodgeDir = direction.normalized;
            if (_dodgeDir.sqrMagnitude < 0.01f)
            {
                _dodgeDir = transform.forward;
            }
        }

        public void ApplyLaunch(float height)
        {
            _verticalVelocity = height;
            IsGrounded = false;
        }

        public void ApplyKnockback(Vector3 direction, float force)
        {
            _cc.Move(direction.normalized * force);
        }

        private void TryDodge()
        {
            if (_combat == null || !_combat.CanDodge()) return;
            if (_health != null && !_health.CanUseStamina(25)) return;

            Vector3 dodgeDir = _moveDir.sqrMagnitude > 0.01f ? _moveDir : transform.forward;
            _combat.TryDodge(dodgeDir);
            if (_health != null) _health.UseStamina(25);
        }

        /// <summary>
        /// Get current input state for frame sync.
        /// </summary>
        public (float h, float v, bool jump, bool dodge) GetInputState()
        {
            return (_inputH, _inputV, _inputJump, _inputDodge);
        }
    }
}
