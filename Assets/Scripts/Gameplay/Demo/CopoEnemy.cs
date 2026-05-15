using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SemillasVivas.Gameplay.Demo
{
    
    public sealed class CopoEnemy : MonoBehaviour
    {
        [Header("Flotación")]
        [SerializeField] private float hoverAmplitude = 0.25f;
        [SerializeField] private float hoverFrequency = 0.8f;

        [Header("Detección y disparo")]
        [SerializeField] private float detectRange     = 6f;
        [SerializeField] private float shootCooldown   = 1.2f;
        [SerializeField] private float projectileSpeed = 5f;

        [Header("Pool de proyectiles")]
        [SerializeField] private int poolSize = 3;

        [Header("Combate")]
        [SerializeField] private int  contactDamage  = 1;
        [SerializeField] private int  hitPoints      = 2;
        [SerializeField] private float damageCooldown = 1f;

        private Rigidbody2D    _rb;
        private SpriteRenderer _spriteRenderer;
        private Animator       _animator;

        private Transform        _playerTransform;
        private DemoPlayerHealth _playerHealth;

        private Transform              _shootTemplate;
        private List<CopoProjectile>   _projectilePool;

        private Vector3 _originPosition;
        private float   _cooldownTimer;
        private float   _lastContactTime = -10f;
        private bool    _isDead;

        public void Setup(Transform playerTransform, DemoPlayerHealth playerHealth)
        {
            _playerTransform = playerTransform;
            _playerHealth    = playerHealth;
        }

        private void Awake()
        {
            enabled = true; 

            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.bodyType       = RigidbodyType2D.Kinematic;
            _rb.gravityScale   = 0f;
            _rb.freezeRotation = true;
            _rb.interpolation  = RigidbodyInterpolation2D.Interpolate;

            _originPosition  = transform.position;
            _spriteRenderer  = GetComponentInChildren<SpriteRenderer>();
            _animator        = GetComponentInChildren<Animator>();

            _shootTemplate = FindChildByName(transform, "shoot")
                             ?? FindChildByName(transform, "Shoot")
                             ?? FindChildByName(transform, "Disparo");

            if (_shootTemplate == null)
            {
                Debug.LogWarning($"[CopoEnemy] '{name}': no se encontró hijo 'shoot'. " +
                                 "El enemigo no disparará. Agrega un hijo llamado 'shoot'.");
            }
            
            Debug.Log($"[CopoEnemy] '{name}' inicializado — " +
                      $"pool={poolSize} | rango={detectRange} | cooldown={shootCooldown}s");
        }

        private void Start()
        {
            
            if (_shootTemplate != null)
            {
                BuildProjectilePool();
                _shootTemplate.gameObject.SetActive(false);
            }
        }

        private void FixedUpdate()
        {
            if (_isDead) return;

            float newY = _originPosition.y +
                         Mathf.Sin(Time.fixedTime * hoverFrequency * Mathf.PI * 2f) * hoverAmplitude;
            _rb.MovePosition(new Vector2(_originPosition.x, newY));
        }

        private void Update()
        {
            if (_isDead || _playerTransform == null || _projectilePool == null) return;

            _cooldownTimer -= Time.deltaTime;

            float dist = Vector2.Distance(transform.position, _playerTransform.position);
            if (dist <= detectRange && _cooldownTimer <= 0f)
            {
                TryFire();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryContactDamage(collision.collider);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryContactDamage(collision.collider);
        }

        private void TryContactDamage(Collider2D other)
        {
            if (_isDead) return;
            if (Time.time - _lastContactTime < damageCooldown) return;

            DemoPlayerHealth health =
                other.GetComponent<DemoPlayerHealth>() ??
                other.GetComponentInParent<DemoPlayerHealth>();

            if (health == null) return;

            health.TakeDamage(contactDamage);
            _lastContactTime = Time.time;
        }

        public void TakeHit()
        {
            if (_isDead) return;

            hitPoints--;

            if (_spriteRenderer != null)
                StartCoroutine(DamageFlashRoutine());

            if (hitPoints > 0) return;

            _isDead = true;
            Destroy(gameObject, 0.1f);
        }

        private void BuildProjectilePool()
        {
            _projectilePool = new List<CopoProjectile>(poolSize);

            for (int i = 0; i < poolSize; i++)
            {
                
                GameObject clone = Instantiate(_shootTemplate.gameObject, transform);
                clone.name = $"shoot_pool_{i}";
                clone.transform.localPosition = Vector3.zero;

                Rigidbody2D projRb = clone.GetComponent<Rigidbody2D>();
                if (projRb == null) projRb = clone.AddComponent<Rigidbody2D>();
                projRb.bodyType      = RigidbodyType2D.Kinematic;
                projRb.gravityScale  = 0f;
                projRb.interpolation = RigidbodyInterpolation2D.Interpolate;

                Collider2D col = clone.GetComponent<Collider2D>();
                if (col == null)
                {
                    CircleCollider2D c = clone.AddComponent<CircleCollider2D>();
                    c.radius    = 0.25f;
                    c.isTrigger = true;
                }
                else
                {
                    col.isTrigger = true;
                }

                CopoProjectile proj = clone.GetComponent<CopoProjectile>();
                if (proj == null) proj = clone.AddComponent<CopoProjectile>();
                proj.Initialize(this);
                _projectilePool.Add(proj);

                clone.SetActive(false);
            }
        }

        private CopoProjectile GetAvailableProjectile()
        {
            if (_projectilePool == null) return null;

            for (int i = 0; i < _projectilePool.Count; i++)
            {
                if (_projectilePool[i] != null && !_projectilePool[i].gameObject.activeSelf)
                    return _projectilePool[i];
            }

            return null; 
        }

        private void TryFire()
        {
            if (_playerTransform == null) return;

            CopoProjectile projectile = GetAvailableProjectile();
            if (projectile == null) return; 

            Vector2 direction = ((Vector2)_playerTransform.position - (Vector2)transform.position).normalized;

            SpriteRenderer projSprite = projectile.GetComponentInChildren<SpriteRenderer>();
            if (projSprite != null) projSprite.flipX = direction.x < 0f;

            projectile.Launch(direction, projectileSpeed);
            _cooldownTimer = shootCooldown;
        }

        private IEnumerator DamageFlashRoutine()
        {
            Color original = _spriteRenderer.color;
            _spriteRenderer.color = new Color(1f, 0.35f, 0.35f, 1f);
            yield return new WaitForSeconds(0.1f);
            if (_spriteRenderer != null) _spriteRenderer.color = original;
        }

        private static Transform FindChildByName(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
                    return child;
                Transform nested = FindChildByName(child, childName);
                if (nested != null) return nested;
            }
            return null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.25f);
            UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.forward, detectRange);
            UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 1f);
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, detectRange);
        }
#endif
    }
}
