using System.Collections;
using System.Collections.Generic;
using SemillasVivas.Gameplay.Demo;
using SemillasVivas.Gameplay.Demo.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SemillasVivas.Gameplay.Boss
{
    
    public sealed class BossFightController : MonoBehaviour, ISeedPickupEffect
    {
        
        private const int MainBossPhaseHealth  = 8;
        private const int Phase3BossHealth     = 12;

        private readonly List<BossEnemyActor> _activeEnemies = new();

        private DemoGameplayUiController  _uiController;
        private DemoLevelFlowController   _levelFlowController;
        private DemoPlayerHealth          _playerHealth;
        private Transform                 _player;
        private Transform                 _enemiesRoot;
        private Transform                 _phaseOneRoot;
        private Transform                 _phaseTwoRoot;
        private Transform                 _phaseThreeRoot;
        private GameObject                _projectileTemplate;
        private BossEnemyActor            _mainBoss;
        private Image                     _enemyBar;
        private Transform                 _shootPooler;

        private int   _totalPhaseHealth;
        private int   _remainingPhaseHealth;
        private float _slowAttacksUntil;
        private bool  _fightCompleted;

        public void Initialize(
            Transform player,
            DemoPlayerHealth playerHealth,
            DemoGameplayUiController uiController,
            DemoLevelFlowController levelFlowController)
        {
            _player              = player;
            _playerHealth        = playerHealth;
            _uiController        = uiController;
            _levelFlowController = levelFlowController;

#if !UNITY_EDITOR
            UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;
#endif

            _enemiesRoot    = FindSceneTransform("Enemigos");
            _phaseOneRoot   = FindSceneTransform("fase1");
            _phaseTwoRoot   = FindSceneTransform("fase 2") ?? FindSceneTransform("fase2");
            _phaseThreeRoot = FindSceneTransform("fase 3") ?? FindSceneTransform("fase3");
            _enemyBar       = FindEnemyBar();

            if (_phaseTwoRoot   != null) _phaseTwoRoot.gameObject.SetActive(false);
            if (_phaseThreeRoot != null) _phaseThreeRoot.gameObject.SetActive(false);

            SetupPhaseOneBoss();
            UpdateEnemyBar();
        }

        public float GetCurrentAttackCooldownMultiplier() =>
            Time.time <= _slowAttacksUntil ? 1.75f : 1f;

        public void ApplySeedEffect(float durationSeconds) =>
            _slowAttacksUntil = Mathf.Max(_slowAttacksUntil,
                                          Time.time + Mathf.Max(0.1f, durationSeconds));

        public bool AreAttacksSlowed() => Time.time <= _slowAttacksUntil;

        public void NotifyEnemyDamaged(BossEnemyActor enemy)
        {
            if (enemy == null) return;
            if (_remainingPhaseHealth > 0)
            {
                _remainingPhaseHealth = Mathf.Max(0, _remainingPhaseHealth - 1);
                UpdateEnemyBar();
            }
        }

        public void NotifyEnemyDefeated(BossEnemyActor enemy)
        {
            _activeEnemies.Remove(enemy);
            if (_fightCompleted) return;

            if (_mainBoss != null && enemy == _mainBoss)
            {
                _fightCompleted = true;
                _levelFlowController?.CompleteLevel();
                return;
            }

        }

        private void SetupPhaseOneBoss()
        {
            Transform bossTransform = FindSceneTransform("Boss1");

            _projectileTemplate = Resources.Load<GameObject>("Prefabs/shoot");
            if (_projectileTemplate == null)
            {
                _projectileTemplate = FindSceneObject("shoot");
                if (_projectileTemplate != null)
                    Debug.LogWarning("[BossFight] Prefab 'Resources/Prefabs/shoot' no encontrado. " +
                                     "Usando objeto de escena 'shoot' como fallback.");
                else
                    Debug.LogError("[BossFight] No se encontró el prefab 'shoot'. " +
                                   "Colócalo en Assets/Resources/Prefabs/shoot.prefab");
            }

            _shootPooler = FindSceneTransform("ShootPooler");
            if (_shootPooler == null)
                Debug.LogWarning("[BossFight] No se encontró 'ShootPooler'. " +
                                 "Los proyectiles se crearán bajo el boss.");

            if (bossTransform == null)
            {
                Debug.LogError("[BossFight] No se encontró 'Boss1' en la escena.");
                return;
            }

            RangedMeleeEnemy rangedMelee = bossTransform.GetComponent<RangedMeleeEnemy>();
            if (rangedMelee != null)
            {
                Debug.Log("[BossFight] Boss1 tiene RangedMeleeEnemy — omitiendo BossEnemyActor. " +
                          "La transición de fases la gestiona RangedMeleeEnemy.");

                rangedMelee.maxHealth = 8;

                _totalPhaseHealth     = rangedMelee.maxHealth;
                _remainingPhaseHealth = rangedMelee.maxHealth;
                UpdateEnemyBar();

                rangedMelee.OnHealthChanged += (current, max) =>
                {
                    _remainingPhaseHealth = current;
                    _totalPhaseHealth     = max;
                    UpdateEnemyBar();
                };

                rangedMelee.OnDied += OnRangedMeleeBoss1Died;

                if (_phaseTwoRoot == null && rangedMelee.nextPhaseObject != null)
                {
                    _phaseTwoRoot = rangedMelee.nextPhaseObject.transform;
                    Debug.Log($"[BossFight] _phaseTwoRoot resuelto desde nextPhaseObject: " +
                              $"'{_phaseTwoRoot.name}'");
                }

                if (_phaseTwoRoot != null)
                    _phaseTwoRoot.gameObject.SetActive(false);

                return;
            }

            _mainBoss = bossTransform.GetComponent<BossEnemyActor>();
            if (_mainBoss == null)
                _mainBoss = bossTransform.gameObject.AddComponent<BossEnemyActor>();

            _mainBoss.Setup(
                owner:            this,
                player:           _player,
                playerHealth:     _playerHealth,
                enemyRole:        BossEnemyRole.MainBoss,
                health:           MainBossPhaseHealth,
                speed:            2.25f,
                phase3Idle:       false,
                invulnerable:     false,
                destroyAfterDeath: false);

            _mainBoss.ConfigureProjectiles(_projectileTemplate, 4, _shootPooler);

            _activeEnemies.Clear();
            _activeEnemies.Add(_mainBoss);
            _totalPhaseHealth     = MainBossPhaseHealth;
            _remainingPhaseHealth = MainBossPhaseHealth;
        }

        private void OnRangedMeleeBoss1Died()
        {
            Debug.Log("[BossFight] Boss1 (RangedMeleeEnemy) derrotado — " +
                      "esperando activación de fase 2.");
            StartCoroutine(WatchPhase2Completion());
        }

        private IEnumerator WatchPhase2Completion()
        {
            
            float timeout = 10f;
            float elapsed = 0f;
            while (_phaseTwoRoot != null &&
                   !_phaseTwoRoot.gameObject.activeSelf &&
                   elapsed < timeout)
            {
                yield return new WaitForSeconds(0.25f);
                elapsed += 0.25f;
            }

            if (_phaseTwoRoot == null || !_phaseTwoRoot.gameObject.activeSelf)
            {
                Debug.Log("[BossFight] No se detectó fase 2 activa — completando nivel.");
                if (!_fightCompleted)
                {
                    _fightCompleted = true;
                    _levelFlowController?.CompleteLevel();
                }
                yield break;
            }

            Debug.Log("[BossFight] Fase 2 activada — monitoreando enemigos.");
            RefillPlayerHealthForNextPhase();

            MeleeOnlyEnemy[]   meleeEnemies  = _phaseTwoRoot.GetComponentsInChildren<MeleeOnlyEnemy>(true);
            SniperOnlyEnemy[]  sniperEnemies = _phaseTwoRoot.GetComponentsInChildren<SniperOnlyEnemy>(true);
            RangedMeleeEnemy[] rangedEnemies = _phaseTwoRoot.GetComponentsInChildren<RangedMeleeEnemy>(true);
            int total = meleeEnemies.Length + sniperEnemies.Length + rangedEnemies.Length;

            Debug.Log($"[BossFight] Fase 2: {meleeEnemies.Length} melee + " +
                      $"{sniperEnemies.Length} sniper + {rangedEnemies.Length} rangedMelee " +
                      $"= {total} enemigos.");

            if (total == 0)
            {
                Debug.LogWarning("[BossFight] Fase 2 no tiene enemigos reconocibles. " +
                                 "Completando nivel automáticamente.");
                if (!_fightCompleted)
                {
                    _fightCompleted = true;
                    _levelFlowController?.CompleteLevel();
                }
                yield break;
            }

            while (true)
            {
                int alive = 0;
                foreach (MeleeOnlyEnemy   e in meleeEnemies)  if (e != null) alive++;
                foreach (SniperOnlyEnemy  e in sniperEnemies) if (e != null) alive++;
                foreach (RangedMeleeEnemy e in rangedEnemies) if (e != null) alive++;

                if (alive == 0) break;

                yield return new WaitForSeconds(0.5f);
            }

            if (_phaseThreeRoot != null)
            {
                Debug.Log("[BossFight] Fase 2 completada — activando fase 3.");
                _phaseThreeRoot.gameObject.SetActive(true);
                RefillPlayerHealthForNextPhase();
                SetupPhaseThree();
            }
            else if (!_fightCompleted)
            {
                _fightCompleted = true;
                Debug.Log("[BossFight] Todos los enemigos de fase 2 derrotados — ¡nivel completado!");
                _levelFlowController?.CompleteLevel();
            }
        }

        private void SetupPhaseThree()
        {
            if (_phaseThreeRoot == null) return;

            _uiController?.ShowSeedMessage(
                "¡Elimina a todos los guardianes\npara poder derrotar al jefe!",
                3f,
                true);

            int bigBossLayerIdx   = LayerMask.NameToLayer("BigBoss");
            int finalBossLayerIdx = LayerMask.NameToLayer("FinalBoss");
            if (bigBossLayerIdx >= 0 && finalBossLayerIdx >= 0)
            {
                Physics2D.IgnoreLayerCollision(bigBossLayerIdx, finalBossLayerIdx, true);
            }
            
            if (finalBossLayerIdx >= 0)
            {
                Physics2D.IgnoreLayerCollision(finalBossLayerIdx, finalBossLayerIdx, true);
            }

            int finalBossLayer = finalBossLayerIdx;
            Transform explicitFinalBoss = null;
            foreach (Transform child in _phaseThreeRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child == null) continue;
                bool matchesName = child.name == "FinalBoss";
                bool matchesLayer = finalBossLayer >= 0 && child.gameObject.layer == finalBossLayer;
                if (!matchesName && !matchesLayer) continue;
                explicitFinalBoss = child;
                break;
            }

            if (explicitFinalBoss != null)
            {
                RangedMeleeEnemy existingRm = explicitFinalBoss.GetComponent<RangedMeleeEnemy>();
                if (existingRm != null) existingRm.enabled = false;

                BossEnemyActor explicitBoss = explicitFinalBoss.GetComponent<BossEnemyActor>()
                    ?? explicitFinalBoss.gameObject.AddComponent<BossEnemyActor>();

                explicitBoss.SetIdleAnimationName("fase3Idle");
                explicitBoss.Setup(
                    owner: this,
                    player: _player,
                    playerHealth: _playerHealth,
                    enemyRole: BossEnemyRole.MainBoss,
                    health: Phase3BossHealth,
                    speed: 2.0f,
                    phase3Idle: true,
                    invulnerable: true,
                    destroyAfterDeath: false);

                explicitBoss.ConfigureProjectiles(_projectileTemplate, 4, _shootPooler);

                _mainBoss = explicitBoss;
                _activeEnemies.Clear();
                _activeEnemies.Add(explicitBoss);
                _totalPhaseHealth = Phase3BossHealth;
                _remainingPhaseHealth = Phase3BossHealth;
                UpdateEnemyBar();

                StartCoroutine(WatchPhase3GuardiansAndActivateBoss(explicitFinalBoss, explicitBoss));
                return;
            }

            Transform mainBossTransform = null;
            Transform bigBoss = _phaseThreeRoot.Find("BigBoss");
            if (bigBoss != null)
            {
                mainBossTransform = bigBoss.Find("Boss1") ?? bigBoss.GetChild(0);
            }

            if (mainBossTransform == null)
            {
                
                RangedMeleeEnemy[] rmArr = _phaseThreeRoot.GetComponentsInChildren<RangedMeleeEnemy>(true);
                if (rmArr.Length > 0) mainBossTransform = rmArr[0].transform;
            }

            BossEnemyActor phase3Boss = null;

            if (mainBossTransform != null)
            {
                
                RangedMeleeEnemy existingRm = mainBossTransform.GetComponent<RangedMeleeEnemy>();
                if (existingRm != null) existingRm.enabled = false;

                phase3Boss = mainBossTransform.GetComponent<BossEnemyActor>()
                          ?? mainBossTransform.gameObject.AddComponent<BossEnemyActor>();

                phase3Boss.SetIdleAnimationName("fase3Idle");

                phase3Boss.Setup(
                    owner:             this,
                    player:            _player,
                    playerHealth:      _playerHealth,
                    enemyRole:         BossEnemyRole.MainBoss,
                    health:            Phase3BossHealth,
                    speed:             2.0f,
                    phase3Idle:        true,   
                    invulnerable:      true,   
                    destroyAfterDeath: false);

                phase3Boss.ConfigureProjectiles(_projectileTemplate, 4, _shootPooler);

                _mainBoss = phase3Boss;
                _activeEnemies.Clear();
                _activeEnemies.Add(phase3Boss);
                _totalPhaseHealth     = Phase3BossHealth;
                _remainingPhaseHealth = Phase3BossHealth;
                UpdateEnemyBar();
            }

            StartCoroutine(WatchPhase3GuardiansAndActivateBoss(mainBossTransform, phase3Boss));
        }

        private IEnumerator WatchPhase3GuardiansAndActivateBoss(
            Transform mainBossTransform, BossEnemyActor phase3Boss)
        {
            if (_phaseThreeRoot == null) yield break;

            int bigBossLayer = LayerMask.NameToLayer("bigboss");
            if (bigBossLayer < 0) bigBossLayer = LayerMask.NameToLayer("BigBoss");
            int shootBossLayer = LayerMask.NameToLayer("ShootBoss");

            var explicitMeleeGuardians = new System.Collections.Generic.List<MeleeOnlyEnemy>();
            var explicitSniperGuardians = new System.Collections.Generic.List<SniperOnlyEnemy>();

            foreach (MeleeOnlyEnemy e in _phaseThreeRoot.GetComponentsInChildren<MeleeOnlyEnemy>(true))
            {
                if (e == null) continue;
                if (mainBossTransform != null && e.transform == mainBossTransform) continue;
                if (bigBossLayer >= 0 && e.gameObject.layer != bigBossLayer) continue;
                explicitMeleeGuardians.Add(e);
            }

            foreach (SniperOnlyEnemy e in _phaseThreeRoot.GetComponentsInChildren<SniperOnlyEnemy>(true))
            {
                if (e == null) continue;
                if (mainBossTransform != null && e.transform == mainBossTransform) continue;
                if (shootBossLayer >= 0 && e.gameObject.layer != shootBossLayer) continue;
                explicitSniperGuardians.Add(e);
            }

            if (explicitMeleeGuardians.Count > 0 || explicitSniperGuardians.Count > 0)
            {
                Debug.Log($"[BossFight] Fase 3 explícita: {explicitMeleeGuardians.Count} melee + " +
                          $"{explicitSniperGuardians.Count} sniper guardias.");

                while (true)
                {
                    int alive = 0;
                    foreach (MeleeOnlyEnemy e in explicitMeleeGuardians) if (e != null) alive++;
                    foreach (SniperOnlyEnemy e in explicitSniperGuardians) if (e != null) alive++;
                    if (alive == 0) break;
                    yield return new WaitForSeconds(0.5f);
                }

                if (phase3Boss != null)
                {
                    phase3Boss.SetIdleAnimationName("Idle");
                    phase3Boss.SetPhase3IdleMode(false);
                    phase3Boss.SetInvulnerable(false);
                    Debug.Log("[BossFight] FinalBoss activado tras derrotar guardias.");
                }

                yield break;
            }

            var meleGuardians   = new System.Collections.Generic.List<MeleeOnlyEnemy>();
            var sniperGuardians = new System.Collections.Generic.List<SniperOnlyEnemy>();
            var rangedGuardians = new System.Collections.Generic.List<RangedMeleeEnemy>();

            foreach (MeleeOnlyEnemy e in _phaseThreeRoot.GetComponentsInChildren<MeleeOnlyEnemy>(true))
                meleGuardians.Add(e);
            foreach (SniperOnlyEnemy e in _phaseThreeRoot.GetComponentsInChildren<SniperOnlyEnemy>(true))
                sniperGuardians.Add(e);
            foreach (RangedMeleeEnemy e in _phaseThreeRoot.GetComponentsInChildren<RangedMeleeEnemy>(true))
            {
                
                if (mainBossTransform != null && e.transform == mainBossTransform) continue;
                if (!e.enabled) continue;
                rangedGuardians.Add(e);
            }

            int total = meleGuardians.Count + sniperGuardians.Count + rangedGuardians.Count;
            Debug.Log($"[BossFight] Fase 3: {rangedGuardians.Count} ranged + " +
                      $"{meleGuardians.Count} melee + {sniperGuardians.Count} sniper guardias. " +
                      $"Boss: {(mainBossTransform != null ? mainBossTransform.name : "N/A")}");

            if (total > 0)
            {
                
                while (true)
                {
                    int alive = 0;
                    foreach (MeleeOnlyEnemy  e in meleGuardians)   if (e != null) alive++;
                    foreach (SniperOnlyEnemy e in sniperGuardians) if (e != null) alive++;
                    foreach (RangedMeleeEnemy e in rangedGuardians) if (e != null) alive++;
                    if (alive == 0) break;
                    yield return new WaitForSeconds(0.5f);
                }

                Debug.Log("[BossFight] Guardias de fase 3 eliminados — boss ahora vulnerable y activo.");
            }
            else
            {
                Debug.LogWarning("[BossFight] Fase 3 no tiene guardias — boss activo desde el inicio.");
            }

            if (phase3Boss != null)
            {
                phase3Boss.SetIdleAnimationName("Idle"); 
                phase3Boss.SetPhase3IdleMode(false);     
                phase3Boss.SetInvulnerable(false);       
                Debug.Log("[BossFight] Boss fase 3 activado — ¡ahora puede ser dañado!");
            }
        }

        private void RefillPlayerHealthForNextPhase()
        {
            if (_playerHealth == null || _playerHealth.IsDead)
            {
                return;
            }

            _playerHealth.RestoreHealthSnapshot(_playerHealth.MaxHealth);
            _uiController?.SetLives(_playerHealth.CurrentHealth);
        }

        private void UpdateEnemyBar()
        {
            if (_enemyBar == null) return;
            _enemyBar.gameObject.SetActive(_totalPhaseHealth > 0 && !_fightCompleted);
            _enemyBar.fillAmount = _totalPhaseHealth <= 0
                ? 0f
                : Mathf.Clamp01((float)_remainingPhaseHealth / _totalPhaseHealth);
        }

        private Image FindEnemyBar()
        {
            GameObject go = FindSceneObject("EnemyBar");
            return go != null ? go.GetComponent<Image>() : null;
        }

        private Transform FindSceneTransform(string objectName)
        {
            GameObject go = FindSceneObject(objectName);
            return go != null ? go.transform : null;
        }

        private GameObject FindSceneObject(string objectName)
        {
            GameObject active = GameObject.Find(objectName);
            if (active != null) return active;

            GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null &&
                    all[i].scene.IsValid() &&
                    all[i].scene == gameObject.scene &&
                    all[i].name == objectName)
                {
                    return all[i];
                }
            }
            return null;
        }
    }
}
