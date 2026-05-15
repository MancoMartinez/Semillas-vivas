using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SemillasVivas.Gameplay.Demo
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DemoPlayerAnimationController : MonoBehaviour
    {
        private const string IdleState   = "Idle";
        private const string RunState    = "Run";
        private const string JumpState   = "Jump";
        private const string FallState   = "Fall";
        private const string AttackState = "Attack";
        private const string HurtState   = "Hurt";
        private const string DeathState  = "Death";

        private readonly Dictionary<string, float> _clipLengths = new();

        private Animator _animator;
        private SpriteRenderer _spriteRenderer;
        private Coroutine _temporaryStateRoutine;
        private string _currentState = string.Empty;
        
        private bool _isPlayerJumping;
        
        private float _airborneTimer;
        private const float MinAirborneTimeForFall = 0.15f;
        private float _lastMoveDirection = 1f;
        private string _idleState   = IdleState;
        private string _runState    = RunState;
        private string _jumpState   = JumpState;
        private string _fallState   = FallState;
        private string _attackState = AttackState;
        private string _hurtState   = HurtState;
        private string _deathState  = DeathState;

        public bool IsDead { get; private set; }
        public bool BlocksMovement { get; private set; }
        
        public bool IsAttacking { get; private set; }
        public float FacingDirection => _lastMoveDirection;
        public bool HasAttackState => !string.IsNullOrEmpty(_attackState);
        public bool HasRunState => !string.IsNullOrEmpty(_runState);

        public void Initialize()
        {
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            _clipLengths.Clear();

            if (_animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("Animator controller is missing on Character.");
                return;
            }

            foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
            {
                _clipLengths[clip.name] = clip.length;
            }

            ResolveStateNames();
            PlayState(_idleState);
        }

        public void SetFacing(float horizontalInput)
        {
            if (Mathf.Abs(horizontalInput) < 0.01f)
            {
                return;
            }

            _lastMoveDirection = Mathf.Sign(horizontalInput);
            _spriteRenderer.flipX = _lastMoveDirection < 0f;
        }

        public void UpdateLocomotion(float horizontalInput, bool isGrounded)
        {
            if (IsDead || BlocksMovement)
            {
                return;
            }

            if (!isGrounded)
            {
                return;
            }

            SetFacing(horizontalInput);
            PlayState(Mathf.Abs(horizontalInput) > 0.01f ? _runState : _idleState);
        }

        public void NotifyJump()
        {
            _isPlayerJumping  = true;
            
            _airborneTimer = MinAirborneTimeForFall;
        }

        public void NotifyLanded()
        {
            _isPlayerJumping = false;
            _airborneTimer   = 0f;
        }

        public void UpdateAirborne(bool isGrounded, float verticalVelocity)
        {
            if (IsDead || BlocksMovement)
            {
                return;
            }

            if (isGrounded)
            {
                
                _airborneTimer = 0f;
                return;
            }

            _airborneTimer += Time.deltaTime;

            if (_isPlayerJumping && verticalVelocity > 0f)
            {
                
                PlayState(!string.IsNullOrEmpty(_jumpState) ? _jumpState : _fallState);
            }
            else if (_airborneTimer >= MinAirborneTimeForFall && verticalVelocity < -0.8f)
            {
                
                PlayState(_fallState);
            }
            
        }

        public void PlayAttack()
        {
            if (IsDead || BlocksMovement || string.IsNullOrEmpty(_attackState))
            {
                return;
            }

            IsAttacking = true;
            PlayTemporaryState(_attackState, allowInterrupt: false);
        }

        public void PlayHurt()
        {
            if (IsDead || string.IsNullOrEmpty(_hurtState))
            {
                return;
            }

            PlayTemporaryState(_hurtState, allowInterrupt: true);
        }

        public void FlashDamage()
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            StartCoroutine(FlashDamageRoutine());
        }

        private System.Collections.IEnumerator FlashDamageRoutine()
        {
            _spriteRenderer.color = new Color(1f, 0.2f, 0.2f);
            yield return new WaitForSeconds(0.18f);
            _spriteRenderer.color = Color.white;
        }

        public void PlayDeath()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            BlocksMovement = true;

            if (_temporaryStateRoutine != null)
            {
                StopCoroutine(_temporaryStateRoutine);
                _temporaryStateRoutine = null;
            }

            PlayState(_deathState, forceReplay: true);
        }

        private void PlayTemporaryState(string stateName, bool allowInterrupt)
        {
            if (_temporaryStateRoutine != null)
            {
                if (!allowInterrupt)
                {
                    return;
                }

                StopCoroutine(_temporaryStateRoutine);
            }

            _temporaryStateRoutine = StartCoroutine(PlayTemporaryStateRoutine(stateName));
        }

        private IEnumerator PlayTemporaryStateRoutine(string stateName)
        {
            BlocksMovement = true;
            PlayState(stateName, forceReplay: true);

            yield return new WaitForSeconds(GetClipLength(stateName));

            BlocksMovement = false;
            IsAttacking    = false; 
            _temporaryStateRoutine = null;
            PlayState(_idleState, forceReplay: true);
        }

        private void ResolveStateNames()
        {
            string[] stateNames = _clipLengths.Keys.ToArray();

            _idleState   = FindBestState(stateNames, "idle");
            _runState    = FindBestState(stateNames, "run", "walk");
            _jumpState   = FindBestState(stateNames, "jump");  
            _fallState   = FindBestState(stateNames, "fall");  
            _attackState = FindBestState(stateNames, "attack", "strike", "combo");
            _hurtState   = FindBestState(stateNames, "hurt", "huurt", "damage", "hit");
            _deathState  = FindBestState(stateNames, "death", "dead", "die");

            if (string.IsNullOrEmpty(_fallState) && !string.IsNullOrEmpty(_jumpState))
            {
                _fallState = _jumpState;
            }

            Debug.Log($"[AnimController] '{name}' estados → " +
                      $"idle:{_idleState} run:{_runState} jump:{_jumpState} " +
                      $"fall:{_fallState} attack:{_attackState} hurt:{_hurtState} death:{_deathState}");
        }

        private static string FindBestState(IEnumerable<string> candidates, params string[] searchTerms)
        {
            foreach (string candidate in candidates)
            {
                string lowered = candidate.ToLowerInvariant();

                for (int index = 0; index < searchTerms.Length; index++)
                {
                    if (lowered.Contains(searchTerms[index]))
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        private void PlayState(string stateName, bool forceReplay = false)
        {
            if (string.IsNullOrEmpty(stateName) || !_clipLengths.ContainsKey(stateName))
            {
                return;
            }

            if (!forceReplay && _currentState == stateName)
            {
                return;
            }

            _currentState = stateName;
            _animator.Play(stateName, 0, 0f);
        }

        private float GetClipLength(string stateName)
        {
            if (string.IsNullOrEmpty(stateName))
            {
                return 0.25f;
            }

            return _clipLengths.TryGetValue(stateName, out float duration) ? duration : 0.25f;
        }
    }
}
