using UnityEngine;

namespace SemillasVivas.Gameplay.Demo
{
    
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class MurcielagoEnemy : MonoBehaviour
    {
        [Header("Flotación suave")]
        [Tooltip("Amplitud de la oscilación vertical (0 = sin movimiento).")]
        [SerializeField] private float hoverAmplitude = 0.18f;
        [Tooltip("Frecuencia de la oscilación (ciclos por segundo).")]
        [SerializeField] private float hoverFrequency = 0.9f;

        [Header("Combate")]
        [Tooltip("Daño por contacto al player.")]
        [SerializeField] private int contactDamage = 1;
        [Tooltip("Puntos de vida del Murciélago.")]
        [SerializeField] private int hitPoints = 1;
        [Tooltip("Segundos de espera entre golpes sucesivos al player.")]
        [SerializeField] private float damageCooldown = 1f;

        private Rigidbody2D    _rb;
        private SpriteRenderer _spriteRenderer;
        private Animator       _animator;

        private DemoPlayerHealth _playerHealth;

        private Vector3 _originPosition;
        private float   _lastContactTime = -10f;
        private bool    _isDead;

        public void Setup(DemoPlayerHealth playerHealth)
        {
            _playerHealth = playerHealth;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType       = RigidbodyType2D.Kinematic;
            _rb.gravityScale   = 0f;
            _rb.freezeRotation = true;
            _rb.interpolation  = RigidbodyInterpolation2D.Interpolate;

            _originPosition = transform.position;
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _animator       = GetComponentInChildren<Animator>();

            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            Debug.Log($"[MurcielagoEnemy] '{name}' inicializado.");
        }

        private void FixedUpdate()
        {
            if (_isDead) return;

            float newY = _originPosition.y +
                         Mathf.Sin(Time.fixedTime * hoverFrequency * Mathf.PI * 2f) * hoverAmplitude;
            _rb.MovePosition(new Vector2(_originPosition.x, newY));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDamagePlayer(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDamagePlayer(other);
        }

        private void OnCollisionEnter2D(Collision2D collision) => TryDamagePlayer(collision.collider);
        private void OnCollisionStay2D(Collision2D collision)  => TryDamagePlayer(collision.collider);

        private void TryDamagePlayer(Collider2D other)
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

        private System.Collections.IEnumerator DamageFlashRoutine()
        {
            Color original = _spriteRenderer.color;
            _spriteRenderer.color = new Color(1f, 0.35f, 0.35f, 1f);
            yield return new WaitForSeconds(0.1f);
            if (_spriteRenderer != null) _spriteRenderer.color = original;
        }
    }
}
