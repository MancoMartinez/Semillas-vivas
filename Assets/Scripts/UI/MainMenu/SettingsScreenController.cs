using System;
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

            _musicSlider = UIPathUtility.FindRequired(settingsScreenRoot, "Background/Audio/Music/Slider").GetComponent<Slider>();
            _effectsSlider = UIPathUtility.FindRequired(settingsScreenRoot, "Background/Audio/Effects/Slider").GetComponent<Slider>();

            _textSpeedGroup = new OptionGroup(
                TextSpeedKey,
                0,
                UIPathUtility.EnsureButton(settingsScreenRoot, "Background/Game/ButtonOptions/Slowly"),
                UIPathUtility.EnsureButton(settingsScreenRoot, "Background/Game/ButtonOptions/Normal"),
                UIPathUtility.EnsureButton(settingsScreenRoot, "Background/Game/ButtonOptions/Faster"));

            _fontSizeGroup = new OptionGroup(
                FontSizeKey,
                1,
                UIPathUtility.EnsureButton(settingsScreenRoot, "Background/Accesibility/Subtitle/ButtonOptions/Small"),
                UIPathUtility.EnsureButton(settingsScreenRoot, "Background/Accesibility/Subtitle/ButtonOptions/Normal"),
                UIPathUtility.EnsureButton(settingsScreenRoot, "Background/Accesibility/Subtitle/ButtonOptions/BIg"));

            _contrastGroup = new OptionGroup(
                ContrastKey,
                1,
                UIPathUtility.EnsureButton(settingsScreenRoot, "Background/Accesibility/ButtonOptions/Small"),
                UIPathUtility.EnsureButton(settingsScreenRoot, "Background/Accesibility/ButtonOptions/Normal"),
                UIPathUtility.EnsureButton(settingsScreenRoot, "Background/Accesibility/ButtonOptions/BIg"));

            ConfigureSlider(_musicSlider, MusicVolumeKey, 0.75f);
            ConfigureSlider(_effectsSlider, EffectsVolumeKey, 0.75f);

            _textSpeedGroup.Initialize();
            _fontSizeGroup.Initialize();
            _contrastGroup.Initialize();
        }

        private static void ConfigureSlider(Slider slider, string key, float defaultValue)
        {
            if (slider == null)
            {
                Debug.LogWarning($"Slider for key '{key}' is missing.");
                return;
            }

            float value = PlayerPrefs.GetFloat(key, defaultValue);
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(newValue =>
            {
                PlayerPrefs.SetFloat(key, newValue);
                PlayerPrefs.Save();
            });
        }

        private sealed class OptionGroup
        {
            private readonly string _playerPrefsKey;
            private readonly Button[] _buttons;
            private readonly int _defaultIndex;

            public OptionGroup(string playerPrefsKey, int defaultIndex, params Button[] buttons)
            {
                _playerPrefsKey = playerPrefsKey;
                _defaultIndex = defaultIndex;
                _buttons = buttons;
            }

            public void Initialize()
            {
                for (int i = 0; i < _buttons.Length; i++)
                {
                    int capturedIndex = i;
                    _buttons[i].onClick.RemoveAllListeners();
                    _buttons[i].onClick.AddListener(() => Select(capturedIndex));
                }

                Select(PlayerPrefs.GetInt(_playerPrefsKey, _defaultIndex), false);
            }

            private void Select(int selectedIndex, bool persist = true)
            {
                for (int i = 0; i < _buttons.Length; i++)
                {
                    bool isSelected = i == selectedIndex;
                    ApplyState(_buttons[i], isSelected);
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
