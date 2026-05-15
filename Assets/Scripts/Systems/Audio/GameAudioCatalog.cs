using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace SemillasVivas.Systems.Audio
{
    [CreateAssetMenu(fileName = "GameAudioCatalog", menuName = "Semillas Vivas/Audio Catalog")]
    public sealed class GameAudioCatalog : ScriptableObject
    {
        [Serializable]
        private struct CueEntry
        {
            public GameAudioCue cue;
            public AudioClip clip;
        }

        [Serializable]
        private struct MusicEntry
        {
            public string sceneName;
            public AudioClip clip;
        }

        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup masterGroup;
        [SerializeField] private CueEntry[] cues = Array.Empty<CueEntry>();
        [SerializeField] private MusicEntry[] musicTracks = Array.Empty<MusicEntry>();

        public AudioMixer Mixer => mixer;
        public AudioMixerGroup MasterGroup => masterGroup;

        public bool TryGetCue(GameAudioCue cue, out AudioClip clip)
        {
            for (int index = 0; index < cues.Length; index++)
            {
                if (cues[index].cue != cue)
                {
                    continue;
                }

                clip = cues[index].clip;
                return clip != null;
            }

            clip = null;
            return false;
        }

        public bool TryGetMusicForScene(string sceneName, out AudioClip clip)
        {
            for (int index = 0; index < musicTracks.Length; index++)
            {
                if (!string.Equals(musicTracks[index].sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                clip = musicTracks[index].clip;
                return clip != null;
            }

            clip = null;
            return false;
        }
    }
}
