using System;
using SemillasVivas.Gameplay.Demo.UI;
using SemillasVivas.Systems.Audio;
using UnityEngine;

namespace SemillasVivas.Gameplay.Demo
{
    public sealed class DemoSeedCollector : MonoBehaviour
    {
        private DemoPlayerPowerUpController _powerUpController;
        private DemoCharacterAudioController _audioController;
        private DemoGameplayUiController _uiController;
        private DemoLevelFlowController _levelFlowController;
        private ISeedPickupEffect _seedPickupEffect;
        private DemoPowerUpType _seedPowerUpType;
        private string _seedMessage;
        private int _totalSeeds;
        private bool _introMessageShown;

        public event Action<int> SeedCollected;

        public int CollectedSeeds { get; private set; }

        public void Initialize(
            int totalSeeds,
            DemoPlayerPowerUpController powerUpController,
            DemoCharacterAudioController audioController,
            DemoGameplayUiController uiController,
            DemoLevelFlowController levelFlowController,
            ISeedPickupEffect seedPickupEffect = null,
            DemoPowerUpType seedPowerUpType = DemoPowerUpType.SachaInchiDoubleJump,
            string seedMessage = "Inchi te ayuda a llegar más alto. Ahora puedes saltar mejor. Prepárate para lo que viene.")
        {
            _totalSeeds = Mathf.Max(0, totalSeeds);
            _powerUpController = powerUpController;
            _audioController = audioController;
            _uiController = uiController;
            _levelFlowController = levelFlowController;
            _seedPickupEffect = seedPickupEffect;
            _seedPowerUpType = seedPowerUpType;
            _seedMessage = seedMessage;
            CollectedSeeds = 0;
            _introMessageShown = false;
            SeedCollected?.Invoke(CollectedSeeds);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsSeed(other.gameObject))
            {
                return;
            }

            CollectedSeeds++;
            SeedCollected?.Invoke(CollectedSeeds);
            _powerUpController?.Apply(_seedPowerUpType);
            _seedPickupEffect?.ApplySeedEffect(3.5f);
            _audioController?.Play(GameAudioCue.ItemCollect);

            if (!_introMessageShown)
            {
                _introMessageShown = true;
                _uiController?.ShowSeedMessage(_seedMessage, 5f, true);
            }

            other.gameObject.SetActive(false);

        }

        private static bool IsSeed(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            if (target.name.Contains("Seed", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            Transform parent = target.transform.parent;
            return parent != null && parent.name.Equals("seeds", StringComparison.OrdinalIgnoreCase);
        }
    }
}
