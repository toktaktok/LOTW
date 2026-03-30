using System.Collections;
using UnityEngine;

namespace Project.Scripts.Core.Managers
{
    /// <summary>
    /// 볼륨 정보를 제공하는 인터페이스.
    /// AudioManager가 이 인터페이스를 통해 볼륨을 읽으므로,
    /// GameInstance 외 다른 시스템으로도 교체 가능합니다.
    /// </summary>
    public interface IVolumeProvider
    {
        float MasterVolume { get; }
        float BgmVolume { get; }
        float SfxVolume { get; }
    }

    /// <summary>
    /// BGM과 SFX 재생을 관리하는 싱글턴 매니저.
    /// IVolumeProvider를 통해 볼륨을 읽습니다.
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        #region Fields

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Settings")]
        [SerializeField] private float crossfadeDuration = 1f;

        private Coroutine _crossfadeRoutine;
        private IVolumeProvider _volumeProvider;

        #endregion

        #region Lifecycle

        protected override void Awake()
        {
            base.Awake();

            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }
        }

        private void Start()
        {
            if (_volumeProvider == null)
                SetVolumeProvider(GameInstance.Instance);

            ApplyVolume();
        }

        private void OnDestroy()
        {
            GameInstance.OnSettingsChanged -= ApplyVolume;
        }

        #endregion

        #region Volume Provider

        /// <summary>
        /// 볼륨 제공자를 주입합니다. 기본값은 GameInstance입니다.
        /// </summary>
        public void SetVolumeProvider(IVolumeProvider provider)
        {
            // 기존 이벤트 해제
            GameInstance.OnSettingsChanged -= ApplyVolume;

            _volumeProvider = provider;

            // GameInstance인 경우 자동으로 이벤트 구독
            if (provider is GameInstance)
                GameInstance.OnSettingsChanged += ApplyVolume;

            ApplyVolume();
        }

        #endregion

        #region BGM

        public void PlayBGM(AudioClip clip, bool crossfade = true)
        {
            if (clip == null) return;

            if (bgmSource.clip == clip && bgmSource.isPlaying)
                return;

            if (crossfade && bgmSource.isPlaying)
            {
                if (_crossfadeRoutine != null)
                    StopCoroutine(_crossfadeRoutine);
                _crossfadeRoutine = StartCoroutine(CrossfadeRoutine(clip));
            }
            else
            {
                bgmSource.clip = clip;
                bgmSource.Play();
                ApplyVolume();
            }
        }

        public void StopBGM(bool fadeOut = true)
        {
            if (!bgmSource.isPlaying) return;

            if (fadeOut)
            {
                if (_crossfadeRoutine != null)
                    StopCoroutine(_crossfadeRoutine);
                _crossfadeRoutine = StartCoroutine(FadeOutRoutine());
            }
            else
            {
                bgmSource.Stop();
            }
        }

        private IEnumerator CrossfadeRoutine(AudioClip newClip)
        {
            float half = crossfadeDuration * 0.5f;
            float startVol = bgmSource.volume;

            float timer = 0f;
            while (timer < half)
            {
                timer += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, timer / half);
                yield return null;
            }

            bgmSource.clip = newClip;
            bgmSource.Play();

            float targetVol = GetBgmVolume();
            timer = 0f;
            while (timer < half)
            {
                timer += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(0f, targetVol, timer / half);
                yield return null;
            }

            bgmSource.volume = targetVol;
            _crossfadeRoutine = null;
        }

        private IEnumerator FadeOutRoutine()
        {
            float startVol = bgmSource.volume;
            float timer = 0f;
            float duration = crossfadeDuration * 0.5f;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, timer / duration);
                yield return null;
            }

            bgmSource.Stop();
            bgmSource.volume = startVol;
            _crossfadeRoutine = null;
        }

        #endregion

        #region SFX

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip, GetSfxVolume());
        }

        public void PlaySFXAtPoint(AudioClip clip, Vector3 position)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, GetSfxVolume());
        }

        #endregion

        #region Volume

        public void ApplyVolume()
        {
            if (_volumeProvider == null) return;
            bgmSource.volume = GetBgmVolume();
            sfxSource.volume = GetSfxVolume();
        }

        private float GetBgmVolume()
        {
            if (_volumeProvider == null) return 1f;
            return _volumeProvider.MasterVolume * _volumeProvider.BgmVolume;
        }

        private float GetSfxVolume()
        {
            if (_volumeProvider == null) return 1f;
            return _volumeProvider.MasterVolume * _volumeProvider.SfxVolume;
        }

        #endregion
    }
}
