using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Framework.Managers;

namespace Project.Scripts.Content.Minigame.GateDrop
{
    /// <summary>
    /// 낙하 게이트의 임시 효과음을 실행 중에 합성합니다. 정식 소리가 생기면 Audio/SFX/ 의 클립으로 바꿉니다.
    /// 핀 소리는 위 줄일수록 높은 5음 음계로, 캔이 내려갈수록 음이 내려갑니다.
    /// </summary>
    public class GateDropSound
    {
        private const int SampleRate = 22050;
        /// <summary>핀 소리 음 개수 (5음 음계 2옥타브).</summary>
        private const int PegNotes = 10;
        private const float PegBaseFrequency = 523.25f;
        /// <summary>클립 끝을 줄여 딸깍 소리를 막는 길이(샘플).</summary>
        private const int TailFadeSamples = 48;
        private static readonly int[] Pentatonic = { 0, 2, 4, 7, 9 };

        private delegate float Wave(float time);

        private readonly List<AudioClip> _owned = new();
        private readonly AudioClip[] _pegs = new AudioClip[PegNotes];
        private readonly AudioClip _wall;
        private readonly AudioClip _gateBump;
        private readonly AudioClip _gateOpen;
        private readonly AudioClip _button;
        private readonly AudioClip _cursor;
        private readonly AudioClip _drop;
        private readonly AudioClip _land;
        private readonly AudioClip _settle;
        private readonly AudioClip _win;
        private readonly AudioClip _dud;
        private readonly AudioClip _rattle;

        public GateDropSound()
        {
            for(int i = 0; i < PegNotes; i++)
            {
                float frequency = PegBaseFrequency * Semitone(12 * (i / Pentatonic.Length) + Pentatonic[i % Pentatonic.Length]);
                _pegs[i] = Create($"GateDrop_Peg_{i}", 0.07f, t => Pluck(t, frequency, 0.018f, 0.35f));
            }

            _wall = Create("GateDrop_Wall", 0.09f, t => Pluck(t, 260f, 0.03f, 0.5f) * 0.8f);
            _gateBump = Create("GateDrop_GateBump", 0.14f, t =>
                (Sine(t, 740f) * 0.5f + Sine(t, 1480f) * 0.25f + Sine(t, 2146f) * 0.15f) * Decay(t, 0.05f) * Attack(t)
                + Noise(t) * 0.3f * Decay(t, 0.004f));
            // 걸쇠가 풀리는 "착-척": 짧은 잡음 두 번과 낮은 울림
            _gateOpen = Create("GateDrop_GateOpen", 0.12f, t =>
                Noise(t) * 0.45f * (Decay(t, 0.004f) + Decay(Mathf.Max(0f, t - 0.045f), 0.006f) * Step(t, 0.045f))
                + Sine(t, 180f) * 0.35f * Decay(t, 0.03f) * Attack(t));
            _button = Create("GateDrop_Button", 0.04f, t =>
                Square(t, 900f) * 0.25f * Decay(t, 0.012f) + Noise(t) * 0.3f * Decay(t, 0.002f));
            _cursor = Create("GateDrop_Cursor", 0.035f, t => Sine(t, 1500f) * 0.18f * Decay(t, 0.01f) * Attack(t));
            _drop = Create("GateDrop_Drop", 0.16f, t =>
                Triangle(Sweep(t, 1000f, 350f, 0.16f)) * 0.28f * Attack(t) * (1f - t / 0.16f));
            _land = Create("GateDrop_Land", 0.2f, t =>
                Mathf.Sin(Sweep(t, 160f, 70f, 0.2f) * 2f * Mathf.PI) * 0.7f * Decay(t, 0.06f) * Attack(t)
                + Noise(t) * 0.35f * Decay(t, 0.01f));
            _settle = Create("GateDrop_Settle", 0.05f, t => Pluck(t, 520f, 0.012f, 0.2f) * 0.5f);
            _win = Create("GateDrop_Win", 0.5f, t =>
                Note(t, 0f, 1318.5f, 0.1f) + Note(t, 0.07f, 1568f, 0.1f) + Note(t, 0.14f, 2093f, 0.25f));
            // 꽝: 한 음 내려가며 흔들리는 "뽀-옹"
            _dud = Create("GateDrop_Dud", 0.5f, t =>
                Mellow(t, 392f) * 0.22f * Window(t, 0f, 0.14f)
                + Mellow(t, 311f + 6f * Mathf.Sin(t * 40f)) * 0.22f * Window(t, 0.16f, 0.48f));
            _rattle = Create("GateDrop_Rattle", 0.02f, t => Noise(t) * 0.18f * Decay(t, 0.002f));
        }

        /// <summary>progress: 0 = 첫 줄, 1 = 마지막 줄.</summary>
        public void PlayPeg(float progress)
        {
            int note = Mathf.RoundToInt((1f - Mathf.Clamp01(progress)) * (PegNotes - 1));
            Play(_pegs[note]);
        }

        public void PlayWall() => Play(_wall);
        public void PlayGateBump() => Play(_gateBump);
        public void PlayGateOpen() => Play(_gateOpen);
        public void PlayButton() => Play(_button);
        public void PlayCursor() => Play(_cursor);
        public void PlayDrop() => Play(_drop);
        public void PlayLand() => Play(_land);
        public void PlaySettle() => Play(_settle);
        public void PlayWin() => Play(_win);
        public void PlayDud() => Play(_dud);
        public void PlayRattle() => Play(_rattle);

        /// <summary>만든 클립을 모두 해제합니다.</summary>
        public void Release()
        {
            foreach(AudioClip clip in _owned)
            {
                if(clip != null)
                    Object.Destroy(clip);
            }
            _owned.Clear();
        }

        private static void Play(AudioClip clip)
        {
            if(AudioManager.HasInstance)
                AudioManager.Instance.PlaySFX(clip);
        }

        private AudioClip Create(string name, float duration, Wave wave)
        {
            int length = Mathf.CeilToInt(duration * SampleRate);
            float[] data = new float[length];
            for(int i = 0; i < length; i++)
                data[i] = Mathf.Clamp(wave(i / (float)SampleRate), -1f, 1f);
            for(int i = 0; i < Mathf.Min(TailFadeSamples, length); i++)
                data[length - 1 - i] *= i / (float)TailFadeSamples;

            AudioClip clip = AudioClip.Create(name, length, 1, SampleRate, false);
            clip.SetData(data, 0);
            _owned.Add(clip);
            return clip;
        }

        #region Waves

        // 퉁기는 소리: 기본음 + 어긋난 배음, 시작에 짧은 딸깍
        private static float Pluck(float t, float frequency, float decay, float click)
        {
            float tone = Sine(t, frequency) * 0.6f + Sine(t, frequency * 2.76f) * 0.25f * Decay(t, decay * 0.5f);
            return tone * Decay(t, decay) * Attack(t) + Noise(t) * click * Decay(t, 0.003f);
        }

        // start초에 시작해 decay로 줄어드는 세모파 한 음
        private static float Note(float t, float start, float frequency, float decay)
        {
            if(t < start)
                return 0f;
            float local = t - start;
            return Triangle(local * frequency) * 0.24f * Decay(local, decay) * Attack(local);
        }

        // 네모파를 사인과 섞어 부드럽게 만든 소리
        private static float Mellow(float t, float frequency) => Square(t, frequency) * 0.35f + Sine(t, frequency) * 0.65f;

        private static float Sine(float t, float frequency) => Mathf.Sin(2f * Mathf.PI * frequency * t);

        private static float Square(float t, float frequency) => Mathf.Repeat(t * frequency, 1f) < 0.5f ? 1f : -1f;

        /// <summary>phase는 주기 단위 (1 = 한 주기).</summary>
        private static float Triangle(float phase) => 1f - 4f * Mathf.Abs(Mathf.Repeat(phase + 0.25f, 1f) - 0.5f);

        /// <summary>주파수가 duration 동안 from에서 to로 바뀌는 소리의 위상 (주기 단위).</summary>
        private static float Sweep(float t, float from, float to, float duration) => from * t + (to - from) * t * t / (2f * duration);

        private static float Decay(float t, float tau) => Mathf.Exp(-t / tau);

        private static float Attack(float t) => Mathf.Clamp01(t / 0.002f);

        private static float Step(float t, float start) => t >= start ? 1f : 0f;

        // start~end 구간만 소리 내고 양 끝을 짧게 이어 붙임
        private static float Window(float t, float start, float end)
        {
            if(t < start || t > end)
                return 0f;
            return Mathf.Clamp01((t - start) / 0.01f) * Mathf.Clamp01((end - t) / 0.06f);
        }

        // 샘플 번호로 만든 잡음 (System.Random 없이 늘 같은 값)
        private static float Noise(float t)
        {
            int index = Mathf.FloorToInt(t * SampleRate);
            float hash = Mathf.Sin(index * 12.9898f) * 43758.5453f;
            return (hash - Mathf.Floor(hash)) * 2f - 1f;
        }

        private static float Semitone(int semitones) => Mathf.Pow(2f, semitones / 12f);

        #endregion
    }
}
