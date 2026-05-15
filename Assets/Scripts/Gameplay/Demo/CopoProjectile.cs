using UnityEngine;

namespace SemillasVivas.Gameplay.Demo
{
    
    public sealed class CopoProjectile : MonoBehaviour
    {
        [SerializeField] private float maxLifetime = 5f;

        private CopoEnemy   _owner;
        private Rigidbody2D _rb;

        private Transform _poolParent;          
        private Vector3   _poolLocalPosition;   

        private Vector2 _velocity;
        private bool    _inFlight;
        private float   _spawnTime;

        public void Initialize(CopoEnemy owner)
        {
            _owner             = owner;
            _rb                = GetComponent<Rigidbody2D>();
            _poolParent        = transform.parent;
            _poolLocalPosition = transform.localPosition;
        }

        public void Launch(Vector2 direction, float speed)
        {
            
            if (_poolParent != null)
                transform.position = _poolParent.position;

            transform.SetParent(null);

            _velocity  = direction.normalized * speed;
            _spawnTime = Time.time;
            _inFlight  = true;

            gameObject.SetActive(true);
        }

        public void TakeHit()
        {
            if (!_inFlight) return;
            ReturnToPool();
        }

        private void FixedUpdate()
        {
            if (!_inFlight) return;

            _rb.MovePosition(_rb.position + _velocity * Time.fixedDeltaTime);

            if (Time.time - _spawnTime >= maxLifetime)
                ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_inFlight) return;

            if (_owner != null)
            {
                if (other.gameObject == _owner.gameObject) return;
                if (other.transform.IsChildOf(_owner.transform)) return;
            }

            if (other.GetComponent<CopoProjectile>() != null) return;

            DemoPlayerHealth playerHealth =
                other.GetComponent<DemoPlayerHealth>() ??
                other.GetComponentInParent<DemoPlayerHealth>();

            if (playerHealth != null)
                playerHealth.TakeDamage(1);

            ReturnToPool();
        }

        private void OnDisable()
        {
            _inFlight = false;
        }

        private void ReturnToPool()
        {
            _inFlight             = false;
            _velocity             = Vector2.zero;
            _rb.linearVelocity    = Vector2.zero;

            if (_poolParent != null)
            {
                transform.SetParent(_poolParent);
                transform.localPosition = _poolLocalPosition;
            }

            gameObject.SetActive(false);
        }
    }
}
