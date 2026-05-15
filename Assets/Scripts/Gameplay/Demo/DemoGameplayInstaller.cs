using System;
using System.Collections;
using SemillasVivas.Gameplay.Demo.UI;
using SemillasVivas.Gameplay.Boss;
using SemillasVivas.Systems;
using SemillasVivas.Systems.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SemillasVivas.Gameplay.Demo
{
    public sealed class DemoGameplayInstaller : MonoBehaviour
    {
        private const string LevelSelectSamplerObjectName = "LevelSelectCharacterSampler";

        [SerializeField] private string playerObjectName = "PersonajeFinal";
        [SerializeField] private string canvasObjectName = "Canvas";
        [SerializeField] private string seedCounterObjectName = "SeedRecoletionText";
        [SerializeField] private string enemiesContainerObjectName = "Enemigos";
        [SerializeField] private string seedsContainerObjectName = "seeds";

        private DemoPlayerHealth _playerHealth;
        private DemoSeedCollector _seedCollector;
        private DemoGameplayUiController _uiController;
        private Transform _playerTransform;

        private void Awake()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;

            CleanupMenuSamplerArtifacts();
            ValidateScene();

            GameAudioService audioService = GameAudioService.EnsureInstance();
            
            GameObject playerObject = FindRequiredObject(playerObjectName, "PersonajeFinal", "Character");
            GameObject canvasObject = FindOrInstantiateCanvas(canvasObjectName);
            
            TMP_Text seedCounterText = FindComponentInChildrenByName<TMP_Text>(canvasObject.transform, seedCounterObjectName);
            if (seedCounterText == null)
                throw new MissingReferenceException($"No se encontró '{seedCounterObjectName}' (TMP_Text) bajo '{canvasObject.name}'. Verifica el nombre exacto en la jerarquía.");

            SetupCameraFollow();
            SetupBackgroundFollower();

            _uiController = GetOrAddComponent<DemoGameplayUiController>(canvasObject);
            _uiController.Initialize(seedCounterText);

            ShowLevelOneInstructions(canvasObject.transform, SceneManager.GetActiveScene().name);

            DemoPlayerAnimationController animationController = GetOrAddComponent<DemoPlayerAnimationController>(playerObject);
            _playerHealth = GetOrAddComponent<DemoPlayerHealth>(playerObject);
            DemoPlayerController playerController = GetOrAddComponent<DemoPlayerController>(playerObject);
            _seedCollector = GetOrAddComponent<DemoSeedCollector>(playerObject);
            DemoPlayerPowerUpController powerUpController = GetOrAddComponent<DemoPlayerPowerUpController>(playerObject);
            DemoPlayerCombatController combatController = GetOrAddComponent<DemoPlayerCombatController>(playerObject);
            DemoCharacterAudioController playerAudioController = GetOrAddComponent<DemoCharacterAudioController>(playerObject);
            DemoLevelFlowController levelFlowController = GetOrAddComponent<DemoLevelFlowController>(gameObject);
            LevelSeedData levelSeedData = LevelSeedCatalog.GetForScene(SceneManager.GetActiveScene().name);
            bool isBossScene = string.Equals(SceneManager.GetActiveScene().name, "Boss", System.StringComparison.OrdinalIgnoreCase);

            animationController.Initialize();
            _playerHealth.Initialize(animationController);
            _playerHealth.SetPowerUpController(powerUpController);
            _playerHealth.SetAudioController(playerAudioController);
            powerUpController.Initialize(_playerHealth);
            powerUpController.SetAudioController(playerAudioController);
            playerAudioController.Initialize(audioService, GameAudioCue.PlayerFootstep, supportsFootsteps: true);
            playerController.Initialize(animationController, _playerHealth, powerUpController, playerAudioController);
            combatController.Initialize(animationController, powerUpController, playerAudioController);
            _uiController.SetGameplayControllers(playerController, combatController);

            int totalSeeds = CountSeeds();
            float deathY = CalculateDeathY(playerObject.transform.position.y);

            _playerTransform = playerObject.transform;
            levelFlowController.Initialize(_playerHealth, _playerTransform, _uiController, deathY);
            ISeedPickupEffect seedPickupEffect = null;

            if (isBossScene)
            {
                BossFightController bossFightController = GetOrAddComponent<BossFightController>(gameObject);
                bossFightController.Initialize(_playerTransform, _playerHealth, _uiController, levelFlowController);
                seedPickupEffect = bossFightController;
            }

            _seedCollector.Initialize(
                totalSeeds,
                powerUpController,
                playerAudioController,
                _uiController,
                levelFlowController,
                seedPickupEffect,
                levelSeedData.PowerUpType,
                levelSeedData.IntroMessage);

            _uiController.SetLives(_playerHealth.CurrentHealth);
            _uiController.SetSeedCount(_seedCollector.CollectedSeeds);

            _playerHealth.HealthChanged += HandleHealthChanged;
            _seedCollector.SeedCollected += HandleSeedCollected;

            if (!isBossScene)
            {
                TrySetupEnemies(audioService);
            }
            TrySetupMovingPlatforms();

            MobileInputState.Reset();
            GameObject overlayObject = new GameObject("MobileControlsOverlay");
            overlayObject.AddComponent<MobileControlsOverlay>();
            WireMobileButtons(canvasObject.transform);

            WirePauseButtons(canvasObject.transform);

            if (!isBossScene)
            {
                SetupEndTrigger(levelFlowController);
            }
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
            _uiController?.SetLives(currentHealth);
        }

        private void HandleSeedCollected(int collectedSeeds)
        {
            _uiController?.SetSeedCount(collectedSeeds);
        }

        private void TrySetupEnemies(GameAudioService audioService)
        {
            Transform enemiesRoot = FindOptionalSceneTransform(enemiesContainerObjectName, "Enemigos", "enemigos");

            if (enemiesRoot == null)
            {
                Debug.LogWarning($"No se encontro el contenedor de enemigos '{enemiesContainerObjectName}'.");
                return;
            }

            for (int index = 0; index < enemiesRoot.childCount; index++)
            {
                Transform enemyTransform = enemiesRoot.GetChild(index);

                if (enemyTransform == null)
                {
                    continue;
                }

                ConfigureEnemy(enemyTransform.gameObject, audioService);
            }
        }

        private void ConfigureEnemy(GameObject enemyObject, GameAudioService audioService)
        {
            DisableIfPresent<DemoPlayerController>(enemyObject);
            DisableIfPresent<DemoPlayerHealth>(enemyObject);
            DisableIfPresent<DemoSeedCollector>(enemyObject);
            DisableIfPresent<DemoPlayerCombatController>(enemyObject);
            DisableIfPresent<DemoPlayerPowerUpController>(enemyObject);
            DisableIfPresent<DemoEnemyController>(enemyObject);

            string lowerName = enemyObject.name.ToLowerInvariant();
            bool isCopo       = lowerName.Contains("copo");
            bool isMurcielago = lowerName.Contains("murcielago") ||
                                lowerName.Contains("murciélago") || 
                                lowerName.Contains("bat");               

            if (isCopo)
            {
                ConfigureCopoEnemy(enemyObject);
            }
            else if (isMurcielago)
            {
                ConfigureMurcielagoEnemy(enemyObject);
            }
            else
            {
                DemoCharacterAudioController enemyAudioController = GetOrAddComponent<DemoCharacterAudioController>(enemyObject);
                enemyAudioController.Initialize(audioService, GameAudioCue.EnemyFootstep, supportsFootsteps: false);
                GetOrAddComponent<SimpleEnemyPatrol>(enemyObject);
            }
        }

        private void ConfigureCopoEnemy(GameObject enemyObject)
        {
            CopoEnemy copo = GetOrAddComponent<CopoEnemy>(enemyObject);

            copo.Setup(_playerTransform, _playerHealth);

            Debug.Log($"[Installer] Copo '{enemyObject.name}' configurado como enemigo flotador.");
        }

        private void ConfigureMurcielagoEnemy(GameObject enemyObject)
        {
            MurcielagoEnemy murcielago = GetOrAddComponent<MurcielagoEnemy>(enemyObject);
            murcielago.Setup(_playerHealth);

            Debug.Log($"[Installer] Murciélago '{enemyObject.name}' configurado como enemigo flotador por contacto.");
        }

        private static void TrySetupMovingPlatforms()
        {
            string[] tags = { "TroncoH", "TroncoV", "TroncoD" };
            int count = 0;

            foreach (string tag in tags)
            {
                GameObject[] platforms;

                try
                {
                    platforms = GameObject.FindGameObjectsWithTag(tag);
                }
                catch (UnityException)
                {
                    
                    Debug.LogWarning($"[MovingPlatforms] Tag '{tag}' no existe en el proyecto. " +
                                     "Créalo en Edit → Project Settings → Tags and Layers.");
                    continue;
                }

                foreach (GameObject platform in platforms)
                {
                    if (platform == null)
                    {
                        continue;
                    }

                    GetOrAddComponent<MovingPlatform>(platform);
                    count++;
                    Debug.Log($"[MovingPlatforms] '{platform.name}' configurado como {tag}.");
                }
            }

            if (count == 0)
            {
                Debug.Log("[MovingPlatforms] No se encontraron objetos con tag TroncoH/TroncoV en esta escena.");
            }
        }

        private int CountSeeds()
        {
            Transform seedsRoot = FindOptionalSceneTransform(seedsContainerObjectName, "seeds", "Seeds");

            if (seedsRoot == null)
            {
                return 0;
            }

            int count = 0;

            for (int index = 0; index < seedsRoot.childCount; index++)
            {
                Transform child = seedsRoot.GetChild(index);

                if (child != null && child.name.Contains("Seed"))
                {
                    count++;
                }
            }

            return count;
        }

        private static float CalculateDeathY(float playerStartY)
        {
            Collider2D[] colliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
            float minGroundY = playerStartY;

            for (int index = 0; index < colliders.Length; index++)
            {
                Collider2D collider = colliders[index];

                if (collider == null || !collider.gameObject.layer.Equals(LayerMask.NameToLayer("Ground")))
                {
                    continue;
                }

                minGroundY = Mathf.Min(minGroundY, collider.bounds.min.y);
            }

            return minGroundY - 2f;
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

        private static GameObject FindOrInstantiateCanvas(string canvasName)
        {
            
            GameObject existing = GameObject.Find(canvasName);
            if (existing != null)
            {
                return existing;
            }

            string resourcePath = $"Prefabs/{canvasName}";
            GameObject prefab = Resources.Load<GameObject>(resourcePath);

            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab);
                instance.name = canvasName; 
                Debug.Log($"[Installer] Canvas instanciado desde Resources/{resourcePath}.");
                return instance;
            }

            throw new MissingReferenceException(
                $"Canvas '{canvasName}' no encontrado en la escena ni en Resources/{resourcePath}. " +
                $"Crea el prefab en Assets/Resources/Prefabs/{canvasName}.prefab " +
                $"o añade el Canvas a la escena manualmente.");
        }

        private static GameObject FindRequiredObject(params string[] objectNames)
        {
            for (int index = 0; index < objectNames.Length; index++)
            {
                GameObject target = GameObject.Find(objectNames[index]);

                if (target != null)
                {
                    return target;
                }
            }

            throw new MissingReferenceException($"Required object '{string.Join("' or '", objectNames)}' was not found in DemoGameplay scene.");
        }

        private static T FindComponentInChildrenByName<T>(Transform root, string childName) where T : Component
        {
            T[] all = root.GetComponentsInChildren<T>(includeInactive: true);

            for (int index = 0; index < all.Length; index++)
            {
                if (all[index].gameObject.name == childName)
                {
                    return all[index];
                }
            }

            return null;
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

        private Transform FindOptionalSceneTransform(params string[] objectNames)
        {
            for (int index = 0; index < objectNames.Length; index++)
            {
                GameObject target = FindSceneObjectByName(objectNames[index]);

                if (target != null)
                {
                    return target.transform;
                }
            }

            return null;
        }

        private GameObject FindSceneObjectByName(string objectName)
        {
            GameObject activeObject = GameObject.Find(objectName);

            if (activeObject != null)
            {
                return activeObject;
            }

            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            for (int index = 0; index < allObjects.Length; index++)
            {
                GameObject candidate = allObjects[index];

                if (candidate == null || candidate.scene != gameObject.scene || candidate.name != objectName)
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        private static void WireMobileButtons(Transform canvasRoot)
        {
            string[] jumpNames     = { "Jump", "Saltar", "BtnJump", "BtnSaltar" };
            string[] attackNames   = { "Attack", "Atacar", "BtnAttack", "BtnAtacar" };
            string[] joystickNames = { "Joystick", "joystick", "Jostick", "VirtualJoystick" };

            GameObject jumpObject     = FindChildByNames(canvasRoot, jumpNames);
            GameObject attackObject   = FindChildByNames(canvasRoot, attackNames);
            GameObject joystickObject = FindChildByNames(canvasRoot, joystickNames);

            MobileControlsOverlay.WireExistingButton(jumpObject, MobileButtonAction.Jump);
            MobileControlsOverlay.WireExistingButton(attackObject, MobileButtonAction.Attack);

            MobileControlsOverlay.WireExistingJoystick(joystickObject);
        }

        private static GameObject FindChildByNames(Transform root, string[] names)
        {
            for (int index = 0; index < names.Length; index++)
            {
                Transform found = root.Find(names[index]);

                if (found != null)
                {
                    return found.gameObject;
                }
            }

            return null;
        }

        private static void WirePauseButtons(Transform canvasRoot)
        {
            GameObject pausePanel    = FindChildRecursive(canvasRoot, "PausePanel");

            GameObject settingsPanel = FindChildRecursive(canvasRoot, "SettingsScreen")
                                       ?? FindChildRecursive(canvasRoot, "SettingsPanel");

            GameObject settingGo = FindChildRecursive(canvasRoot, "Settings")
                                   ?? FindChildRecursive(canvasRoot, "Setting");
            if (settingGo != null && pausePanel != null)
            {
                Button settingBtn = settingGo.GetComponent<Button>() ?? settingGo.AddComponent<Button>();
                settingBtn.onClick.RemoveAllListeners();
                settingBtn.onClick.AddListener(() =>
                {
                    DemoGameplayUiController uiController = canvasRoot.GetComponent<DemoGameplayUiController>();
                    uiController?.SetGameplayHudVisible(false);
                    pausePanel.SetActive(true);
                    Time.timeScale = 0f;
                });
            }

            if (pausePanel != null)
            {
                
                UnityEngine.Events.UnityAction resumeAction = () =>
                {
                    pausePanel.SetActive(false);
                    DemoGameplayUiController uiController = canvasRoot.GetComponent<DemoGameplayUiController>();
                    uiController?.SetGameplayHudVisible(true);
                    Time.timeScale = 1f;
                };

                BindButton(pausePanel.transform, new[] { "Back", "Volver" }, resumeAction, bringToFront: true);

                BindButton(pausePanel.transform, new[] { "Reanudar", "Resume" }, resumeAction);

                BindButton(pausePanel.transform, new[] { "Reiniciar", "Restart" }, () =>
                {
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                });

                BindButton(pausePanel.transform, new[] { "Salir", "Exit" }, () =>
                {
                    Time.timeScale = 1f;
                    PlayerPrefs.SetInt("NavigateToMenu", 1);
                    PlayerPrefs.Save();
                    SceneManager.LoadScene("MainMenu");
                });

                if (settingsPanel != null)
                {
                    BindButton(pausePanel.transform, new[] { "Ajustes", "Settings" }, () =>
                    {
                        pausePanel.SetActive(false);
                        settingsPanel.SetActive(true);
                        DemoGameplayUiController uiController = canvasRoot.GetComponent<DemoGameplayUiController>();
                        uiController?.SetGameplayHudVisible(false);
                    });

                    BindButton(settingsPanel.transform, new[] { "Back", "Volver", "Atras" }, () =>
                    {
                        settingsPanel.SetActive(false);
                        pausePanel.SetActive(true);
                        DemoGameplayUiController uiController = canvasRoot.GetComponent<DemoGameplayUiController>();
                        uiController?.SetGameplayHudVisible(false);
                    }, bringToFront: true);

                    SettingsWirer.WireSliders(settingsPanel.transform);

                    settingsPanel.SetActive(false);
                }

                pausePanel.SetActive(false);
            }
            else
            {
                Debug.LogWarning("WirePauseButtons: no encontré 'PausePanel' en el Canvas.");
            }
        }

        private static void SetupEndTrigger(DemoLevelFlowController levelFlowController)
        {
            GameObject endObject = GameObject.Find("END");

            if (endObject == null)
            {
                Debug.LogWarning("SetupEndTrigger: no encontré un objeto llamado 'END' en la escena.");
                return;
            }

            DemoLevelEndTrigger trigger = endObject.GetComponent<DemoLevelEndTrigger>()
                                          ?? endObject.AddComponent<DemoLevelEndTrigger>();
            trigger.Setup(levelFlowController);
        }

        private static void BindButton(
            Transform root,
            string[] names,
            UnityEngine.Events.UnityAction action,
            bool bringToFront = false)
        {
            for (int index = 0; index < names.Length; index++)
            {
                GameObject found = FindChildRecursive(root, names[index]);

                if (found == null)
                {
                    continue;
                }

                Button btn = found.GetComponent<Button>() ?? found.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(action);

                if (bringToFront)
                {
                    found.transform.SetAsLastSibling();
                }

                return;
            }
        }

        private static GameObject FindChildRecursive(Transform root, string targetName)
        {
            if (root == null) return null;

            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);

                if (child.name == targetName)
                {
                    return child.gameObject;
                }

                GameObject nested = FindChildRecursive(child, targetName);

                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private void ValidateScene()
        {
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            System.Text.StringBuilder sb = new();
            sb.AppendLine($"[SceneValidation] ══ {scene} ══");

            string[] playerNames = { playerObjectName, "PersonajeFinal", "Character" };
            bool playerFound = false;
            foreach (string n in playerNames)
            {
                if (GameObject.Find(n) != null) { sb.AppendLine($"  ✓ Player: '{n}'"); playerFound = true; break; }
            }
            if (!playerFound) sb.AppendLine($"  ✗ Player NOT FOUND — esperado: '{playerObjectName}' (o PersonajeFinal, Character)");

            bool canvasFound = GameObject.Find(canvasObjectName) != null;
            sb.AppendLine(canvasFound ? $"  ✓ Canvas: '{canvasObjectName}'" : $"  ✗ Canvas NOT FOUND — esperado: '{canvasObjectName}'");

            string[] seedNames = { seedsContainerObjectName, "seeds", "Seeds" };
            bool seedsFound = false;
            foreach (string n in seedNames)
            {
                if (GameObject.Find(n) != null) { sb.AppendLine($"  ✓ Seeds container: '{n}'"); seedsFound = true; break; }
            }
            if (!seedsFound) sb.AppendLine($"  ✗ Seeds container NOT FOUND — esperado: '{seedsContainerObjectName}'");

            string[] enemyNames = { enemiesContainerObjectName, "Enemigos", "enemigos" };
            bool enemiesFound = false;
            foreach (string n in enemyNames)
            {
                if (GameObject.Find(n) != null) { sb.AppendLine($"  ✓ Enemies container: '{n}'"); enemiesFound = true; break; }
            }
            if (!enemiesFound) sb.AppendLine($"  ~ Enemies container NOT FOUND — esto es opcional si el nivel no tiene enemigos");

            bool endFound = GameObject.Find("END") != null;
            sb.AppendLine(endFound ? "  ✓ END trigger encontrado" : "  ✗ END trigger NOT FOUND — agrega un GameObject llamado 'END' con Collider2D (Is Trigger)");

            bool camFound = GameObject.Find("Main Camera") != null;
            sb.AppendLine(camFound ? "  ✓ Main Camera encontrada" : "  ✗ Main Camera NOT FOUND — nombra la cámara 'Main Camera'");

            sb.AppendLine(GameAudioService.Instance != null ? "  ✓ GameAudioService activo" : "  ~ GameAudioService no encontrado (se crea automáticamente)");

            int troncoH = 0, troncoV = 0, troncoD = 0;
            try { troncoH = GameObject.FindGameObjectsWithTag("TroncoH").Length; } catch (UnityException) { }
            try { troncoV = GameObject.FindGameObjectsWithTag("TroncoV").Length; } catch (UnityException) { }
            try { troncoD = GameObject.FindGameObjectsWithTag("TroncoD").Length; } catch (UnityException) { }
            sb.AppendLine($"  ~ Plataformas móviles: {troncoH} TroncoH, {troncoV} TroncoV, {troncoD} TroncoD");

            Debug.Log(sb.ToString());
        }

        private void ShowLevelOneInstructions(Transform canvasRoot, string sceneName)
        {
            bool isLevel1 = string.Equals(sceneName, "DemoGameplay", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(sceneName, "lvl1", StringComparison.OrdinalIgnoreCase);

            GameObject panel = FindChildRecursive(canvasRoot, "InstruccionesLvl1");

            if (panel == null)
            {
                if (isLevel1)
                {
                    Debug.LogWarning("[Installer] InstruccionesLvl1 no encontrado en el Canvas. " +
                                     "Asegúrate de que el GameObject se llame exactamente 'InstruccionesLvl1'.");
                }
                return;
            }

            if (!isLevel1)
            {
                
                panel.SetActive(false);
                return;
            }

            Debug.Log("[Installer] InstruccionesLvl1 encontrado → mostrando 10 s con pausa.");
            panel.SetActive(true);
            Time.timeScale = 0f; 
            StartCoroutine(HideInstructionsRoutine(panel));
        }

        private static IEnumerator HideInstructionsRoutine(GameObject panel)
        {
            
            yield return new WaitForSecondsRealtime(20f);

            if (panel != null)
            {
                panel.SetActive(false);
            }

            Time.timeScale = 1f; 
            Debug.Log("[Installer] InstruccionesLvl1 cerrado — juego reanudado.");
        }

        private void SetupCameraFollow()
        {
            GameObject cameraObject = GameObject.Find("Main Camera");

            if (cameraObject != null)
            {
                GetOrAddComponent<DemoCameraFollow2D>(cameraObject);
            }
        }

        private void SetupBackgroundFollower()
        {
            string[] backgroundNames = { "Plane", "Background", "Fondo", "BG", "Sky", "Cielo" };
            GameObject backgroundObject = null;

            for (int index = 0; index < backgroundNames.Length; index++)
            {
                backgroundObject = GameObject.Find(backgroundNames[index]);

                if (backgroundObject != null)
                {
                    break;
                }
            }

            if (backgroundObject != null)
            {
                GetOrAddComponent<DemoBackgroundFollower>(backgroundObject);
            }
        }
    }
}
