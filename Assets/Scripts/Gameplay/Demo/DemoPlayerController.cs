using UnityEngine;
using UnityEngine.InputSystem;
using SemillasVivas.Systems.Audio;

namespace SemillasVivas.Gameplay.Demo
{
    public sealed class DemoPlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float jumpForce = 12.0f;
        [SerializeField] private float baseGravityScale = 2.1f;
        [SerializeField] private float fallGravityMultiplier = 1.8f;
        [SerializeField] private float lowJumpGravityMultiplier = 1.35f;
        [SerializeField] private float groundCheckDistance = 0.12f;
        [SerializeField] private LayerMask groundLayerMask = 0;

        private DemoPlayerAnimationController _animationController;
        private DemoPlayerHealth _playerHealth;
        private DemoPlayerPowerUpController _powerUpController;
        private DemoCharacterAudioController _audioController;
        private Rigidbody2D _rigidbody;
        private float _horizontalInput;
        private bool _jumpRequested;
        private bool _inputLocked;
        private Vector2 _colliderCenter;
        private Vector2 _colliderSize;
        private bool _isGrounded;
        private bool _wasGrounded;

        private const float CoyoteTime = 0.12f;
        private float _coyoteTimeCounter;

        private const float GroundedAnimGrace = 0.06f;
        private float _groundedAnimCounter;

        private const float MaxInputLockDuration = 8f;
        private float _inputLockTimer;

        public void Initialize(
            DemoPlayerAnimationController animationController,
            DemoPlayerHealth playerHealth,
            DemoPlayerPowerUpController powerUpController,
            DemoCharacterAudioController audioController)
        {
            _animationController = animationController;
            _playerHealth = playerHealth;
            _powerUpController = powerUpController;
            _audioController = audioController;
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

                PhysicsMaterial2D noFriction = new PhysicsMaterial2D("PlayerNoFriction")
                {
                    friction   = 0f,
                    bounciness = 0f,
                };
                collider.sharedMaterial = noFriction;

                collider.edgeRadius = 0.04f;
            }
            else
            {
                _colliderCenter = Vector2.zero;
                _colliderSize = new Vector2(0.6f, 1.2f);
            }

            if (_rigidbody.sharedMaterial == null)
            {
                PhysicsMaterial2D noFrictionRb = new PhysicsMaterial2D("RigidbodyNoFriction")
                {
                    friction   = 0f,
                    bounciness = 0f,
                };
                _rigidbody.sharedMaterial = noFrictionRb;
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

            if (_inputLocked)
            {
                _horizontalInput = 0f;
                _jumpRequested = false;
                
                _inputLockTimer -= Time.unscaledDeltaTime;
                if (_inputLockTimer <= 0f)
                {
                    _inputLocked = false;
                    Debug.LogWarning("[PlayerController] InputLock expiró por safety timeout.");
                }
                return;
            }

            _isGrounded = CheckGrounded();

            if (_isGrounded && !_wasGrounded)
            {
                _animationController.NotifyLanded();
            }
            _wasGrounded = _isGrounded;

            if (_isGrounded)
            {
                _coyoteTimeCounter   = CoyoteTime;
                _groundedAnimCounter = GroundedAnimGrace;
            }
            else
            {
                _coyoteTimeCounter   -= Time.deltaTime;
                _groundedAnimCounter -= Time.deltaTime;
            }

            bool visuallyGrounded = _isGrounded || _groundedAnimCounter > 0f;

            Keyboard keyboard = Keyboard.current;
            float keyboardHorizontal = ReadHorizontalInput(keyboard);
            float mobileHorizontal = MobileInputState.Horizontal;
            _horizontalInput = Mathf.Abs(keyboardHorizontal) >= Mathf.Abs(mobileHorizontal)
                ? keyboardHorizontal
                : mobileHorizontal;

            _animationController.SetFacing(_horizontalInput);
            _animationController.UpdateLocomotion(_horizontalInput, visuallyGrounded);
            
            _animationController.UpdateAirborne(_isGrounded, _rigidbody != null ? _rigidbody.linearVelocityY : 0f);
            _audioController?.UpdateFootsteps(Mathf.Abs(_horizontalInput) > 0.01f, visuallyGrounded);

            if (WasDamagePressed(keyboard))
            {
                _playerHealth.TakeDamage(1);
            }

            bool jumpPressed = WasJumpPressed(keyboard) || MobileInputState.ConsumeJump();

            if (jumpPressed && _coyoteTimeCounter > 0f)
            {
                _jumpRequested = true;
                _coyoteTimeCounter = 0f; 
                
                _animationController.NotifyJump();
            }
        }

        private void FixedUpdate()
        {
            if (_rigidbody == null || _playerHealth == null || _playerHealth.IsDead)
            {
                return;
            }

            float effectiveInput = (_inputLocked || _animationController.BlocksMovement) ? 0f : _horizontalInput;
            Vector2 currentVelocity = _rigidbody.linearVelocity;

            if (!_playerHealth.IsKnockedBack)
            {
                _rigidbody.linearVelocity = new Vector2(effectiveInput * GetMoveSpeed(), currentVelocity.y);
            }

            if (_jumpRequested)
            {
                _jumpRequested = false;
                Vector2 jumpVelocity = _rigidbody.linearVelocity;
                jumpVelocity.y = GetJumpForce();
                _rigidbody.linearVelocity = jumpVelocity;
                _isGrounded = false;
                _audioController?.Play(GameAudioCue.PlayerJump);
            }

            ApplyBetterGravity();
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

            if (hit.collider != null) return true;

            Collider2D[] nearby = Physics2D.OverlapBoxAll(
                origin + Vector2.down * (groundCheckDistance * 0.5f),
                size,
                0f);

            for (int i = 0; i < nearby.Length; i++)
            {
                if (nearby[i] != null && nearby[i].GetComponent<MovingPlatform>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyBetterGravity()
        {
            Vector2 velocity = _rigidbody.linearVelocity;
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
            
            if (MobileInputState.JumpHeld)
            {
                return true;
            }

            Keyboard keyboard = Keyboard.current;

            if (keyboard != null)
            {
                return keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
            }

            return true;
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

        private static bool WasDamagePressed(Keyboard keyboard)
        {
            return keyboard != null && keyboard.rKey.wasPressedThisFrame;
        }

        private float GetMoveSpeed()
        {
            return _powerUpController != null ? _powerUpController.CurrentMoveSpeed : moveSpeed;
        }

        private float GetJumpForce()
        {
            float multiplier = _powerUpController != null ? _powerUpController.CurrentJumpMultiplier : 1f;
            return jumpForce * multiplier;
        }

        public void SetInputLocked(bool isLocked)
        {
            _inputLocked = isLocked;

            if (!_inputLocked)
            {
                
                MobileInputState.Reset();
                return;
            }

            _horizontalInput = 0f;
            _jumpRequested = false;
            _inputLockTimer = MaxInputLockDuration;
            MobileInputState.Reset();

            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = new Vector2(0f, _rigidbody.linearVelocity.y);
            }
        }
    }
}
