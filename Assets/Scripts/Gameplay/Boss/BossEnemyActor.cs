using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SemillasVivas.Gameplay.Boss
{
    
    public enum BossEnemyRole
    {
        MainBoss,
        GroundMini,
        PlatformShooter,
    }

    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class BossEnemyActor : MonoBehaviour
    {
        
        [Header("Rangos")]
        [SerializeField] private float detectRange = 12f;
        [SerializeField] private float meleeRange  =  1.5f;
        [SerializeField] private float shootRange  =  8f;

        [Header("Movimiento")]
        [SerializeField] private float moveSpeed = 2.25f;

        [Header("Combate")]
        [SerializeField] private int   hitPoints      = 8;
        [SerializeField] private int   contactDamage  = 1;
        [SerializeField] private float attackCooldown = 1.4f;
        [SerializeField] private float projectileSpeed = 5f;

        [Header("Pool de proyectiles")]
        [SerializeField] private int poolSize = 4;

        [Header("Nombres de estado del Animator")]
        [Tooltip("Estado idle/reposo. Normalmente 'Idle'.")]
        [SerializeField] private string idleAnim   = "Idle";
        [Tooltip("Estado de caminar. Normalmente 'Walk'.")]
        [SerializeField] private string walkAnim   = "Walk";
        [Tooltip("Estado de ataque (melee y disparo). Normalmente 'Attack1'.")]
        [SerializeField] private string attackAnim = "Attack1";
        [Tooltip("Estado de muerte. Normalmente 'Defeat'.")]
        [SerializeField] private string deathAnim  = "Defeat";

        private Rigidbody2D           _rb;
        private SpriteRenderer        _sr;
        private Animator              _anim;
        private Transform             _player;
        private Demo.DemoPlayerHealth _playerHealth;
        private BossFightController   _owner;

        private readonly List<BossProjectile> _pool = new();
        private float _cooldown;
        private bool  _isDead;
        private bool  _isInvulnerable;
        private bool  _phase3IdleMode;  

        public bool          IsDead        => _isDead;
        public int           CurrentHealth { get; private set; }
        public int           MaxHealth     => hitPoints;
        
        public BossEnemyRole Role          => BossEnemyRole.MainBoss;

        public void Setup(
            BossFightController       owner,
            Transform                 player,
            Demo.DemoPlayerHealth     playerHealth,
            BossEnemyRole             enemyRole,       
            int   health,
            float speed,
            bool  phase3Idle,        
            bool  invulnerable,
            bool  destroyAfterDeath) 
        {
            _owner          = owner;
            _player         = player;
            _playerHealth   = playerHealth;
            hitPoints       = Mathf.Max(1, health);
            moveSpeed       = speed;
            _isInvulnerable = invulnerable;
            _phase3IdleMode = phase3Idle;
            CurrentHealth   = hitPoints;
            _isDead         = false;

            GrabComponents();

            if (_rb != null)
            {
                _rb.constraints = _phase3IdleMode
                    ? RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation
                    : RigidbodyConstraints2D.FreezeRotation;
            }

            SetAnim(idleAnim, forceRestart: true);
        }

        public void ConfigureProjectiles(GameObject template, int size, Transform poolParent = null)
        {
            if (template == null || size <= 0 || _pool.Count > 0) return;

            Transform parent = poolParent != null ? poolParent
                             : (transform.parent != null ? transform.parent : transform);

            for (int i = 0; i < size; i++)
            {
                GameObject go = Instantiate(template, parent);
                go.name = $"shoot_pool_{i}";
                BossProjectile bp = go.GetComponent<BossProjectile>() ?? go.AddComponent<BossProjectile>();
                _pool.Add(bp);
                go.SetActive(false);
            }
        }

        public void SetInvulnerable(bool value) => _isInvulnerable = value;

        public void SetPhase3IdleMode(bool value)
        {
            _phase3IdleMode = value;
            if (_rb != null)
            {
                _rb.constraints = value
                    ? RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation
                    : RigidbodyConstraints2D.FreezeRotation;
            }
        }

        public void SetIdleAnimationName(string name)
        {
            if (!string.IsNullOrEmpty(name)) idleAnim = name;
        }

        public void TakeHit()
        {
            if (_isDead || _isInvulnerable) return;
            CurrentHealth = Mathf.Max(0, CurrentHealth - 1);
            _owner?.NotifyEnemyDamaged(this);
            if (CurrentHealth > 0) { StartCoroutine(FlashRoutine()); return; }
            StartCoroutine(DeathRoutine());
        }

        private void Awake()
        {
            GrabComponents();
            CurrentHealth = Mathf.Max(1, hitPoints);
        }

        private void Update()
        {
            if (_isDead || _player == null) return;

            _cooldown -= Time.deltaTime;

            float dist = Vector2.Distance(transform.position, _player.position);

            if (_phase3IdleMode)
            {
                
                StopAndIdle();
                if (_player != null)
                {
                    Flip(_player.position.x - transform.position.x);
                }

                if (dist <= detectRange)
                    TryShoot();
                return;
            }

            if (dist > detectRange)
            {
                StopAndIdle();
                return;
            }

            if (dist <= meleeRange)
            {
                StopAndIdle();
                TryMelee();
            }
            else if (dist <= shootRange)
            {
                StopAndIdle();
                TryShoot();
            }
            else
            {
                WalkToPlayer();
            }
        }

        private void WalkToPlayer()
        {
            if (_rb == null || _player == null) return;
            float dir = Mathf.Sign(_player.position.x - transform.position.x);
            _rb.linearVelocity = new Vector2(dir * moveSpeed, _rb.linearVelocity.y);
            Flip(dir);
            SetAnim(walkAnim);
        }

        private void StopAndIdle()
        {
            if (_rb != null)
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);

            if (_anim != null)
            {
                AnimatorStateInfo s = _anim.GetCurrentAnimatorStateInfo(0);
                if (s.IsName(attackAnim) && s.normalizedTime < 0.9f)
                    return;
            }

            SetAnim(idleAnim);
        }

        private void TryMelee()
        {
            if (_cooldown > 0f) return;
            _cooldown = attackCooldown * GetCooldownMult();
            SetAnim(attackAnim, forceRestart: true);
            _playerHealth?.TakeDamage(contactDamage);

            if (_playerHealth != null && _player != null)
            {
                float dir = Mathf.Sign(_player.position.x - transform.position.x);
                _playerHealth.ApplyKnockback(new Vector2(dir * 7f, 4f));
            }
        }

        private void TryShoot()
        {
            if (_cooldown > 0f || _player == null) return;

            BossProjectile proj = GetFreeProjectile();
            if (proj == null) return; 

            _cooldown = attackCooldown * GetCooldownMult();
            SetAnim(attackAnim, forceRestart: true);

            Vector2 dir      = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            
            Vector2 spawnPos = (Vector2)transform.position + dir * 0.9f;
            proj.Launch(spawnPos, dir, projectileSpeed);
            Flip(dir.x);
        }

        private float GetCooldownMult() =>
            _owner != null ? _owner.GetCurrentAttackCooldownMultiplier() : 1f;

        private BossProjectile GetFreeProjectile()
        {
            foreach (BossProjectile bp in _pool)
                if (bp != null && !bp.gameObject.activeSelf) return bp;
            return null;
        }

        private void GrabComponents()
        {
            _rb   = GetComponent<Rigidbody2D>();
            _sr   = GetComponentInChildren<SpriteRenderer>(true);
            _anim = GetComponentInChildren<Animator>(true);

            if (_rb == null) return;

            _rb.bodyType               = RigidbodyType2D.Dynamic;
            _rb.gravityScale           = 3f;
            _rb.linearDamping          = 0f;
            _rb.angularDamping         = 0f;
            _rb.freezeRotation         = true;
            _rb.interpolation          = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.constraints            = RigidbodyConstraints2D.FreezeRotation;

            if (_rb.sharedMaterial == null)
            {
                _rb.sharedMaterial = new PhysicsMaterial2D("BossRbNoFriction")
                {
                    friction   = 0f,
                    bounciness = 0f,
                };
            }

            Collider2D col = GetComponent<Collider2D>() ?? GetComponentInChildren<Collider2D>();
            if (col != null)
            {
                col.isTrigger = false;
            }
        }

        private void SetAnim(string state, bool forceRestart = false)
        {
            if (_anim == null || string.IsNullOrEmpty(state)) return;
            if (!forceRestart && _anim.GetCurrentAnimatorStateInfo(0).IsName(state)) return;
            _anim.Play(state, 0, 0f);
        }

        private void Flip(float dirX)
        {
            if (_sr == null || Mathf.Abs(dirX) < 0.01f) return;
            _sr.flipX = dirX < 0f;
        }

        private IEnumerator FlashRoutine()
        {
            if (_sr == null) yield break;
            Color orig = _sr.color;
            _sr.color = new Color(1f, 0.35f, 0.35f, 1f);
            yield return new WaitForSeconds(0.1f);
            if (_sr != null) _sr.color = orig;
        }

        private IEnumerator DeathRoutine()
        {
            _isDead = true;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            SetAnim(deathAnim, forceRestart: true);
            yield return new WaitForSeconds(0.6f);
            _owner?.NotifyEnemyDefeated(this);
            gameObject.SetActive(false);
        }
    }
}
