using System;
using System.Collections.Generic;
using SemillasVivas.Systems;
using SemillasVivas.Systems.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SemillasVivas.UI.MainMenu
{
    public sealed class SettingsScreenController
    {
        private const string MusicVolumeKey = "settings.musicVolume";
        private const string EffectsVolumeKey = "settings.effectsVolume";
        private const string TextSpeedKey = "settings.textSpeed";
        private const string FontSizeKey = "settings.fontSize";
        private const string ContrastKey = "settings.contrast";

        private readonly List<GameObject> _subPanels = new();
        private readonly List<GameObject> _mainMenuButtons = new();

        private Transform _settingsScreenRoot;
        private Transform _mainPanelRoot;
        private GameObject _audioSubPanel;
        private GameObject _gameSubPanel;
        private GameObject _accessibilitySubPanel;
        private Slider _musicSlider;
        private Slider _effectsSlider;
        private OptionGroup _textSpeedGroup;
        private OptionGroup _fontSizeGroup;
        private OptionGroup _contrastGroup;

        public void Initialize(Transform settingsScreenRoot)
        {
            if (settingsScreenRoot == null)
            {
                throw new ArgumentNullException(nameof(settingsScreenRoot));
            }

            _settingsScreenRoot = settingsScreenRoot;
            _mainPanelRoot = FindDirectChild(settingsScreenRoot, "Background");
            _audioSubPanel = FindSubPanel(_mainPanelRoot,
                "AudioSub", "Sonido", "Audio", "SonidoSub", "Música", "Musica");
            _gameSubPanel = FindSubPanel(_mainPanelRoot,
                "GameSub", "Juego", "Game", "JuegoSub", "Gameplay");
            _accessibilitySubPanel = FindSubPanel(_mainPanelRoot,
                "AccesibilitySub", "AccessibilitySub", "AccesibilidadSub",
                "Accesibilidad", "Accessibility");

            RegisterSubPanel(_audioSubPanel);
            RegisterSubPanel(_gameSubPanel);
            RegisterSubPanel(_accessibilitySubPanel);
            RegisterMainMenuButton("Audio");
            RegisterMainMenuButton("Game");
            RegisterMainMenuButton("Accesibilidad");
            RegisterMainMenuButton("Accesibility");
            RegisterMainMenuButton("Accessibility");

            BindMainButton("Audio", () => ShowSubPanel(_audioSubPanel));
            BindMainButton("Game", () => ShowSubPanel(_gameSubPanel));
            BindMainButton("Accesibilidad", () => ShowSubPanel(_accessibilitySubPanel));
            BindMainButton("Accesibility", () => ShowSubPanel(_accessibilitySubPanel));
            BindMainButton("Accessibility", () => ShowSubPanel(_accessibilitySubPanel));

            _musicSlider = FindOptionalSlider(_audioSubPanel, "Music/Slider", "Music", "Slider");
            _effectsSlider = FindOptionalSlider(_audioSubPanel, "Effects/Slider", "Effects", "Slider");

            _textSpeedGroup = new OptionGroup(
                TextSpeedKey,
                0,
                FindOptionalButton(_gameSubPanel, "Slowly"),
                FindOptionalButton(_gameSubPanel, "Normal"),
                FindOptionalButton(_gameSubPanel, "Faster"));

            _fontSizeGroup = new OptionGroup(
                FontSizeKey,
                1,
                FindOptionalButton(_accessibilitySubPanel, "Small"),
                FindOptionalButton(_accessibilitySubPanel, "Normal"),
                FindOptionalButton(_accessibilitySubPanel, "Big", "BIg"));

            _contrastGroup = new OptionGroup(
                ContrastKey,
                1,
                FindOptionalButton(_accessibilitySubPanel, "Small"),
                FindOptionalButton(_accessibilitySubPanel, "Normal"),
                FindOptionalButton(_accessibilitySubPanel, "Big", "BIg"));

            ConfigureSlider(_musicSlider, MusicVolumeKey, 0.75f);
            ConfigureSlider(_effectsSlider, EffectsVolumeKey, 0.75f);
            _textSpeedGroup.Initialize();
            _fontSizeGroup.Initialize();
            _contrastGroup.Initialize();

            SettingsWirer.WireSliders(_settingsScreenRoot);

            ShowMainPanel();
        }

        public void ShowMainPanel()
        {
            if (_mainPanelRoot != null)
            {
                _mainPanelRoot.gameObject.SetActive(true);
            }

            for (int index = 0; index < _mainMenuButtons.Count; index++)
            {
                if (_mainMenuButtons[index] != null)
                {
                    _mainMenuButtons[index].SetActive(true);
                }
            }

            for (int index = 0; index < _subPanels.Count; index++)
            {
                if (_subPanels[index] != null)
                {
                    _subPanels[index].SetActive(false);
                }
            }
        }

        public bool HandleBackRequested()
        {
            if (IsAnySubPanelOpen())
            {
                ShowMainPanel();
                return true;
            }

            return false;
        }

        private void ShowSubPanel(GameObject panel)
        {
            if (_mainPanelRoot != null)
            {
                _mainPanelRoot.gameObject.SetActive(true);
            }

            for (int index = 0; index < _mainMenuButtons.Count; index++)
            {
                if (_mainMenuButtons[index] != null)
                {
                    _mainMenuButtons[index].SetActive(false);
                }
            }

            for (int index = 0; index < _subPanels.Count; index++)
            {
                if (_subPanels[index] != null)
                {
                    _subPanels[index].SetActive(false);
                }
            }

            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        private bool IsAnySubPanelOpen()
        {
            for (int index = 0; index < _subPanels.Count; index++)
            {
                GameObject panel = _subPanels[index];

                if (panel != null && panel.activeSelf)
                {
                    return true;
                }
            }

            return false;
        }

        private void RegisterSubPanel(GameObject panel)
        {
            if (panel != null && !_subPanels.Contains(panel))
            {
                _subPanels.Add(panel);
            }
        }

        private void RegisterMainMenuButton(string childName)
        {
            if (_mainPanelRoot == null)
            {
                return;
            }

            Transform child = FindDirectChild(_mainPanelRoot, childName);

            if (child == null)
            {
                return;
            }

            if (child.GetComponent<Button>() == null)
            {
                return;
            }

            if (!child.gameObject.activeSelf)
            {
                return;
            }

            if (!_mainMenuButtons.Contains(child.gameObject))
            {
                _mainMenuButtons.Add(child.gameObject);
            }
        }

        private void BindMainButton(string buttonName, UnityEngine.Events.UnityAction action)
        {
            if (_mainPanelRoot == null)
            {
                return;
            }

            Button button = FindDirectChildButton(_mainPanelRoot, buttonName);

            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                GameAudioService.Instance?.PlayUiClick();
                action?.Invoke();
            });
        }

        private static Slider FindOptionalSlider(GameObject panel, params string[] nameHints)
        {
            if (panel == null)
            {
                return null;
            }

            Slider[] sliders = panel.GetComponentsInChildren<Slider>(true);

            for (int sliderIndex = 0; sliderIndex < sliders.Length; sliderIndex++)
            {
                Slider slider = sliders[sliderIndex];
                string hierarchyPath = BuildHierarchyPath(slider.transform);

                for (int hintIndex = 0; hintIndex < nameHints.Length; hintIndex++)
                {
                    if (hierarchyPath.IndexOf(nameHints[hintIndex], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return slider;
                    }
                }
            }

            return sliders.Length > 0 ? sliders[0] : null;
        }

        private static Button FindOptionalButton(GameObject rootObject, params string[] candidateNames)
        {
            if (rootObject == null)
            {
                return null;
            }

            Button[] buttons = rootObject.GetComponentsInChildren<Button>(true);

            for (int nameIndex = 0; nameIndex < candidateNames.Length; nameIndex++)
            {
                for (int buttonIndex = 0; buttonIndex < buttons.Length; buttonIndex++)
                {
                    if (buttons[buttonIndex].gameObject.name.Equals(candidateNames[nameIndex], StringComparison.OrdinalIgnoreCase))
                    {
                        return buttons[buttonIndex];
                    }
                }
            }

            return null;
        }

        private static Button FindDirectChildButton(Transform root, params string[] candidateNames)
        {
            if (root == null)
            {
                return null;
            }

            for (int nameIndex = 0; nameIndex < candidateNames.Length; nameIndex++)
            {
                for (int childIndex = 0; childIndex < root.childCount; childIndex++)
                {
                    Transform child = root.GetChild(childIndex);

                    if (!child.name.Equals(candidateNames[nameIndex], StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    Button button = child.GetComponent<Button>();

                    if (button != null)
                    {
                        return button;
                    }
                }
            }

            return null;
        }

        private static Transform FindDirectChild(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);

                if (child.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }

            return null;
        }

        private static GameObject FindDirectChildGameObject(Transform root, string childName)
        {
            return FindDirectChild(root, childName)?.gameObject;
        }

        private static GameObject FindSubPanel(Transform root, params string[] candidateNames)
        {
            if (root == null)
            {
                return null;
            }

            for (int nameIndex = 0; nameIndex < candidateNames.Length; nameIndex++)
            {
                GameObject subPanel = FindDirectChildGameObject(root, candidateNames[nameIndex]);

                if (subPanel != null)
                {
                    return subPanel;
                }
            }

            return null;
        }

        private static string BuildHierarchyPath(Transform target)
        {
            if (target == null)
            {
                return string.Empty;
            }

            string path = target.name;
            Transform current = target.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        private static void ConfigureSlider(Slider slider, string key, float defaultValue)
        {
            if (slider == null)
            {
                return;
            }

            float value = PlayerPrefs.GetFloat(key, defaultValue);
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(newValue =>
            {
                PlayerPrefs.SetFloat(key, newValue);
                PlayerPrefs.Save();
                ApplyAudioSetting(key, newValue);
            });

            ApplyAudioSetting(key, value);
        }

        private static void ApplyAudioSetting(string key, float value)
        {
            if (GameAudioService.Instance == null)
            {
                return;
            }

            if (key == MusicVolumeKey)
            {
                GameAudioService.Instance.SetMusicVolume(value);
                return;
            }

            if (key == EffectsVolumeKey)
            {
                GameAudioService.Instance.SetEffectsVolume(value);
            }
        }

        private sealed class OptionGroup
        {
            private readonly string _playerPrefsKey;
            private readonly List<Button> _buttons = new();
            private readonly int _defaultIndex;

            public OptionGroup(string playerPrefsKey, int defaultIndex, params Button[] buttons)
            {
                _playerPrefsKey = playerPrefsKey;
                _defaultIndex = defaultIndex;

                for (int index = 0; index < buttons.Length; index++)
                {
                    if (buttons[index] != null)
                    {
                        _buttons.Add(buttons[index]);
                    }
                }
            }

            public void Initialize()
            {
                if (_buttons.Count == 0)
                {
                    return;
                }

                for (int index = 0; index < _buttons.Count; index++)
                {
                    int capturedIndex = index;
                    _buttons[index].onClick.RemoveAllListeners();
                    _buttons[index].onClick.AddListener(() =>
                    {
                        GameAudioService.Instance?.PlayUiClick();
                        Select(capturedIndex);
                    });
                }

                Select(PlayerPrefs.GetInt(_playerPrefsKey, _defaultIndex), false);
            }

            private void Select(int selectedIndex, bool persist = true)
            {
                for (int index = 0; index < _buttons.Count; index++)
                {
                    bool isSelected = index == selectedIndex;
                    ApplyState(_buttons[index], isSelected);
                }

                if (!persist)
                {
                    return;
                }

                PlayerPrefs.SetInt(_playerPrefsKey, selectedIndex);
                PlayerPrefs.Save();
            }

            private static void ApplyState(Button button, bool isSelected)
            {
                Graphic graphic = button.targetGraphic != null ? button.targetGraphic : button.GetComponent<Graphic>();
                TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);

                if (graphic != null)
                {
                    graphic.color = isSelected
                        ? new Color(0.92f, 0.92f, 0.92f, 1f)
                        : new Color(1f, 1f, 1f, 1f);
                }

                if (text != null)
                {
                    text.color = isSelected
                        ? new Color(0.1f, 0.1f, 0.1f, 1f)
                        : new Color(0f, 0f, 0f, 1f);
                }
            }
        }
    }
}
