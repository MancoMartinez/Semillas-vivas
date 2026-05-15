using UnityEngine;
using UnityEngine.InputSystem;
using SemillasVivas.Systems.Audio;
using SemillasVivas.Gameplay.Boss;

namespace SemillasVivas.Gameplay.Demo
{
    public sealed class DemoPlayerCombatController : MonoBehaviour
    {
        [SerializeField] private Vector2 attackBoxSize = new(1.4f, 1f);
        [SerializeField] private float attackOffset = 0.75f;

        private DemoPlayerAnimationController _animationController;
        private DemoPlayerPowerUpController _powerUpController;
        private DemoCharacterAudioController _audioController;
        private bool _inputLocked;

        public void Initialize(
            DemoPlayerAnimationController animationController,
            DemoPlayerPowerUpController powerUpController,
            DemoCharacterAudioController audioController)
        {
            _animationController = animationController;
            _powerUpController = powerUpController;
            _audioController = audioController;
        }

        private void Update()
        {
            if (_inputLocked)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;

            bool attackPressed = (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                                 || MobileInputState.ConsumeAttack();

            if (!attackPressed)
            {
                return;
            }

            _animationController?.PlayAttack();
            TryHitEnemy();
        }

        private void TryHitEnemy()
        {
            _audioController?.Play(GameAudioCue.PlayerAttack);

            float facingDirection = _animationController != null ? _animationController.FacingDirection : 1f;
            float rangeMultiplier = _powerUpController != null ? _powerUpController.CurrentAttackRange : 1f;
            Vector2 center = (Vector2)transform.position + Vector2.right * (attackOffset * facingDirection * rangeMultiplier);
            Vector2 size = new(attackBoxSize.x * rangeMultiplier, attackBoxSize.y);

            Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);

            foreach (Collider2D hit in hits)
            {
                SimpleEnemyPatrol simpleEnemy =
                    hit.GetComponent<SimpleEnemyPatrol>() ??
                    hit.GetComponentInParent<SimpleEnemyPatrol>();

                if (simpleEnemy != null)
                {
                    simpleEnemy.TakeHit();
                    continue;
                }

                CopoEnemy copoEnemy =
                    hit.GetComponent<CopoEnemy>() ??
                    hit.GetComponentInParent<CopoEnemy>();

                if (copoEnemy != null)
                {
                    copoEnemy.TakeHit();
                    continue;
                }

                MurcielagoEnemy murcielago =
                    hit.GetComponent<MurcielagoEnemy>() ??
                    hit.GetComponentInParent<MurcielagoEnemy>();

                if (murcielago != null)
                {
                    murcielago.TakeHit();
                    continue;
                }

                CopoProjectile copoProjectile =
                    hit.GetComponent<CopoProjectile>() ??
                    hit.GetComponentInParent<CopoProjectile>();

                if (copoProjectile != null)
                {
                    copoProjectile.TakeHit();
                    continue;
                }

                DemoEnemyController legacyEnemy =
                    hit.GetComponent<DemoEnemyController>() ??
                    hit.GetComponentInParent<DemoEnemyController>();

                if (legacyEnemy != null)
                {
                    legacyEnemy.TakeHit(_powerUpController);
                    continue;
                }

                BossEnemyActor bossEnemy =
                    hit.GetComponent<BossEnemyActor>() ??
                    hit.GetComponentInParent<BossEnemyActor>();

                if (bossEnemy != null)
                {
                    bossEnemy.TakeHit();
                    continue;
                }

                MeleeOnlyEnemy meleeOnly =
                    hit.GetComponent<MeleeOnlyEnemy>() ??
                    hit.GetComponentInParent<MeleeOnlyEnemy>();

                if (meleeOnly != null)
                {
                    meleeOnly.TakeDamage(1);
                    continue;
                }

                SniperOnlyEnemy sniperOnly =
                    hit.GetComponent<SniperOnlyEnemy>() ??
                    hit.GetComponentInParent<SniperOnlyEnemy>();

                if (sniperOnly != null)
                {
                    sniperOnly.TakeDamage(1);
                    continue;
                }

                RangedMeleeEnemy rangedMelee =
                    hit.GetComponent<RangedMeleeEnemy>() ??
                    hit.GetComponentInParent<RangedMeleeEnemy>();

                if (rangedMelee != null)
                {
                    rangedMelee.TakeDamage(1);
                }
            }
        }

        public void SetInputLocked(bool isLocked)
        {
            _inputLocked = isLocked;

            if (_inputLocked)
            {
                MobileInputState.Reset();
            }
        }
    }
}
