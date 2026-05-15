using System.Collections;
using UnityEngine;
using SemillasVivas.Systems.Audio;

namespace SemillasVivas.Gameplay.Demo
{
    public sealed class DemoEnemyController : MonoBehaviour
    {
        private enum EnemyMovementMode
        {
            GroundPatrol,
            AerialHover,
        }

        [SerializeField] private float patrolDistance = 2f;
        [SerializeField] private float moveSpeed = 2.25f;
        [SerializeField] private float detectionRange = 5f;
        [SerializeField] private float attackCooldown = 1.1f;
        [SerializeField] private int attackDamage = 1;
        [SerializeField] private float hoverAmplitude = 0.25f;
        [SerializeField] private float hoverSpeed = 2f;

        private Transform _player;
        private DemoPlayerAnimationController _animationController;
        private DemoPlayerHealth _playerHealth;
        private DemoCharacterAudioController _audioController;
        private DemoEnemyPatrolPath _patrolPath;
        private Rigidbody2D _rigidbody;
        private Vector3 _spawnPosition;
        private bool _movingRight = true;
        private bool _isDead;
        private bool _isAttacking;
        private EnemyMovementMode _movementMode;
        private DemoPowerUpType _grantedPowerUp;
        private float _hoverTime;
        private bool _supportsAttackState;

        public void Initialize(
            Transform player,
            DemoPlayerAnimationController animationController,
            DemoCharacterAudioController audioController)
        {
            _player = player;
            _animationController = animationController;
            _audioController = audioController;
            _playerHealth = player != null ? player.GetComponent<DemoPlayerHealth>() : null;
            _patrolPath = GetComponent<DemoEnemyPatrolPath>() ?? GetComponentInParent<DemoEnemyPatrolPath>();
            _rigidbody = GetComponent<Rigidbody2D>();

            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody2D>();
            }

            _movementMode = ResolveMovementMode();
            _grantedPowerUp = ResolveGrantedPowerUp();
            _supportsAttackState = _animationController != null && _animationController.HasAttackState;

            _rigidbody.gravityScale = _movementMode == EnemyMovementMode.AerialHover ? 0f : 3f;
            _rigidbody.freezeRotation = true;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rigidbody.bodyType = RigidbodyType2D.Dynamic;
            _spawnPosition = transform.position;
        }

        private void Update()
        {
            if (_isDead || _player == null || _playerHealth == null || _playerHealth.IsDead)
            {
                _audioController?.UpdateFootsteps(false, false);
                _animationController?.UpdateLocomotion(0f, true);
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

            if (_isAttacking)
            {
                _audioController?.UpdateFootsteps(false, false);
                _animationController?.UpdateLocomotion(0f, true);
                return;
            }

            if (_supportsAttackState && distanceToPlayer <= detectionRange)
            {
                StartCoroutine(AttackRoutine());
                return;
            }

            if (!_supportsAttackState && distanceToPlayer <= detectionRange)
            {
                ChasePlayer();
                return;
            }

            Patrol();
        }

        public void TakeHit(DemoPlayerPowerUpController playerPowerUpController)
        {
            if (_isDead)
            {
                return;
            }

            _isDead = true;
            _rigidbody.linearVelocity = Vector2.zero;
            _audioController?.StopAllLoops();
            _audioController?.Play(GameAudioCue.EnemyDeath);
            playerPowerUpController?.Apply(_grantedPowerUp);
            _animationController?.PlayDeath();
            Destroy(gameObject, 0.6f);
        }

        private void Patrol()
        {
            if (_patrolPath != null && _patrolPath.HasPoints)
            {
                PatrolAlongPoints();
                return;
            }

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

        private void PatrolAlongPoints()
        {
            Vector2 targetPoint = _patrolPath.CurrentPoint;

            if (_movementMode == EnemyMovementMode.AerialHover)
            {
                MoveAerial(targetPoint);
            }
            else
            {
                float horizontalDelta = targetPoint.x - transform.position.x;
                float direction = Mathf.Abs(horizontalDelta) > 0.05f ? Mathf.Sign(horizontalDelta) : 0f;
                Move(direction);
            }

            _patrolPath.AdvanceIfReached(transform.position);
        }

        private void ChasePlayer()
        {
            if (_movementMode == EnemyMovementMode.AerialHover)
            {
                MoveAerial(_player.position);
                return;
            }

            float direction = Mathf.Sign(_player.position.x - transform.position.x);
            Move(direction);
        }

        private void Move(float direction)
        {
            Vector2 velocity = _rigidbody.linearVelocity;
            _rigidbody.linearVelocity = new Vector2(direction * moveSpeed, velocity.y);
            _animationController?.SetFacing(direction);
            _animationController?.UpdateLocomotion(direction, true);
            _audioController?.UpdateFootsteps(Mathf.Abs(direction) > 0.01f, _movementMode == EnemyMovementMode.GroundPatrol);
        }

        private void MoveAerial(Vector2 destination)
        {
            float direction = Mathf.Sign(destination.x - transform.position.x);
            _hoverTime += Time.deltaTime * hoverSpeed;

            Vector2 targetPosition = new(
                Mathf.MoveTowards(transform.position.x, destination.x, moveSpeed * Time.deltaTime),
                destination.y + Mathf.Sin(_hoverTime) * hoverAmplitude);

            _rigidbody.MovePosition(targetPosition);
            _animationController?.SetFacing(direction);
            _animationController?.UpdateLocomotion(Mathf.Abs(direction) > 0.01f ? direction : 0f, true);
            _audioController?.UpdateFootsteps(false, false);
        }

        private IEnumerator AttackRoutine()
        {
            _isAttacking = true;
            _rigidbody.linearVelocity = new Vector2(0f, _rigidbody.linearVelocity.y);
            _animationController?.PlayAttack();

            yield return new WaitForSeconds(0.2f);

            if (_player != null && Vector3.Distance(transform.position, _player.position) <= detectionRange + 0.1f)
            {
                _playerHealth.TakeDamage(attackDamage);
            }

            yield return new WaitForSeconds(attackCooldown);
            _isAttacking = false;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_supportsAttackState || _isDead)
            {
                return;
            }

            DemoPlayerHealth collidedPlayer = collision.collider.GetComponent<DemoPlayerHealth>() ??
                                             collision.collider.GetComponentInParent<DemoPlayerHealth>();

            if (collidedPlayer == null)
            {
                return;
            }

            collidedPlayer.TakeDamage(attackDamage);
            TakeHit(collidedPlayer.GetComponent<DemoPlayerPowerUpController>());
        }

        private EnemyMovementMode ResolveMovementMode()
        {
            string enemyName = name.ToLowerInvariant();

            if (enemyName.Contains("acai") || enemyName.Contains("copo") || enemyName.Contains("uva"))
            {
                return EnemyMovementMode.AerialHover;
            }

            return EnemyMovementMode.GroundPatrol;
        }

        private DemoPowerUpType ResolveGrantedPowerUp()
        {
            string enemyName = name.ToLowerInvariant();

            if (enemyName.Contains("acai"))
            {
                return DemoPowerUpType.AcaiSpeed;
            }

            if (enemyName.Contains("copo"))
            {
                return DemoPowerUpType.CopoazuVitality;
            }

            if (enemyName.Contains("uva"))
            {
                return DemoPowerUpType.UvaShield;
            }

            if (enemyName.Contains("sacha"))
            {
                return DemoPowerUpType.SachaInchiDoubleJump;
            }

            return DemoPowerUpType.ChontaduroStrength;
        }
    }
}
