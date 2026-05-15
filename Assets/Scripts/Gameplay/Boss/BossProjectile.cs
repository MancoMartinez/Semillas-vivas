using UnityEngine;

namespace SemillasVivas.Gameplay.Boss
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class BossProjectile : MonoBehaviour
    {
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private int damage = 1;

        private Rigidbody2D _rigidbody;
        private float _expireAt;
        private bool _isActive;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();

            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody2D>();
            }

            _rigidbody.bodyType      = RigidbodyType2D.Dynamic;
            _rigidbody.gravityScale  = 0f;
            _rigidbody.linearDamping = 0f;
            _rigidbody.freezeRotation = true;

            Collider2D collider = GetComponent<Collider2D>();
            collider.isTrigger = true;

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_isActive && Time.time >= _expireAt)
            {
                Deactivate();
            }
        }

        public void Launch(Vector2 startPosition, Vector2 direction, float speed)
        {
            transform.position = startPosition;
            gameObject.SetActive(true);
            _isActive = true;
            _expireAt = Time.time + lifetime;

            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = direction.normalized * speed;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isActive)
            {
                return;
            }

            Demo.DemoPlayerHealth playerHealth =
                other.GetComponent<Demo.DemoPlayerHealth>() ??
                other.GetComponentInParent<Demo.DemoPlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                Deactivate();
                return;
            }

            if (!other.isTrigger)
            {
                Deactivate();
            }
        }

        public void Deactivate()
        {
            _isActive = false;

            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector2.zero;
            }

            gameObject.SetActive(false);
        }
    }
}
