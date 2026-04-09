using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SemillasVivas.Gameplay.Demo
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DemoPlayerAnimationController : MonoBehaviour
    {
        private const string IdleState = "Idle";
        private const string RunState = "Run";
        private const string FallState = "Fall";
        private const string AttackState = "Attack";
        private const string HurtState = "Huurt";
        private const string DeathState = "Death";

        private readonly Dictionary<string, float> _clipLengths = new();

        private Animator _animator;
        private SpriteRenderer _spriteRenderer;
        private Coroutine _temporaryStateRoutine;
        private string _currentState = string.Empty;
        private float _lastMoveDirection = 1f;

        public bool IsDead { get; private set; }
        public bool BlocksMovement { get; private set; }
        public float FacingDirection => _lastMoveDirection;

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

            PlayState(IdleState);
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
            PlayState(Mathf.Abs(horizontalInput) > 0.01f ? RunState : IdleState);
        }

        public void UpdateAirborne(bool isGrounded, float verticalVelocity)
        {
            if (IsDead || BlocksMovement)
            {
                return;
            }

            if (!isGrounded && verticalVelocity <= -0.01f)
            {
                PlayState(FallState);
            }
        }

        public void PlayAttack()
        {
            if (IsDead || BlocksMovement)
            {
                return;
            }

            PlayTemporaryState(AttackState, allowInterrupt: false);
        }

        public void PlayHurt()
        {
            if (IsDead)
            {
                return;
            }

            PlayTemporaryState(HurtState, allowInterrupt: true);
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

            PlayState(DeathState, forceReplay: true);
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
            _temporaryStateRoutine = null;
            PlayState(IdleState, forceReplay: true);
        }

        private void PlayState(string stateName, bool forceReplay = false)
        {
            if (!forceReplay && _currentState == stateName)
            {
                return;
            }

            _currentState = stateName;
            _animator.Play(stateName, 0, 0f);
        }

        private float GetClipLength(string stateName)
        {
            return _clipLengths.TryGetValue(stateName, out float duration) ? duration : 0.25f;
        }
    }
}
