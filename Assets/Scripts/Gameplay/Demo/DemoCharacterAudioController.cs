using SemillasVivas.Systems.Audio;
using UnityEngine;

namespace SemillasVivas.Gameplay.Demo
{
    public sealed class DemoCharacterAudioController : MonoBehaviour
    {
        private GameAudioService _audioService;
        private AudioSource _loopSource;
        private AudioSource _oneShotSource;
        private GameAudioCue _footstepCue;
        private bool _supportsFootsteps;

        public void Initialize(GameAudioService audioService, GameAudioCue footstepCue, bool supportsFootsteps)
        {
            _audioService = audioService;
            _footstepCue = footstepCue;
            _supportsFootsteps = supportsFootsteps;

            _oneShotSource ??= _audioService.CreateWorldSource(transform);

            if (_supportsFootsteps)
            {
                _loopSource ??= _audioService.CreateWorldSource(transform, loop: true);

                if (_audioService.TryGetClip(_footstepCue, out AudioClip footstepClip))
                {
                    _loopSource.clip = footstepClip;
                    _loopSource.volume = 0.6f;
                }
            }
        }

        public void Play(GameAudioCue cue, float volume = 1f)
        {
            if (_audioService == null || _oneShotSource == null)
            {
                return;
            }

            if (!_audioService.TryGetClip(cue, out AudioClip clip) || clip == null)
            {
                return;
            }

            _oneShotSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        public void UpdateFootsteps(bool isMoving, bool isGrounded)
        {
            if (!_supportsFootsteps || _loopSource == null || _loopSource.clip == null)
            {
                return;
            }

            bool shouldPlay = isMoving && isGrounded;

            if (shouldPlay)
            {
                if (!_loopSource.isPlaying)
                {
                    _loopSource.Play();
                }

                return;
            }

            if (_loopSource.isPlaying)
            {
                _loopSource.Stop();
            }
        }

        public void StopAllLoops()
        {
            if (_loopSource != null && _loopSource.isPlaying)
            {
                _loopSource.Stop();
            }
        }
    }
}
