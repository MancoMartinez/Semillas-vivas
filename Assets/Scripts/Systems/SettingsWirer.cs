using UnityEngine;
using UnityEngine.UI;
using SemillasVivas.Systems.Audio;

namespace SemillasVivas.Systems
{
    
    public static class SettingsWirer
    {
        
        public const string MusicVolumeKey   = "settings.musicVolume";
        public const string EffectsVolumeKey = "settings.effectsVolume";
        private const float DefaultVolume = 0.75f;

        public static void WireSliders(Transform root)
        {
            if (root == null)
            {
                return;
            }

            Slider[] sliders = root.GetComponentsInChildren<Slider>(includeInactive: true);

            if (sliders.Length == 0)
            {
                Debug.LogWarning($"[SettingsWirer] No sliders found under '{root.name}'.");
                return;
            }

            bool wiredMusic   = false;
            bool wiredEffects = false;

            for (int i = 0; i < sliders.Length; i++)
            {
                Slider slider = sliders[i];
                string context = BuildContext(slider.transform);

                if (!wiredMusic && IsMusic(context))
                {
                    ConfigureSlider(slider, MusicVolumeKey);
                    wiredMusic = true;
                    Debug.Log($"[SettingsWirer] Music slider wired: '{slider.name}' (path: {context})");
                }
                else if (!wiredEffects && IsEffects(context))
                {
                    ConfigureSlider(slider, EffectsVolumeKey);
                    wiredEffects = true;
                    Debug.Log($"[SettingsWirer] Effects slider wired: '{slider.name}' (path: {context})");
                }
            }

            if (!wiredMusic && !wiredEffects && sliders.Length >= 1)
            {
                ConfigureSlider(sliders[0], MusicVolumeKey);
                Debug.Log($"[SettingsWirer] Fallback: first slider → music: '{sliders[0].name}'");

                if (sliders.Length >= 2)
                {
                    ConfigureSlider(sliders[1], EffectsVolumeKey);
                    Debug.Log($"[SettingsWirer] Fallback: second slider → effects: '{sliders[1].name}'");
                }
            }
        }

        public static void ApplySavedSettings()
        {
            if (GameAudioService.Instance == null)
            {
                return;
            }

            GameAudioService.Instance.SetMusicVolume(
                PlayerPrefs.GetFloat(MusicVolumeKey, DefaultVolume));
            GameAudioService.Instance.SetEffectsVolume(
                PlayerPrefs.GetFloat(EffectsVolumeKey, DefaultVolume));
        }

        private static void ConfigureSlider(Slider slider, string key)
        {
            float saved = PlayerPrefs.GetFloat(key, DefaultVolume);
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.interactable = true;   
            slider.SetValueWithoutNotify(saved);

            slider.onValueChanged.RemoveAllListeners();

            string capturedKey = key;
            slider.onValueChanged.AddListener(value =>
            {
                PlayerPrefs.SetFloat(capturedKey, value);
                PlayerPrefs.Save();
                ApplyToService(capturedKey, value);
            });

            ApplyToService(key, saved);
        }

        private static void ApplyToService(string key, float value)
        {
            if (GameAudioService.Instance == null)
            {
                return;
            }

            if (key == MusicVolumeKey)
            {
                GameAudioService.Instance.SetMusicVolume(value);
            }
            else if (key == EffectsVolumeKey)
            {
                GameAudioService.Instance.SetEffectsVolume(value);
            }
        }

        private static string BuildContext(Transform t)
        {
            string name = t.name;
            if (t.parent != null)
            {
                name = t.parent.name + "/" + name;
                if (t.parent.parent != null)
                {
                    name = t.parent.parent.name + "/" + name;
                }
            }

            return name.ToLowerInvariant();
        }

        private static bool IsMusic(string ctx) =>
            ctx.Contains("music") ||
            ctx.Contains("musica") ||
            ctx.Contains("música") ||
            ctx.Contains("bgm")    ||
            ctx.Contains("song");

        private static bool IsEffects(string ctx) =>
            ctx.Contains("effect") ||
            ctx.Contains("efecto") ||
            ctx.Contains("sfx")    ||
            ctx.Contains("sound")  ||
            ctx.Contains("sonido") ||
            ctx.Contains("fx");
    }
}
