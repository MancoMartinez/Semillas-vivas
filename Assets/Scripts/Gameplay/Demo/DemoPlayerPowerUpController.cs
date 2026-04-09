using UnityEngine;

namespace SemillasVivas.Gameplay.Demo
{
    public enum DemoPowerUpType
    {
        AcaiSpeed,
        CopoazuVitality,
        UvaShield,
        SachaInchiDoubleJump,
        ChontaduroStrength,
    }

    public sealed class DemoPlayerPowerUpController : MonoBehaviour
    {
        [SerializeField] private float defaultMoveSpeed = 4f;
        [SerializeField] private float acaiMoveSpeed = 6.5f;
        [SerializeField] private float defaultAttackRange = 1f;
        [SerializeField] private float chontaduroAttackRange = 1.8f;

        private DemoPlayerHealth _playerHealth;
        private bool _hasShield;
        private bool _hasDoubleJump;

        public float CurrentMoveSpeed { get; private set; }
        public float CurrentAttackRange { get; private set; }
        public int MaxJumpCount => _hasDoubleJump ? 2 : 1;

        public void Initialize(DemoPlayerHealth playerHealth)
        {
            _playerHealth = playerHealth;
            CurrentMoveSpeed = defaultMoveSpeed;
            CurrentAttackRange = defaultAttackRange;
            _hasShield = false;
            _hasDoubleJump = false;
        }

        public void Apply(DemoPowerUpType powerUpType)
        {
            switch (powerUpType)
            {
                case DemoPowerUpType.AcaiSpeed:
                    CurrentMoveSpeed = acaiMoveSpeed;
                    break;
                case DemoPowerUpType.CopoazuVitality:
                    _playerHealth?.IncreaseMaxHealth(1);
                    break;
                case DemoPowerUpType.UvaShield:
                    _hasShield = true;
                    break;
                case DemoPowerUpType.SachaInchiDoubleJump:
                    _hasDoubleJump = true;
                    break;
                case DemoPowerUpType.ChontaduroStrength:
                    CurrentAttackRange = chontaduroAttackRange;
                    break;
            }
        }

        public bool TryConsumeShield()
        {
            if (!_hasShield)
            {
                return false;
            }

            _hasShield = false;
            return true;
        }
    }
}
