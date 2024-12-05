using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Managers
{
    public class SFXPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;

        private void Start()
        {
            InvokeRepeating(nameof(IsStillPlaying), 0.1f, 0.1f);
        }

        private void IsStillPlaying()
        {
            if (!_audioSource.isPlaying)
            {
                Destroy(gameObject);
            }
        }

        public void PlaySFX(AudioClip sfx)
        {
            _audioSource.PlayOneShot(sfx);
        }
        
        public void PlaySFXWithPitch(AudioClip sfx, float pitch)
        {
            _audioSource.pitch = pitch;
            _audioSource.PlayOneShot(sfx);
        }

        public void PlaySFXWithVolume(AudioClip sfx, float volume)
        {
            _audioSource.volume = volume;
            _audioSource.PlayOneShot(sfx);
        }     
    }
}
