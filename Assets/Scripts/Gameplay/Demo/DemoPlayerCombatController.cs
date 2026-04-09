using UnityEngine;
using UnityEngine.InputSystem;

namespace SemillasVivas.Gameplay.Demo
{
    public sealed class DemoPlayerCombatController : MonoBehaviour
    {
        [SerializeField] private Vector2 attackBoxSize = new(1.4f, 1f);
        [SerializeField] private float attackOffset = 0.75f;

        private DemoPlayerAnimationController _animationController;
        private DemoPlayerPowerUpController _powerUpController;

        public void Initialize(DemoPlayerAnimationController animationController, DemoPlayerPowerUpController powerUpController)
        {
            _animationController = animationController;
            _powerUpController = powerUpController;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame)
            {
                return;
            }

            TryHitEnemy();
        }

        private void TryHitEnemy()
        {
            float facingDirection = _animationController != null ? _animationController.FacingDirection : 1f;
            float rangeMultiplier = _powerUpController != null ? _powerUpController.CurrentAttackRange : 1f;
            Vector2 center = (Vector2)transform.position + Vector2.right * (attackOffset * facingDirection * rangeMultiplier);
            Vector2 size = new(attackBoxSize.x * rangeMultiplier, attackBoxSize.y);

            Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);

            foreach (Collider2D hit in hits)
            {
                DemoEnemyController enemy = hit.GetComponent<DemoEnemyController>();

                if (enemy == null)
                {
                    enemy = hit.GetComponentInParent<DemoEnemyController>();
                }

                if (enemy == null)
                {
                    continue;
                }

                enemy.TakeHit();
            }
        }
    }
}
