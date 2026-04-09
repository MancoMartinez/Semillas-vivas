using UnityEngine;
using UnityEngine.InputSystem;

namespace SemillasVivas.Gameplay.Demo
{
    public sealed class DemoPlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float jumpForce = 7.5f;
        [SerializeField] private float baseGravityScale = 3f;
        [SerializeField] private float fallGravityMultiplier = 2.5f;
        [SerializeField] private float lowJumpGravityMultiplier = 2f;
        [SerializeField] private float groundCheckDistance = 0.12f;
        [SerializeField] private LayerMask groundLayerMask = 0;

        private DemoPlayerAnimationController _animationController;
        private DemoPlayerHealth _playerHealth;
        private DemoPlayerPowerUpController _powerUpController;
        private Rigidbody2D _rigidbody;
        private float _horizontalInput;
        private bool _jumpRequested;
        private Vector2 _colliderCenter;
        private Vector2 _colliderSize;
        private bool _isGrounded;
        private int _jumpCount;

        public void Initialize(
            DemoPlayerAnimationController animationController,
            DemoPlayerHealth playerHealth,
            DemoPlayerPowerUpController powerUpController)
        {
            _animationController = animationController;
            _playerHealth = playerHealth;
            _powerUpController = powerUpController;
            _rigidbody = GetComponent<Rigidbody2D>();

            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody2D>();
            }

            BoxCollider2D collider = GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                _colliderCenter = collider.offset;
                _colliderSize = collider.size;
            }
            else
            {
                _colliderCenter = Vector2.zero;
                _colliderSize = new Vector2(0.6f, 1.2f);
            }

            _rigidbody.gravityScale = baseGravityScale;
            _rigidbody.freezeRotation = true;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rigidbody.linearDamping = 0f;
            _rigidbody.angularDamping = 0f;
            _rigidbody.mass = 1f;

            if (groundLayerMask.value == 0)
            {
                groundLayerMask = LayerMask.GetMask("Ground");
            }
        }

        private void Update()
        {
            if (_playerHealth == null || _playerHealth.IsDead)
            {
                _horizontalInput = 0f;
                return;
            }

            _isGrounded = CheckGrounded();

            Keyboard keyboard = Keyboard.current;
            _horizontalInput = ReadHorizontalInput(keyboard);
            _animationController.SetFacing(_horizontalInput);
            _animationController.UpdateLocomotion(_horizontalInput, _isGrounded);
            _animationController.UpdateAirborne(_isGrounded, _rigidbody != null ? _rigidbody.linearVelocityY : 0f);

            if (WasAttackPressed(keyboard))
            {
                _animationController.PlayAttack();
            }

            if (WasDamagePressed(keyboard))
            {
                _playerHealth.TakeDamage(1);
            }

            if (WasJumpPressed(keyboard) && CanJump())
            {
                _jumpRequested = true;
            }
        }

        private void FixedUpdate()
        {
            if (_rigidbody == null || _playerHealth == null || _playerHealth.IsDead)
            {
                return;
            }

            float effectiveInput = _animationController.BlocksMovement ? 0f : _horizontalInput;
            Vector2 currentVelocity = _rigidbody.linearVelocity;
            _rigidbody.linearVelocity = new Vector2(effectiveInput * GetMoveSpeed(), currentVelocity.y);

            if (_jumpRequested)
            {
                _jumpRequested = false;
                Vector2 jumpVelocity = _rigidbody.linearVelocity;
                jumpVelocity.y = jumpForce;
                _rigidbody.linearVelocity = jumpVelocity;
                _isGrounded = false;
                _jumpCount++;
            }

            ApplyBetterGravity();
        }

        private bool IsGrounded()
        {
            return _isGrounded;
        }

        private bool CanJump()
        {
            int maxJumpCount = _powerUpController != null ? _powerUpController.MaxJumpCount : 1;

            if (_isGrounded)
            {
                return true;
            }

            return _jumpCount < maxJumpCount;
        }

        private bool CheckGrounded()
        {
            Vector2 origin = (Vector2)transform.position + _colliderCenter + Vector2.up * 0.02f;
            Vector2 size = new(
                Mathf.Max(0.1f, _colliderSize.x * 0.85f),
                Mathf.Max(0.1f, _colliderSize.y * 0.95f));

            RaycastHit2D hit = Physics2D.BoxCast(
                origin,
                size,
                0f,
                Vector3.down,
                groundCheckDistance,
                groundLayerMask);

            return hit.collider != null;
        }

        private void ApplyBetterGravity()
        {
            Vector2 velocity = _rigidbody.linearVelocity;

            if (_isGrounded && velocity.y <= 0.01f)
            {
                _jumpCount = 0;
            }

            float gravityScale = baseGravityScale;

            if (velocity.y < 0f)
            {
                gravityScale = baseGravityScale * fallGravityMultiplier;
            }
            else if (velocity.y > 0f && !IsJumpHeld())
            {
                gravityScale = baseGravityScale * lowJumpGravityMultiplier;
            }

            _rigidbody.gravityScale = gravityScale;
        }

        private static bool IsJumpHeld()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed);
        }

        private static float ReadHorizontalInput(Keyboard keyboard)
        {
            float horizontalInput = 0f;

            if (keyboard != null && (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed))
            {
                horizontalInput -= 1f;
            }

            if (keyboard != null && (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed))
            {
                horizontalInput += 1f;
            }

            return Mathf.Clamp(horizontalInput, -1f, 1f);
        }

        private static bool WasJumpPressed(Keyboard keyboard)
        {
            return keyboard != null && (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame);
        }

        private static bool WasAttackPressed(Keyboard keyboard)
        {
            return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
        }

        private static bool WasDamagePressed(Keyboard keyboard)
        {
            return keyboard != null && keyboard.rKey.wasPressedThisFrame;
        }

        private float GetMoveSpeed()
        {
            return _powerUpController != null ? _powerUpController.CurrentMoveSpeed : moveSpeed;
        }
    }
}
