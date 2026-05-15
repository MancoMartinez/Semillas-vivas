using UnityEngine;
using SemillasVivas.Systems.Audio;

namespace SemillasVivas.Gameplay.Demo
{
    public enum DemoPowerUpType
    {
        None,
        AcaiSpeed,
        CopoazuVitality,
        UvaShield,
        SachaInchiDoubleJump,
        ChontaduroStrength,
        CocaSlowdown,
    }

    public sealed class DemoPlayerPowerUpController : MonoBehaviour
    {
        [SerializeField] private float defaultMoveSpeed = 4f;
        [SerializeField] private float acaiMoveSpeed = 6.5f;
        [SerializeField] private float defaultAttackRange = 1f;
        [SerializeField] private float chontaduroAttackRange = 1.8f;
        [SerializeField] private float highJumpMultiplier = 1.3f;

        private DemoPlayerHealth _playerHealth;
        private DemoCharacterAudioController _audioController;
        private bool _hasShield;
        private bool _hasHighJump;

        public float CurrentMoveSpeed { get; private set; }
        public float CurrentAttackRange { get; private set; }
        public float CurrentJumpMultiplier => _hasHighJump ? highJumpMultiplier : 1f;
        public bool HasHighJump => _hasHighJump;

        public void Initialize(DemoPlayerHealth playerHealth)
        {
            _playerHealth = playerHealth;
            CurrentMoveSpeed = defaultMoveSpeed;
            CurrentAttackRange = defaultAttackRange;
            _hasShield = false;
            _hasHighJump = false;
        }

        public void SetAudioController(DemoCharacterAudioController audioController)
        {
            _audioController = audioController;
        }

        public void Apply(DemoPowerUpType powerUpType)
        {
            switch (powerUpType)
            {
                case DemoPowerUpType.None:
                    break;
                case DemoPowerUpType.AcaiSpeed:
                    CurrentMoveSpeed = acaiMoveSpeed;
                    _audioController?.Play(GameAudioCue.PowerUp);
                    break;
                case DemoPowerUpType.CopoazuVitality:
                    _playerHealth?.IncreaseMaxHealth(1);
                    _audioController?.Play(GameAudioCue.Heal);
                    break;
                case DemoPowerUpType.UvaShield:
                    _hasShield = true;
                    _audioController?.Play(GameAudioCue.PowerUp);
                    break;
                case DemoPowerUpType.SachaInchiDoubleJump:
                    if (!_hasHighJump)
                    {
                        _hasHighJump = true;
                        _audioController?.Play(GameAudioCue.PowerUp);
                    }
                    break;
                case DemoPowerUpType.ChontaduroStrength:
                    CurrentAttackRange = chontaduroAttackRange;
                    _audioController?.Play(GameAudioCue.PowerUp);
                    break;
                case DemoPowerUpType.CocaSlowdown:
                    _audioController?.Play(GameAudioCue.PowerUp);
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
