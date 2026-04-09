using SemillasVivas.Gameplay.Demo.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SemillasVivas.Gameplay.Demo
{
    public sealed class DemoGameplayInstaller : MonoBehaviour
    {
        private const string LevelSelectSamplerObjectName = "LevelSelectCharacterSampler";

        [SerializeField] private string playerObjectName = "Character";
        [SerializeField] private string canvasObjectName = "Canvas";
        [SerializeField] private string seedCounterObjectName = "SeedRecoletionText";
        [SerializeField] private string healthBarObjectName = "Healthbar";
        [SerializeField] private string enemyObjectName = "Enemy";

        private DemoPlayerController _playerController;
        private DemoPlayerHealth _playerHealth;
        private DemoSeedCollector _seedCollector;
        private DemoGameplayHud _hud;

        private void Awake()
        {
            CleanupMenuSamplerArtifacts();

            GameObject playerObject = FindRequiredObject(playerObjectName);
            GameObject canvasObject = FindRequiredObject(canvasObjectName);
            TMP_Text seedCounterText = FindRequiredComponent<TMP_Text>(canvasObject.transform, seedCounterObjectName);

            DemoPlayerAnimationController animationController = GetOrAddComponent<DemoPlayerAnimationController>(playerObject);
            _playerHealth = GetOrAddComponent<DemoPlayerHealth>(playerObject);
            _playerController = GetOrAddComponent<DemoPlayerController>(playerObject);
            _seedCollector = GetOrAddComponent<DemoSeedCollector>(playerObject);
            DemoPlayerPowerUpController powerUpController = GetOrAddComponent<DemoPlayerPowerUpController>(playerObject);
            DemoPlayerCombatController combatController = GetOrAddComponent<DemoPlayerCombatController>(playerObject);

            animationController.Initialize();
            _playerHealth.Initialize(animationController);
            _playerHealth.SetPowerUpController(powerUpController);
            powerUpController.Initialize(_playerHealth);
            _playerController.Initialize(animationController, _playerHealth, powerUpController);
            combatController.Initialize(animationController, powerUpController);
            _seedCollector.Initialize();

            TrySetupEnemy(playerObject, enemyObjectName);
            SetupPowerUps();

            _hud = new DemoGameplayHud(canvasObject.transform, seedCounterText, healthBarObjectName);
            _hud.SetSeedCount(_seedCollector.CollectedSeeds);
            _hud.SetHealth(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);

            _playerHealth.HealthChanged += HandleHealthChanged;
            _seedCollector.SeedCollected += HandleSeedCollected;
        }

        private void OnDestroy()
        {
            if (_playerHealth != null)
            {
                _playerHealth.HealthChanged -= HandleHealthChanged;
            }

            if (_seedCollector != null)
            {
                _seedCollector.SeedCollected -= HandleSeedCollected;
            }
        }

        private void HandleHealthChanged(int currentHealth, int maxHealth)
        {
            _hud?.SetHealth(currentHealth, maxHealth);
        }

        private void HandleSeedCollected(int collectedSeeds)
        {
            _hud?.SetSeedCount(collectedSeeds);
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void DisableIfPresent<T>(GameObject target) where T : Behaviour
        {
            T component = target.GetComponent<T>();

            if (component != null)
            {
                component.enabled = false;
            }
        }

        private void TrySetupEnemy(GameObject playerObject, string targetEnemyObjectName)
        {
            GameObject enemyObject = GameObject.Find(targetEnemyObjectName);

            if (enemyObject == null)
            {
                Debug.LogWarning($"Optional enemy '{targetEnemyObjectName}' was not found. Enemy AI setup skipped.");
                return;
            }

            DisableIfPresent<DemoPlayerController>(enemyObject);
            DisableIfPresent<DemoPlayerHealth>(enemyObject);
            DisableIfPresent<DemoSeedCollector>(enemyObject);
            DisableIfPresent<DemoPlayerCombatController>(enemyObject);
            DisableIfPresent<DemoPlayerPowerUpController>(enemyObject);

            DemoPlayerAnimationController enemyAnimation = GetOrAddComponent<DemoPlayerAnimationController>(enemyObject);
            enemyAnimation.Initialize();

            DemoEnemyController enemyController = GetOrAddComponent<DemoEnemyController>(enemyObject);
            enemyController.Initialize(playerObject.transform, enemyAnimation);
        }

        private void SetupPowerUps()
        {
            List<GameObject> candidates = new();
            Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (Transform currentTransform in allTransforms)
            {
                string lowerName = currentTransform.name.ToLowerInvariant();

                if (lowerName.Contains("power") ||
                    lowerName.Contains("acai") ||
                    lowerName.Contains("copo") ||
                    lowerName.Contains("sacha") ||
                    lowerName.Contains("uva") ||
                    lowerName.Contains("chonta"))
                {
                    candidates.Add(currentTransform.gameObject);
                }
            }

            DemoPowerUpType[] fallbackOrder =
            {
                DemoPowerUpType.AcaiSpeed,
                DemoPowerUpType.CopoazuVitality,
                DemoPowerUpType.SachaInchiDoubleJump,
            };

            for (int index = 0; index < candidates.Count; index++)
            {
                GameObject candidate = candidates[index];
                DemoPowerUpPickup pickup = GetOrAddComponent<DemoPowerUpPickup>(candidate);
                pickup.Initialize(ResolvePowerUpType(candidate.name, fallbackOrder, index));
            }
        }

        private static DemoPowerUpType ResolvePowerUpType(string objectName, DemoPowerUpType[] fallbackOrder, int fallbackIndex)
        {
            string lowerName = objectName.ToLowerInvariant();

            if (lowerName.Contains("acai"))
            {
                return DemoPowerUpType.AcaiSpeed;
            }

            if (lowerName.Contains("copo"))
            {
                return DemoPowerUpType.CopoazuVitality;
            }

            if (lowerName.Contains("sacha"))
            {
                return DemoPowerUpType.SachaInchiDoubleJump;
            }

            if (lowerName.Contains("uva"))
            {
                return DemoPowerUpType.UvaShield;
            }

            if (lowerName.Contains("chonta"))
            {
                return DemoPowerUpType.ChontaduroStrength;
            }

            return fallbackOrder[Mathf.Clamp(fallbackIndex, 0, fallbackOrder.Length - 1)];
        }

        private static GameObject FindRequiredObject(string objectName)
        {
            GameObject target = GameObject.Find(objectName);

            if (target == null)
            {
                throw new MissingReferenceException($"Required object '{objectName}' was not found in DemoGameplay scene.");
            }

            return target;
        }

        private static T FindRequiredComponent<T>(Transform root, string childName) where T : Component
        {
            Transform child = root.Find(childName);

            if (child == null)
            {
                throw new MissingReferenceException($"Required child '{childName}' was not found under '{root.name}'.");
            }

            T component = child.GetComponent<T>();

            if (component == null)
            {
                throw new MissingReferenceException($"Required component '{typeof(T).Name}' was not found on '{childName}'.");
            }

            return component;
        }

        private static void CleanupMenuSamplerArtifacts()
        {
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            for (int index = 0; index < allObjects.Length; index++)
            {
                GameObject candidate = allObjects[index];

                if (candidate == null || candidate.name != LevelSelectSamplerObjectName)
                {
                    continue;
                }

                Destroy(candidate);
            }
        }
    }
}
