using System;
using UnityEngine;

namespace SemillasVivas.Systems.Audio
{
    
    public sealed class GameAudioSetup : MonoBehaviour
    {
        [Serializable]
        public struct SceneMusic
        {
            [Tooltip("Nombre exacto de la escena (distingue mayúsculas).")]
            public string sceneName;
            [Tooltip("AudioClip de música para esa escena.")]
            public AudioClip clip;
        }

        [SerializeField] private SceneMusic[] musicByScene;

        private void Awake()
        {
            GameAudioService service = GameAudioService.EnsureInstance();

            if (musicByScene == null)
            {
                return;
            }

            foreach (SceneMusic entry in musicByScene)
            {
                if (!string.IsNullOrEmpty(entry.sceneName) && entry.clip != null)
                {
                    service.RegisterSceneMusic(entry.sceneName, entry.clip);
                }
            }
        }
    }
}
