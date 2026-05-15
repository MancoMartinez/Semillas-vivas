using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SemillasVivas.Systems;

namespace SemillasVivas.Gameplay.Demo.UI
{
    public sealed class DemoGameplayUiController : MonoBehaviour
    {
        private static readonly string[] HealthPanelNames = { "HealthPanel", "HeealthPanel" };
        
        private static readonly string[] GameOverPanelNames = { "GameOver", "GameOverPanel" };
        private static readonly string[] WinPanelNames = { "WinPanel" };
        private static readonly string[] MessagePanelNames = { "PanelMessage", "MessagePanel" };
        private static readonly string[] MessageTextNames = { "MessageRecolectedSeed", "MessageCollectedSeed", "Text (TMP)" };
        private static readonly string[] HudRootNames = { "SeedPanel", "HealthPanel", "HeealthPanel", "Attack", "Jump", "Settings", "Setting" };

        private TMP_Text _seedCounterText;
        private TMP_Text _messageText;
        private GameObject _messagePanel;
        private GameObject _gameOverPanel;
        private GameObject _winPanel;
        private Image _winSeedImage;
        private Image _winCocaImage;
        private TMP_Text _winSeedNameText;
        private TMP_Text _winDescriptionText;
        private GameObject[] _hudRoots;
        private Image[] _lifeImages;
        private Coroutine _messageRoutine;
        private Coroutine _messageLockRoutine;
        private DemoPlayerController _playerController;
        private DemoPlayerCombatController _combatController;

        public void Initialize(TMP_Text seedCounterText)
        {
            _seedCounterText = seedCounterText;
            _messagePanel = FindChildGameObject(transform, MessagePanelNames);
            _gameOverPanel = FindChildGameObject(transform, GameOverPanelNames);
            _winPanel = FindChildGameObject(transform, WinPanelNames);
            _messageText = FindOptionalText(_messagePanel != null ? _messagePanel.transform : null, MessageTextNames);
            _winSeedImage = FindNamedImage(_winPanel != null ? _winPanel.transform : null, "Semilla", "Seed");
            _winCocaImage = FindNamedImage(_winPanel != null ? _winPanel.transform : null, "Coca");
            _winSeedNameText = FindNamedText(_winPanel != null ? _winPanel.transform : null, "TxtSemilla");
            _winDescriptionText = FindNamedText(_winPanel != null ? _winPanel.transform : null, "Guia", "Text (TMP)");
            _lifeImages = ResolveLifeImages();
            _hudRoots = ResolveHudRoots();

            SetPanelState(_messagePanel, false);
            SetPanelState(_gameOverPanel, false);
            SetPanelState(_winPanel, false);
            SetGameplayHudVisible(true);
            SetSeedCount(0);
        }

        public void SetGameplayControllers(DemoPlayerController playerController, DemoPlayerCombatController combatController)
        {
            _playerController = playerController;
            _combatController = combatController;
        }

        public void SetSeedCount(int seedCount)
        {
            if (_seedCounterText == null)
            {
                return;
            }

            _seedCounterText.text = seedCount.ToString();
        }

        public void SetLives(int currentLives)
        {
            if (_lifeImages == null)
            {
                return;
            }

            for (int index = 0; index < _lifeImages.Length; index++)
            {
                if (_lifeImages[index] != null)
                {
                    _lifeImages[index].enabled = index < currentLives;
                }
            }
        }

        public void ShowSeedMessage(string message, float duration, bool blockGameplay)
        {
            if (_messagePanel == null || _messageText == null)
            {
                return;
            }

            if (_messageRoutine != null)
            {
                StopCoroutine(_messageRoutine);
            }

            if (_messageLockRoutine != null)
            {
                StopCoroutine(_messageLockRoutine);
            }

            _messageRoutine = StartCoroutine(ShowSeedMessageRoutine(message, duration));

            if (blockGameplay)
            {
                _messageLockRoutine = StartCoroutine(LockGameplayForMessageRoutine(duration));
            }
        }

        public void ShowGameOver()
        {
            ReleaseMessageLock();
            SetGameplayHudVisible(false);
            SetPanelState(_gameOverPanel, true);
        }

        public void ShowWin(LevelSeedData levelSeedData, Sprite seedSprite)
        {
            ReleaseMessageLock();
            SetGameplayHudVisible(false);
            ConfigureWinPanel(levelSeedData, seedSprite);
            SetPanelState(_winPanel, true);
        }

        public void SetGameplayHudVisible(bool visible)
        {
            if (_hudRoots == null)
            {
                return;
            }

            for (int index = 0; index < _hudRoots.Length; index++)
            {
                GameObject hudRoot = _hudRoots[index];

                if (hudRoot != null)
                {
                    hudRoot.SetActive(visible);
                }
            }

            if (!visible)
            {
                SetPanelState(_messagePanel, false);
            }
        }

        private IEnumerator ShowSeedMessageRoutine(string message, float duration)
        {
            _messageText.text = message;
            SetPanelState(_messagePanel, true);
            yield return new WaitForSecondsRealtime(duration);
            SetPanelState(_messagePanel, false);
            _messageRoutine = null;
        }

        private IEnumerator LockGameplayForMessageRoutine(float duration)
        {
            _playerController?.SetInputLocked(true);
            _combatController?.SetInputLocked(true);
            MobileInputState.Reset();
            Time.timeScale = 0f;

            yield return new WaitForSecondsRealtime(duration);

            Time.timeScale = 1f;
            _playerController?.SetInputLocked(false);
            _combatController?.SetInputLocked(false);
            _messageLockRoutine = null;
        }

        private void ReleaseMessageLock()
        {
            if (_messageLockRoutine != null)
            {
                StopCoroutine(_messageLockRoutine);
                _messageLockRoutine = null;
            }

            Time.timeScale = 1f;
            _playerController?.SetInputLocked(false);
            _combatController?.SetInputLocked(false);
        }

        private Image[] ResolveLifeImages()
        {
            GameObject healthPanel = FindChildGameObject(transform, HealthPanelNames);

            if (healthPanel == null)
            {
                return null;
            }

            return new[]
            {
                FindChildImage(healthPanel.transform, "Life1"),
                FindChildImage(healthPanel.transform, "Life2"),
                FindChildImage(healthPanel.transform, "Life3"),
            };
        }

        private GameObject[] ResolveHudRoots()
        {
            GameObject[] hudRoots = new GameObject[HudRootNames.Length];

            for (int index = 0; index < HudRootNames.Length; index++)
            {
                hudRoots[index] = FindChildGameObject(transform, new[] { HudRootNames[index] });
            }

            return hudRoots;
        }

        private void ConfigureWinPanel(LevelSeedData levelSeedData, Sprite seedSprite)
        {
            if (_winCocaImage != null)
            {
                _winCocaImage.sprite = seedSprite;
                _winCocaImage.preserveAspect = true;
            }

            if (_winSeedImage != null)
            {
                _winSeedImage.sprite = seedSprite;
                _winSeedImage.preserveAspect = true;
            }

            if (_winSeedNameText != null)
            {
                _winSeedNameText.text = levelSeedData.SeedName;
            }

            if (_winDescriptionText != null)
            {
                _winDescriptionText.text = levelSeedData.WinDescription;
            }
        }

        private static void SetPanelState(GameObject panel, bool state)
        {
            if (panel != null)
            {
                panel.SetActive(state);
            }
        }

        private static Image FindChildImage(Transform root, string name)
        {
            Transform child = FindChildRecursive(root, name);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private static Image FindNamedImage(Transform root, params string[] preferredNames)
        {
            if (root == null)
            {
                return null;
            }

            for (int index = 0; index < preferredNames.Length; index++)
            {
                Transform child = FindChildRecursive(root, preferredNames[index]);

                if (child == null)
                {
                    continue;
                }

                Image image = child.GetComponent<Image>();

                if (image != null)
                {
                    return image;
                }
            }

            return null;
        }

        private static TMP_Text FindNamedText(Transform root, params string[] preferredNames)
        {
            if (root == null)
            {
                return null;
            }

            for (int index = 0; index < preferredNames.Length; index++)
            {
                Transform child = FindChildRecursive(root, preferredNames[index]);

                if (child == null)
                {
                    continue;
                }

                TMP_Text directText = child.GetComponent<TMP_Text>();

                if (directText != null)
                {
                    return directText;
                }

                TMP_Text nestedText = child.GetComponentInChildren<TMP_Text>(true);

                if (nestedText != null)
                {
                    return nestedText;
                }
            }

            return null;
        }

        private static TMP_Text FindOptionalText(Transform root, string[] preferredNames)
        {
            if (root == null)
            {
                return null;
            }

            for (int index = 0; index < preferredNames.Length; index++)
            {
                Transform child = FindChildRecursive(root, preferredNames[index]);

                if (child != null)
                {
                    TMP_Text namedText = child.GetComponent<TMP_Text>();

                    if (namedText != null)
                    {
                        return namedText;
                    }
                }
            }

            return root.GetComponentInChildren<TMP_Text>(true);
        }

        private static GameObject FindChildGameObject(Transform root, string[] candidateNames)
        {
            for (int index = 0; index < candidateNames.Length; index++)
            {
                Transform child = FindChildRecursive(root, candidateNames[index]);

                if (child != null)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform root, string targetName)
        {
            if (root == null)
            {
                return null;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);

                if (child.name == targetName)
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, targetName);

                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
