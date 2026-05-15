using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using SemillasVivas.Gameplay.Demo.UI;
using SemillasVivas.Systems;

namespace SemillasVivas.Gameplay.Demo
{
    public sealed class DemoLevelFlowController : MonoBehaviour
    {
        [SerializeField] private float restartDelay = 2f;
        [SerializeField] private float winDelay = 5f;
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string bossSceneName = "Boss";

        private DemoPlayerHealth _playerHealth;
        private Transform _playerTransform;
        private DemoGameplayUiController _uiController;
        private float _fallDeathY;
        private bool _isTransitioning;
        private LevelSeedData _levelSeedData;
        private Sprite _levelSeedSprite;

        public void Initialize(
            DemoPlayerHealth playerHealth,
            Transform playerTransform,
            DemoGameplayUiController uiController,
            float fallDeathY)
        {
            _playerHealth = playerHealth;
            _playerTransform = playerTransform;
            _uiController = uiController;
            _fallDeathY = fallDeathY;
            _isTransitioning = false;
            _levelSeedData = LevelSeedCatalog.GetForScene(SceneManager.GetActiveScene().name);
            _levelSeedSprite = LevelSeedCatalog.LoadSeedSprite(_levelSeedData);

            if (_playerHealth != null)
            {
                _playerHealth.Died -= HandlePlayerDied;
                _playerHealth.Died += HandlePlayerDied;
            }
        }

        private void Update()
        {
            if (_isTransitioning || _playerHealth == null || _playerTransform == null || _playerHealth.IsDead)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
            {
                CompleteLevel();
                return;
            }

            if (_playerTransform.position.y <= _fallDeathY)
            {
                _playerHealth.Kill();
            }
        }

        public void CompleteLevel()
        {
            if (_isTransitioning)
            {
                return;
            }

            _isTransitioning = true;

            string currentSceneName = SceneManager.GetActiveScene().name;

            if (!string.Equals(currentSceneName, bossSceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                
                LevelProgressService.MarkLevelCompleted(currentSceneName);
            }

            PlayerPrefs.SetInt("NavigateToLevelSelect", 1);
            PlayerPrefs.Save();

            _uiController?.ShowWin(_levelSeedData, _levelSeedSprite);
            StartCoroutine(ReturnToMenuRoutine());
        }

        private void HandlePlayerDied()
        {
            if (_isTransitioning)
            {
                return;
            }

            _isTransitioning = true;
            _uiController?.ShowGameOver();
            StartCoroutine(RestartLevelRoutine());
        }

        private IEnumerator RestartLevelRoutine()
        {
            
            Time.timeScale = 1f;
            yield return new WaitForSeconds(restartDelay);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private IEnumerator ReturnToMenuRoutine()
        {
            Time.timeScale = 1f;
            yield return new WaitForSeconds(winDelay);

            string currentSceneName = SceneManager.GetActiveScene().name;

            if (string.Equals(currentSceneName, "lvl5", System.StringComparison.OrdinalIgnoreCase))
            {
                SceneManager.LoadScene(bossSceneName);
                yield break;
            }

            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void OnDestroy()
        {
            if (_playerHealth != null)
            {
                _playerHealth.Died -= HandlePlayerDied;
            }
        }
    }
}
