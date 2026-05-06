using UnityEngine;
using UnityEngine.InputSystem;

namespace Velora.Player
{
    /// <summary>
    /// FPS の移動・視点操作・ジャンプ・スプリントを担当する MonoBehaviour。
    /// Input System の PlayerInput コンポーネントと連携し、
    /// CharacterController で物理挙動を制御する。
    ///
    /// このクラスは「物理・入力」のみを担当し、HP やバフなどのゲームロジックは
    /// PlayerModel（pure C#）が管理する。責務を分離することで
    /// 操作感の調整とバランス調整を独立して行える。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FPSController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 6f;
        [SerializeField] private float _sprintMultiplier = 1.5f;
        [SerializeField] private float _gravity = -20f;

        [Header("Jump")]
        [SerializeField] private float _jumpForce = 7f;

        [Header("Look")]
        [SerializeField] private float _mouseSensitivity = 0.15f;
        [SerializeField] private float _maxLookAngle = 89f;

        private CharacterController _controller;
        private Transform _cameraTransform;

        private Vector2 _moveInput;
        private bool _isSprinting;
        private float _verticalVelocity;
        private float _cameraPitch;

        private const string MouseSensitivityKey = "MouseSensitivity";

        public bool IsGrounded { get; private set; }
        public bool IsSprinting => _isSprinting && _moveInput.sqrMagnitude > 0f;
        public Vector3 Velocity => _controller.velocity;
        public float MouseSensitivity => _mouseSensitivity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _cameraTransform = GetComponentInChildren<Camera>().transform;

            _mouseSensitivity = PlayerPrefs.GetFloat(MouseSensitivityKey, _mouseSensitivity);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void SetMouseSensitivity(float sensitivity)
        {
            _mouseSensitivity = sensitivity;
            PlayerPrefs.SetFloat(MouseSensitivityKey, sensitivity);
        }

        private void Update()
        {
            // UI 操作中（カーソルアンロック時）は入力処理を停止する。
            // アップグレード選択・リザルト画面等でプレイヤーが動かないようにする。
            if (Cursor.lockState != CursorLockMode.Locked) return;

            UpdateGroundCheck();
            UpdateGravity();
            UpdateMovement();
            UpdateLook();
        }

        // --- Input System コールバック ---
        // PlayerInput の Broadcast Messages から呼ばれる（InputValue シグネチャ）

        public void OnMove(InputValue value)
        {
            _moveInput = value.Get<Vector2>();
        }

        public void OnJump(InputValue value)
        {
            if (value.isPressed && IsGrounded)
            {
                _verticalVelocity = _jumpForce;
            }
        }

        public void OnSprint(InputValue value)
        {
            _isSprinting = value.isPressed;
        }

        private void UpdateGroundCheck()
        {
            IsGrounded = _controller.isGrounded;
        }

        private void UpdateGravity()
        {
            if (IsGrounded && _verticalVelocity < 0f)
            {
                // 接地時は小さな下向き速度を維持して接地判定を安定させる
                _verticalVelocity = -2f;
            }

            _verticalVelocity += _gravity * Time.deltaTime;
        }

        private void UpdateMovement()
        {
            float speed = IsSprinting ? _moveSpeed * _sprintMultiplier : _moveSpeed;

            Vector3 moveDirection = transform.right * _moveInput.x + transform.forward * _moveInput.y;
            Vector3 horizontalMove = moveDirection * speed;

            Vector3 finalVelocity = horizontalMove + Vector3.up * _verticalVelocity;
            _controller.Move(finalVelocity * Time.deltaTime);
        }

        /// <summary>
        /// Mouse.current.delta を毎フレーム直接読み取る。
        /// delta はフレーム間のピクセル移動量なので Time.deltaTime は掛けない。
        /// </summary>
        private void UpdateLook()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 delta = mouse.delta.ReadValue();

            float mouseX = delta.x * _mouseSensitivity;
            float mouseY = delta.y * _mouseSensitivity;

            _cameraPitch -= mouseY;
            _cameraPitch = Mathf.Clamp(_cameraPitch, -_maxLookAngle, _maxLookAngle);

            _cameraTransform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }
    }
}
