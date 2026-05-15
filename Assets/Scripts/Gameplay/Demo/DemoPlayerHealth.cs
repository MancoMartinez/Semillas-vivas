using System;
using UnityEngine;
using SemillasVivas.Systems.Audio;

namespace SemillasVivas.Gameplay.Demo
{
    public sealed class DemoPlayerHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 3;

        private DemoPlayerAnimationController _animationController;
        private DemoPlayerPowerUpController _powerUpController;
        private DemoCharacterAudioController _audioController;

        public event Action<int, int> HealthChanged;
        public event Action Died;

        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }
        
        public bool IsKnockedBack { get; private set; }

        public void Initialize(DemoPlayerAnimationController animationController)
        {
            _animationController = animationController;
            CurrentHealth = maxHealth;
            IsDead = false;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void SetPowerUpController(DemoPlayerPowerUpController powerUpController)
        {
            _powerUpController = powerUpController;
        }

        public void SetAudioController(DemoCharacterAudioController audioController)
        {
            _audioController = audioController;
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0)
            {
                return;
            }

            if (_animationController != null && _animationController.IsAttacking)
            {
                return;
            }

            if (_powerUpController != null && _powerUpController.TryConsumeShield())
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);

            _animationController?.FlashDamage();

            if (CurrentHealth <= 0)
            {
                TriggerDeath();
                return;
            }

            _audioController?.Play(GameAudioCue.PlayerHurt);
            _animationController.PlayHurt();
        }

        public void ApplyKnockback(Vector2 force)
        {
            if (IsDead) return;
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb == null) return;

            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            rb.AddForce(force, ForceMode2D.Impulse);

            StartCoroutine(KnockbackRoutine());
        }

        private System.Collections.IEnumerator KnockbackRoutine()
        {
            IsKnockedBack = true;
            yield return new WaitForSeconds(0.35f);
            IsKnockedBack = false;
        }

        public void Kill()
        {
            if (IsDead)
            {
                return;
            }

            CurrentHealth = 0;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            TriggerDeath();
        }

        public void IncreaseMaxHealth(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            maxHealth += amount;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void RestoreHealthSnapshot(int health)
        {
            if (IsDead)
            {
                return;
            }

            int clamped = Mathf.Clamp(health, 0, MaxHealth);
            if (clamped == CurrentHealth)
            {
                return;
            }

            CurrentHealth = clamped;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        private void TriggerDeath()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            _audioController?.StopAllLoops();
            _audioController?.Play(GameAudioCue.PlayerDeath);
            _animationController?.PlayDeath();
            Died?.Invoke();
        }
    }
}
