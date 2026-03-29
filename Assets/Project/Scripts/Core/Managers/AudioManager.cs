using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Core;

namespace Project.Scripts.Core.Managers
{
    /// <summary>
    /// BGM과 SFX 재생을 관리하는 싱글턴 매니저.
    /// GameInstance의 볼륨 설정과 연동됩니다.
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

            GameInstance.OnSettingsChanged += ApplyVolume;
            ApplyVolume();
        }

        private void OnDestroy()
        {
            GameInstance.OnSettingsChanged -= ApplyVolume;
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

            // Fade out
            float timer = 0f;
            while (timer < half)
            {
                timer += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, timer / half);
                yield return null;
            }

            bgmSource.clip = newClip;
            bgmSource.Play();

            // Fade in
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

        private void ApplyVolume()
        {
            bgmSource.volume = GetBgmVolume();
            sfxSource.volume = GetSfxVolume();
        }

        private float GetBgmVolume()
        {
            return GameInstance.Instance.MasterVolume * GameInstance.Instance.BgmVolume;
        }

        private float GetSfxVolume()
        {
            return GameInstance.Instance.MasterVolume * GameInstance.Instance.SfxVolume;
        }

        #endregion
    }
}
