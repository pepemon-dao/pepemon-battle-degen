using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Scripts.Managers.Sound
{
    public class ThemePlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip[] _chillSongs;
        [SerializeField] private AudioClip[] _actionSongs;
        [SerializeField] private AudioClip _loseClip;
        [SerializeField] private AudioClip _winClip;
        [SerializeField] private AudioSource _themePlayer;
        [SerializeField] private List<string> _chillScenes;
        [SerializeField] private List<string> _actionScenes;
        [SerializeField] private AudioMixer _mainGroup;

        private bool _isChillMode = true;
        private bool _wasChillMode = true;
        private int _previousClipNumber = -1;

        private bool isMuted = false;
        private bool isDestroyed = false;

        private float volume = 0.1f;

        public static ThemePlayer Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _themePlayer.mute = true;

            volume = _themePlayer.volume;
            InvokeRepeating(nameof(CheckIfMusicEnded), 0f, 6f);
        }

        private void CheckIfMusicEnded()
        {
            if (!_themePlayer.isPlaying)
            {
                StartCoroutine(PlayNextMusic());
            }
        }

        private IEnumerator PlayNextMusic(AudioClip clip = null)
        {
            int randomClipNumber = GetRandomClipNumber();
            AudioClip nextClip = GetCurrentClip(randomClipNumber);

            if (clip != null)
            {
                nextClip = clip;
            }

            // Start volume transition
            StartCoroutine(TransitionVolume(volume, 0f, 0.5f)); // Transition to volume 0

            yield return new WaitForSeconds(0.5f); // Wait for transition to complete

            _themePlayer.clip = nextClip;
            _themePlayer.Play();

            // Resume volume transition
            StartCoroutine(TransitionVolume(0f, volume, 0.5f)); // Transition to volume 1

            _previousClipNumber = randomClipNumber;
        }
        
        
        private IEnumerator PlayNextMusic(AudioClip clip, float newVolume)
        {
            int randomClipNumber = GetRandomClipNumber();
            AudioClip nextClip = GetCurrentClip(randomClipNumber);

            if (clip != null)
            {
                nextClip = clip;
            }

            // Start volume transition
            StartCoroutine(TransitionVolume(volume, 0f, 0.5f)); // Transition to volume 0

            yield return new WaitForSeconds(0.5f); // Wait for transition to complete

            _themePlayer.clip = nextClip;
            _themePlayer.Play();

            // Resume volume transition
            StartCoroutine(TransitionVolume(0f, newVolume, 0.5f)); // Transition to volume 1

            _previousClipNumber = randomClipNumber;
        }

        public void MuteItGradually()
        {
            StartCoroutine(TransitionVolume(volume, 0f, 0.5f)); // Transition to volume 0
            isMuted = true;
        }

        public void TurnOnGradually()
        {
            isMuted = false;
            StartCoroutine(TransitionVolume(0f, volume, 0.5f)); // Transition back
        }

        private IEnumerator PlayNextMusicWithDelay(AudioClip nextClip)
        {
            yield return new WaitForSeconds(0.5f); // Wait for transition to complete

            _themePlayer.clip = nextClip;
            _themePlayer.Play();

            StartCoroutine(TransitionVolume(0f, volume, 0.5f)); // Transition to volume 1
        }

        private IEnumerator TransitionVolume(float startVolume, float targetVolume, float transitionTime)
        {
            if (isMuted) yield break;

            float elapsedTime = 0f;
            float transitionSpeed = 1f / transitionTime;

            while (elapsedTime < transitionTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime * transitionSpeed;

                _themePlayer.volume = Mathf.Lerp(startVolume, targetVolume, t);
                yield return null;
            }

            // Ensure final volume is set correctly
            _themePlayer.volume = targetVolume;
        }

        private int GetRandomClipNumber()
        {
            int randomClipNumber;
            if (_isChillMode)
            {
                randomClipNumber = Random.Range(0, _chillSongs.Length);
                if (_chillSongs.Length > 1)
                {
                    while (randomClipNumber == _previousClipNumber)
                    {
                        randomClipNumber = Random.Range(0, _chillSongs.Length);
                    }
                }
            }
            else
            {
                randomClipNumber = Random.Range(0, _actionSongs.Length);
                if (_chillSongs.Length > 1)
                {
                    while (randomClipNumber == _previousClipNumber)
                    {
                        randomClipNumber = Random.Range(0, _actionSongs.Length);
                    }
                }
            }
            return randomClipNumber;
        }

        private AudioClip GetCurrentClip(int clipNumber)
        {
            return _isChillMode ? _chillSongs[clipNumber] : _actionSongs[clipNumber];
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            string sceneName = scene.name;
            _isChillMode = _chillScenes.Contains(sceneName);

            if (!isDestroyed && _isChillMode != _wasChillMode)
            {
                _wasChillMode = _isChillMode;
                StartCoroutine(PlayNextMusic());
            }
            
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            isDestroyed = true;
        }

        public void ToggleAudio(bool enable)
        {
            bool isMuted = !enable;
            _mainGroup.SetFloat("ThemeVolume", isMuted ? -80f : 0f);
        }

        public void UnMute()
        {
            print("theme player unmuted");
            _themePlayer.mute = false;
        }

        public void SkipEndGameMusic()
        {
            StartCoroutine(PlayNextMusic());
        }
        
        public void PlayGameOverSong(bool isWon)
        {
            if (isWon)
            {
                StartCoroutine(PlayNextMusic(_winClip, 0.3f));
            }
            else
            {
                StartCoroutine(PlayNextMusic(_loseClip, 0.3f));
            }
        }
    }
}
