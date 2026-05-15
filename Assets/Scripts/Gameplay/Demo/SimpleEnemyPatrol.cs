using System.Collections;
using UnityEngine;
using SemillasVivas.Systems.Audio;

namespace SemillasVivas.Gameplay.Demo
{
    public sealed class SimpleEnemyPatrol : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float patrolDistance = 2.25f;
        [SerializeField] private int contactDamage = 1;
        [SerializeField] private int hitPoints = 2;
        [SerializeField] private float damageCooldown = 1f;
        [SerializeField] private LayerMask groundMask = 0;

        [Header("Animation State Names")]
        [Tooltip("Exact state name as it appears in the Animator Controller window. " +
                 "Leave blank to auto-detect from clip names.")]
        [SerializeField] private string idleStateOverride   = "";
        [SerializeField] private string runStateOverride    = "";
        [SerializeField] private string attackStateOverride = "";

        [Header("Patrulla")]
        [Tooltip("Desactivar si el suelo es un BoxCollider plano y el raycast de borde " +
                 "causa que el enemigo gire constantemente sin avanzar.")]
        [SerializeField] private bool useGroundAheadCheck = true;
        [Tooltip("Tiempo mínimo entre giros de dirección. Evita el loop rápido " +
                 "cuando el raycast falla varios frames seguidos.")]
        [SerializeField] private float directionChangeCooldown = 0.25f;

        private Rigidbody2D _rigidbody;
        private SpriteRenderer _spriteRenderer;
        private Animator _animator;
        private DemoCharacterAudioController _audioController;
        private Vector2 _origin;
        private float _direction = 1f;
        private float _lastDamageTime = -10f;
        private float _lastDirectionChange = -10f;
        private bool _isDead;
        private bool _isIdleEnemy;

        private bool _isAttacking;
        private float _attackDuration = 0.4f;
        private string _idleState;
        private string _runState;
        private string _attackState;
        private string _currentState;

        private void Start()
        {
            _origin = transform.position;
            _isIdleEnemy = IsIdleOnlyEnemy();

            if (groundMask.value == 0)
            {
                groundMask = LayerMask.GetMask("Ground");
            }

            _rigidbody = GetComponent<Rigidbody2D>();

            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody2D>();
            }

            _rigidbody.gravityScale = 3f;
            _rigidbody.freezeRotation = true;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rigidbody.linearDamping = 0f;

            PhysicsMaterial2D noFriction = new PhysicsMaterial2D("EnemyNoFriction")
            {
                friction   = 0f,
                bounciness = 0f,
            };
            _rigidbody.sharedMaterial = noFriction;

            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.sharedMaterial = noFriction;
                
                col.edgeRadius = 0.04f;
            }

            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _animator = GetComponentInChildren<Animator>();
            _audioController = GetComponent<DemoCharacterAudioController>();

            ResolveAnimStates();
            PlayState(_idleState);
        }

        private void FixedUpdate()
        {
            if (_isDead)
            {
                return;
            }

            if (_isIdleEnemy)
            {
                _rigidbody.linearVelocity = new Vector2(0f, _rigidbody.linearVelocity.y);

                if (!_isAttacking)
                {
                    PlayState(_idleState);
                }

                return;
            }

            bool hitWall = Physics2D.Raycast(
                transform.position + Vector3.up * 0.25f, Vector2.right * _direction, 0.45f, groundMask);
            bool reachedLimit = _direction > 0f
                ? transform.position.x >= _origin.x + patrolDistance
                : transform.position.x <= _origin.x - patrolDistance;

            bool groundFlip = false;
            if (useGroundAheadCheck)
            {
                bool groundAhead = Physics2D.Raycast(
                    transform.position + new Vector3(_direction * 0.35f, 0.15f, 0f), Vector2.down, 0.95f, groundMask);
                groundFlip = !groundAhead && Time.time - _lastDirectionChange >= directionChangeCooldown;
            }

            if (hitWall || reachedLimit || groundFlip)
            {
                _direction *= -1f;
                _lastDirectionChange = Time.time;
            }

            _rigidbody.linearVelocity = new Vector2(_direction * moveSpeed, _rigidbody.linearVelocity.y);
            SetFacing(_direction);

            if (!_isAttacking)
            {
                
                PlayState(!string.IsNullOrEmpty(_runState) ? _runState : _idleState);
            }
        }

        public void TakeHit()
        {
            if (_isDead)
            {
                return;
            }

            hitPoints--;
            _audioController?.Play(GameAudioCue.EnemyHurt, 0.9f);

            if (_spriteRenderer != null)
            {
                StartCoroutine(DamageFlashRoutine());
            }

            if (hitPoints > 0)
            {
                return;
            }

            _isDead = true;
            _rigidbody.linearVelocity = Vector2.zero;
            _audioController?.Play(GameAudioCue.EnemyDeath, 0.9f);
            Destroy(gameObject, 0.15f);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryDamagePlayer(collision.collider);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryDamagePlayer(collision.collider);
        }

        private void TryDamagePlayer(Collider2D other)
        {
            if (_isDead || Time.time - _lastDamageTime < damageCooldown)
            {
                return;
            }

            DemoPlayerHealth playerHealth =
                other.GetComponent<DemoPlayerHealth>() ??
                other.GetComponentInParent<DemoPlayerHealth>();

            if (playerHealth == null)
            {
                return;
            }

            playerHealth.TakeDamage(contactDamage);
            _lastDamageTime = Time.time;

            if (!string.IsNullOrEmpty(_attackState) && !_isAttacking)
            {
                StartCoroutine(AttackAnimRoutine());
            }
        }

        private IEnumerator AttackAnimRoutine()
        {
            _isAttacking = true;
            _currentState = null;
            PlayState(_attackState);

            yield return new WaitForSeconds(_attackDuration);

            string returnState = (!string.IsNullOrEmpty(_runState) && !_isIdleEnemy)
                ? _runState
                : _idleState;

            _currentState = null;
            PlayState(returnState);
            _isAttacking = false;
        }

        private IEnumerator DamageFlashRoutine()
        {
            Color originalColor = _spriteRenderer.color;
            _spriteRenderer.color = new Color(1f, 0.35f, 0.35f, 1f);
            yield return new WaitForSeconds(0.1f);

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = originalColor;
            }
        }

        private void ResolveAnimStates()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                return;
            }

            AnimationClip[] clips = _animator.runtimeAnimatorController.animationClips;

            _idleState   = !string.IsNullOrEmpty(idleStateOverride)
                ? idleStateOverride
                : FindClipByKeyword(clips, "idle", "stand", "rest", "loop", "parado", "estatico", "default");

            _runState    = !string.IsNullOrEmpty(runStateOverride)
                ? runStateOverride
                : FindClipByKeyword(clips, "run", "walk", "correr", "caminar", "move");

            if (!string.IsNullOrEmpty(attackStateOverride))
            {
                _attackState    = attackStateOverride;
                
                AnimationClip attackClip = FindClip(clips, attackStateOverride);
                if (attackClip != null)
                {
                    _attackDuration = Mathf.Max(0.1f, attackClip.length);
                }
            }
            else
            {
                AnimationClip attackClip = FindClip(clips, "attack", "hit", "damage", "bite", "ataque", "golpe");
                if (attackClip != null)
                {
                    _attackState    = attackClip.name;
                    _attackDuration = Mathf.Max(0.1f, attackClip.length);
                }
            }

            if (string.IsNullOrEmpty(_idleState))
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    string n = clips[i].name;
                    if (n != _runState && n != _attackState)
                    {
                        _idleState = n;
                        Debug.Log($"[SimpleEnemyPatrol] '{name}': idle state not found by keyword, " +
                                  $"falling back to first available clip: '{_idleState}'");
                        break;
                    }
                }
            }

            Debug.Log($"[SimpleEnemyPatrol] '{name}' states — " +
                      $"idle: '{_idleState}' | run: '{_runState}' | " +
                      $"attack: '{_attackState}' ({_attackDuration:F2}s)");
        }

        private void SetFacing(float direction)
        {
            if (_spriteRenderer == null || Mathf.Abs(direction) < 0.01f)
            {
                return;
            }

            _spriteRenderer.flipX = direction < 0f;
        }

        private void PlayState(string stateName)
        {
            if (_animator == null || string.IsNullOrEmpty(stateName) || _currentState == stateName)
            {
                return;
            }

            _currentState = stateName;
            _animator.Play(stateName, 0, 0f);
        }

        private bool IsIdleOnlyEnemy()
        {
            string lowerName = name.ToLowerInvariant();
            return lowerName.Contains("sachainchi 2") || lowerName.Contains("sachaicho 2");
        }

        private static AnimationClip FindClip(AnimationClip[] clips, params string[] keywords)
        {
            for (int clipIndex = 0; clipIndex < clips.Length; clipIndex++)
            {
                string lowerName = clips[clipIndex].name.ToLowerInvariant();

                for (int keywordIndex = 0; keywordIndex < keywords.Length; keywordIndex++)
                {
                    if (lowerName.Contains(keywords[keywordIndex]))
                    {
                        return clips[clipIndex];
                    }
                }
            }

            return null;
        }

        private static string FindClipByKeyword(AnimationClip[] clips, params string[] keywords)
        {
            AnimationClip clip = FindClip(clips, keywords);
            return clip != null ? clip.name : null;
        }
    }
}
