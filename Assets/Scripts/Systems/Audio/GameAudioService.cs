using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace SemillasVivas.Systems.Audio
{
    public sealed class GameAudioService : MonoBehaviour
    {
        private const string ServiceObjectName = "GameAudioService";
        private const string CatalogResourcePath = "GameAudioCatalog";
        private const string MusicVolumeKey = "settings.musicVolume";
        private const string EffectsVolumeKey = "settings.effectsVolume";

        private readonly Dictionary<GameAudioCue, AudioClip> _clips = new();
        private readonly Dictionary<string, AudioClip> _musicOverrides = new(StringComparer.OrdinalIgnoreCase);
        
        private readonly List<AudioSource> _worldSources = new();
        private GameAudioCatalog _catalog;
        private AudioSource _uiSource;
        private AudioSource _sfxSource;
        private AudioSource _musicSource;
        private AudioClip[] _allAudioClips = Array.Empty<AudioClip>();
        private float _currentEffectsVolume = 0.75f;

        public static GameAudioService Instance { get; private set; }

        public static GameAudioService EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            GameAudioService existing = FindFirstObjectByType<GameAudioService>();

            if (existing != null)
            {
                existing.InitializeIfNeeded();
                return existing;
            }

            GameObject serviceObject = new(ServiceObjectName);
            return serviceObject.AddComponent<GameAudioService>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeIfNeeded();
        }

        public AudioSource CreateWorldSource(Transform owner, bool loop = false)
        {
            GameObject child = new(loop ? "LoopAudioSource" : "OneShotAudioSource");
            child.transform.SetParent(owner, false);

            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.volume = _currentEffectsVolume;
            source.outputAudioMixerGroup = _catalog != null ? _catalog.MasterGroup : null;

            _worldSources.Add(source);
            return source;
        }

        public void PlayUiClick()
        {
            PlayOneShot(_uiSource, GameAudioCue.UiClick, 0.85f);
        }

        public void PlaySfx(GameAudioCue cue, float volume = 1f)
        {
            PlayOneShot(_sfxSource, cue, volume);
        }

        public bool TryGetClip(GameAudioCue cue, out AudioClip clip)
        {
            return _clips.TryGetValue(cue, out clip);
        }

        public void RegisterSceneMusic(string sceneName, AudioClip clip)
        {
            if (string.IsNullOrEmpty(sceneName) || clip == null)
            {
                return;
            }

            _musicOverrides[sceneName] = clip;

            if (string.Equals(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                              sceneName, StringComparison.OrdinalIgnoreCase))
            {
                PlayMusicForScene(sceneName);
            }
        }

        public void SetMusicVolume(float value)
        {
            if (_musicSource == null)
            {
                return;
            }

            _musicSource.volume = Mathf.Clamp01(value);

            if (_musicSource.volume > 0f &&
                !_musicSource.isPlaying &&
                _musicSource.clip != null)
            {
                _musicSource.Play();
            }

            PlayerPrefs.SetFloat(MusicVolumeKey, _musicSource.volume);
            PlayerPrefs.Save();
        }

        public void SetEffectsVolume(float value)
        {
            _currentEffectsVolume = Mathf.Clamp01(value);

            if (_uiSource != null)  _uiSource.volume  = _currentEffectsVolume;
            if (_sfxSource != null) _sfxSource.volume = _currentEffectsVolume;

            for (int i = _worldSources.Count - 1; i >= 0; i--)
            {
                if (_worldSources[i] == null)
                {
                    _worldSources.RemoveAt(i);
                    continue;
                }
                _worldSources[i].volume = _currentEffectsVolume;
            }

            PlayerPrefs.SetFloat(EffectsVolumeKey, _currentEffectsVolume);
            PlayerPrefs.Save();
        }

        private void InitializeIfNeeded()
        {
            if (_uiSource != null)
            {
                return;
            }

            _catalog = Resources.Load<GameAudioCatalog>(CatalogResourcePath);
            _uiSource = CreateBusSource("UI_Source");
            _sfxSource = CreateBusSource("SFX_Source");
            _musicSource = CreateBusSource("Music_Source");
            _musicSource.loop = true;

            CacheClips();
            ApplySavedVolumes();
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            PlayMusicForScene(SceneManager.GetActiveScene().name);
        }

        private AudioSource CreateBusSource(string sourceName)
        {
            GameObject child = new(sourceName);
            child.transform.SetParent(transform, false);

            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = _catalog != null ? _catalog.MasterGroup : null;
            return source;
        }

        private void CacheClips()
        {
            if (_clips.Count > 0)
            {
                return;
            }

            if (_catalog != null)
            {
                Array cues = Enum.GetValues(typeof(GameAudioCue));

                for (int index = 0; index < cues.Length; index++)
                {
                    GameAudioCue cue = (GameAudioCue)cues.GetValue(index);

                    if (_catalog.TryGetCue(cue, out AudioClip clip) && clip != null)
                    {
                        _clips[cue] = clip;
                    }
                }

                if (_clips.Count > 0)
                {
                    return;
                }
            }

            AudioClip[] allClips = Resources.FindObjectsOfTypeAll<AudioClip>();
            _allAudioClips = allClips;

            AssignCue(allClips, GameAudioCue.UiClick, "item collect");
            AssignCue(allClips, GameAudioCue.PlayerAttack, "deadly-strike");
            AssignCue(allClips, GameAudioCue.PlayerJump, "jump");
            AssignCue(allClips, GameAudioCue.PlayerHurt, "ouch");
            AssignCue(allClips, GameAudioCue.PlayerDeath, "game over");
            AssignCue(allClips, GameAudioCue.PlayerFootstep, "running-on-concrete", "running-wood", "running");
            AssignCue(allClips, GameAudioCue.EnemyFootstep, "footsteps-on-gravel");
            AssignCue(allClips, GameAudioCue.EnemyHurt, "enemy hurt-pain");
            AssignCue(allClips, GameAudioCue.EnemyDeath, "enemy death");
            AssignCue(allClips, GameAudioCue.PowerUp, "power-up");
            AssignCue(allClips, GameAudioCue.ItemCollect, "item collect");
            AssignCue(allClips, GameAudioCue.Heal, "heal");
        }

        private void AssignCue(IReadOnlyList<AudioClip> allClips, GameAudioCue cue, params string[] searchTerms)
        {
            if (_clips.ContainsKey(cue))
            {
                return;
            }

            AudioClip clip = FindClip(allClips, searchTerms);

            if (clip != null)
            {
                _clips[cue] = clip;
            }
        }

        private static AudioClip FindClip(IReadOnlyList<AudioClip> allClips, IReadOnlyList<string> searchTerms)
        {
            for (int clipIndex = 0; clipIndex < allClips.Count; clipIndex++)
            {
                AudioClip clip = allClips[clipIndex];

                if (clip == null)
                {
                    continue;
                }

                string clipName = clip.name.ToLowerInvariant();

                for (int termIndex = 0; termIndex < searchTerms.Count; termIndex++)
                {
                    if (clipName.Contains(searchTerms[termIndex]))
                    {
                        return clip;
                    }
                }
            }

            return null;
        }

        private void PlayOneShot(AudioSource source, GameAudioCue cue, float volume)
        {
            if (source == null)
            {
                return;
            }

            if (!_clips.TryGetValue(cue, out AudioClip clip) || clip == null)
            {
                Debug.LogWarning($"Audio cue '{cue}' is not mapped to any clip.");
                return;
            }

            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private void ApplySavedVolumes()
        {
            
            _currentEffectsVolume = PlayerPrefs.GetFloat(EffectsVolumeKey, 0.75f);
            SetMusicVolume(PlayerPrefs.GetFloat(MusicVolumeKey, 0.75f));
            SetEffectsVolume(_currentEffectsVolume);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PlayMusicForScene(scene.name);
        }

        private void PlayMusicForScene(string sceneName)
        {
            if (_musicSource == null)
            {
                return;
            }

            AudioClip musicClip = null;

            if (_musicOverrides.TryGetValue(sceneName, out AudioClip overrideClip) && overrideClip != null)
            {
                musicClip = overrideClip;
            }

            if (musicClip == null && _catalog != null)
            {
                _catalog.TryGetMusicForScene(sceneName, out musicClip);
            }

            musicClip ??= FindFallbackMusic(sceneName);

            if (musicClip == null)
            {
                return;
            }

            if (_musicSource.clip == musicClip && _musicSource.isPlaying)
            {
                return;
            }

            _musicSource.clip = musicClip;
            _musicSource.Play();
        }

        private AudioClip FindFallbackMusic(string sceneName)
        {
            if (_allAudioClips == null || _allAudioClips.Length == 0)
            {
                _allAudioClips = Resources.FindObjectsOfTypeAll<AudioClip>();
            }

            string token = sceneName.ToLowerInvariant() switch
            {
                "mainmenu" => "home",
                "demogameplay" => "1 ",
                "lvl1" => "1 ",
                "level_01" => "1 ",
                "lvl2" => "2 ",
                "lvl3" => "3 ",
                "lvl4" => "4 ",
                "lvl5" => "5 ",
                "boss" => "5 ",
                _ => string.Empty,
            };

            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            for (int index = 0; index < _allAudioClips.Length; index++)
            {
                AudioClip clip = _allAudioClips[index];

                if (clip == null)
                {
                    continue;
                }

                string clipName = clip.name.ToLowerInvariant();

                if (token == "home")
                {
                    if (clipName.Contains("home"))
                    {
                        return clip;
                    }

                    continue;
                }

                if (clipName.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                {
                    return clip;
                }
            }

            return null;
        }
    }
}
