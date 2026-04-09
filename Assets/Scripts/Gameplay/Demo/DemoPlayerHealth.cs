using System;
using UnityEngine;

namespace SemillasVivas.Gameplay.Demo
{
    public sealed class DemoPlayerHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 3;

        private DemoPlayerAnimationController _animationController;
        private DemoPlayerPowerUpController _powerUpController;

        public event Action<int, int> HealthChanged;

        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

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

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0)
            {
                return;
            }

            if (_powerUpController != null && _powerUpController.TryConsumeShield())
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (CurrentHealth <= 0)
            {
                IsDead = true;
                _animationController.PlayDeath();
                return;
            }

            _animationController.PlayHurt();
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
    }
}
