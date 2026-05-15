using System.Collections.Generic;
using UnityEngine;

namespace SemillasVivas.Gameplay.Demo
{
    
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class MovingPlatform : MonoBehaviour
    {
        [SerializeField] private float speed = 2f;
        [SerializeField] private float range = 3f;
        [Tooltip("Invierte la dirección inicial. Úsalo en una de las dos plataformas " +
                 "para que se muevan en sentidos opuestos.")]
        [SerializeField] private bool invertInitialDirection = false;

        private Rigidbody2D _rb;
        private Vector2     _origin;
        private Vector2     _direction;
        private int         _sign = 1;

        private readonly HashSet<Rigidbody2D> _passengers = new();

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();

            _rb.bodyType                = RigidbodyType2D.Kinematic;
            _rb.interpolation           = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode  = CollisionDetectionMode2D.Continuous;
            _rb.constraints             = RigidbodyConstraints2D.FreezeRotation;

            _origin    = _rb.position;

            if (CompareTag("TroncoV"))
                _direction = Vector2.up;
            else if (CompareTag("TroncoD"))
                _direction = new Vector2(1f, 1f).normalized; 
            else
                _direction = Vector2.right; 

            _sign = invertInitialDirection ? -1 : 1;

            string modeLabel = CompareTag("TroncoV") ? "VERTICAL"
                             : CompareTag("TroncoD") ? "DIAGONAL"
                             : "HORIZONTAL";
            Debug.Log($"[MovingPlatform] '{name}' → {modeLabel} | speed={speed} range={range}");
        }

        private void FixedUpdate()
        {
            Vector2 currentPos = _rb.position;

            float   step      = _sign * speed * Time.fixedDeltaTime;
            Vector2 candidate = currentPos + _direction * step;
            float   dist      = Vector2.Dot(candidate - _origin, _direction);

            if (dist >= range)
            {
                _sign = -1;
                dist  = range;
            }
            else if (dist <= -range)
            {
                _sign = 1;
                dist  = -range;
            }

            Vector2 newPos = _origin + _direction * dist;
            Vector2 delta  = newPos - currentPos;

            RefreshPassengers();

            _rb.MovePosition(newPos);

            foreach (Rigidbody2D passenger in _passengers)
            {
                if (passenger != null)
                {
                    passenger.position += delta;
                }
            }
        }

        private void RefreshPassengers()
        {
            _passengers.Clear();

            Collider2D platformCol = GetComponent<Collider2D>();
            if (platformCol == null) return;

            Bounds b = platformCol.bounds;
            Vector2 scanCenter = new Vector2(b.center.x, b.max.y + 0.12f);
            Vector2 scanSize   = new Vector2(b.size.x * 0.92f, 0.28f);

            Collider2D[] hits = Physics2D.OverlapBoxAll(scanCenter, scanSize, 0f);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject == gameObject) continue;

                Rigidbody2D rb = hits[i].attachedRigidbody;
                
                if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
                {
                    _passengers.Add(rb);
                }
            }
        }
    }
}
