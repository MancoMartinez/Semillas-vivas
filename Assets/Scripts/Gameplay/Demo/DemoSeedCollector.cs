using System;
using UnityEngine;

namespace SemillasVivas.Gameplay.Demo
{
    public sealed class DemoSeedCollector : MonoBehaviour
    {
        public event Action<int> SeedCollected;

        public int CollectedSeeds { get; private set; }

        public void Initialize()
        {
            CollectedSeeds = 0;
            SeedCollected?.Invoke(CollectedSeeds);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.gameObject.name.Contains("Seed", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            CollectedSeeds++;
            SeedCollected?.Invoke(CollectedSeeds);
            other.gameObject.SetActive(false);
        }
    }
}
