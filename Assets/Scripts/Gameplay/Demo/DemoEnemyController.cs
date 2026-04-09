using System.Collections;
using UnityEngine;

namespace SemillasVivas.Gameplay.Demo
{
    public sealed class DemoEnemyController : MonoBehaviour
    {
        [SerializeField] private float patrolDistance = 2f;
        [SerializeField] private float moveSpeed = 2.25f;
        [SerializeField] private float detectionRange = 5f;
        [SerializeField] private float attackRange = 1.1f;
        [SerializeField] private float attackCooldown = 1.1f;
        [SerializeField] private int attackDamage = 1;

        private Transform _player;
        private DemoPlayerAnimationController _animationController;
        private DemoPlayerHealth _playerHealth;
        private Rigidbody2D _rigidbody;
        private Vector3 _spawnPosition;
        private bool _movingRight = true;
        private bool _isDead;
        private bool _isAttacking;

        public void Initialize(Transform player, DemoPlayerAnimationController animationController)
        {
            _player = player;
            _animationController = animationController;
            _playerHealth = player != null ? player.GetComponent<DemoPlayerHealth>() : null;
            _rigidbody = GetComponent<Rigidbody2D>();

            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody2D>();
            }

            _rigidbody.gravityScale = 3f;
            _rigidbody.freezeRotation = true;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _spawnPosition = transform.position;
        }

        private void Update()
        {
            if (_isDead || _player == null || _playerHealth == null || _playerHealth.IsDead)
            {
                _animationController?.UpdateLocomotion(0f, true);
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

            if (_isAttacking)
            {
                _animationController?.UpdateLocomotion(0f, true);
                return;
            }

            if (distanceToPlayer <= attackRange)
            {
                StartCoroutine(AttackRoutine());
                return;
            }

            if (distanceToPlayer <= detectionRange)
            {
                ChasePlayer();
                return;
            }

            Patrol();
        }

        public void TakeHit()
        {
            if (_isDead)
            {
                return;
            }

            _isDead = true;
            _rigidbody.linearVelocity = Vector2.zero;
            _animationController?.PlayDeath();
            Destroy(gameObject, 0.6f);
        }

        private void Patrol()
        {
            float leftLimit = _spawnPosition.x - patrolDistance;
            float rightLimit = _spawnPosition.x + patrolDistance;
            float direction = _movingRight ? 1f : -1f;

            if (transform.position.x >= rightLimit)
            {
                _movingRight = false;
                direction = -1f;
            }
            else if (transform.position.x <= leftLimit)
            {
                _movingRight = true;
                direction = 1f;
            }

            Move(direction);
        }

        private void ChasePlayer()
        {
            float direction = Mathf.Sign(_player.position.x - transform.position.x);
            Move(direction);
        }

        private void Move(float direction)
        {
            Vector2 velocity = _rigidbody.linearVelocity;
            _rigidbody.linearVelocity = new Vector2(direction * moveSpeed, velocity.y);
            _animationController?.SetFacing(direction);
            _animationController?.UpdateLocomotion(direction, true);
        }

        private IEnumerator AttackRoutine()
        {
            _isAttacking = true;
            _rigidbody.linearVelocity = new Vector2(0f, _rigidbody.linearVelocity.y);
            _animationController?.PlayAttack();

            yield return new WaitForSeconds(0.2f);

            if (_player != null && Vector3.Distance(transform.position, _player.position) <= attackRange + 0.1f)
            {
                _playerHealth.TakeDamage(attackDamage);
            }

            yield return new WaitForSeconds(attackCooldown);
            _isAttacking = false;
        }
    }
}
