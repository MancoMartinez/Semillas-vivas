using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SemillasVivas.Gameplay.Demo.UI
{
    public sealed class DemoGameplayHud
    {
        private readonly TMP_Text _seedCounterText;
        private readonly Image _healthFillImage;

        public DemoGameplayHud(Transform canvasRoot, TMP_Text seedCounterText, string healthBarObjectName)
        {
            _seedCounterText = seedCounterText;
            _healthFillImage = FindRequiredHealthBar(canvasRoot, healthBarObjectName);
        }

        public void SetSeedCount(int seedCount)
        {
            _seedCounterText.text = $"Seeds : {seedCount}";
        }

        public void SetHealth(int currentHealth, int maxHealth)
        {
            float normalizedHealth = maxHealth <= 0 ? 0f : (float)currentHealth / maxHealth;
            _healthFillImage.fillAmount = Mathf.Clamp01(normalizedHealth);
        }

        private static Image FindRequiredHealthBar(Transform canvasRoot, string healthBarObjectName)
        {
            Transform healthBarTransform = canvasRoot.Find(healthBarObjectName);

            if (healthBarTransform == null)
            {
                throw new MissingReferenceException($"Health bar '{healthBarObjectName}' was not found under canvas '{canvasRoot.name}'.");
            }

            Image healthBarImage = healthBarTransform.GetComponent<Image>();

            if (healthBarImage == null)
            {
                throw new MissingReferenceException($"Health bar '{healthBarObjectName}' does not have an Image component.");
            }

            return healthBarImage;
        }
    }
}
