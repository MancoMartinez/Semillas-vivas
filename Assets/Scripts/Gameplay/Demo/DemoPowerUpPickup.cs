using UnityEngine;

namespace SemillasVivas.Gameplay.Demo
{
    public sealed class DemoPowerUpPickup : MonoBehaviour
    {
        [SerializeField] private DemoPowerUpType powerUpType;

        public void Initialize(DemoPowerUpType assignedPowerUpType)
        {
            powerUpType = assignedPowerUpType;

            Collider collider3D = GetComponent<Collider>();

            if (collider3D != null)
            {
                collider3D.isTrigger = true;
                return;
            }

            Collider2D collider2D = GetComponent<Collider2D>();

            if (collider2D != null)
            {
                collider2D.isTrigger = true;
                return;
            }

            throw new MissingComponentException($"Power-up '{name}' requires a Collider or Collider2D component.");
        }

        private void OnTriggerEnter(Collider other)
        {
            TryApplyTo(other.GetComponent<DemoPlayerPowerUpController>() ?? other.GetComponentInParent<DemoPlayerPowerUpController>());
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryApplyTo(other.GetComponent<DemoPlayerPowerUpController>() ?? other.GetComponentInParent<DemoPlayerPowerUpController>());
        }

        private void TryApplyTo(DemoPlayerPowerUpController powerUpController)
        {
            if (powerUpController == null)
            {
                return;
            }

            powerUpController.Apply(powerUpType);
            gameObject.SetActive(false);
        }
    }
}
