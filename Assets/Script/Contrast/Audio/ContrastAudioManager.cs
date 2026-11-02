using Contrast.Color;
using UnityEngine;

namespace Contrast.Audio
{
    /// <summary>
    /// Procedural audio synthesizer and sound dispatcher for Contrast.
    /// Synthesizes crystal-clear optical chimes and kinetic audio cues in memory
    /// using standard PCM audio buffers. Requires zero external audio files.
    /// Operates identically across Windows and Android platforms.
    /// </summary>
    public sealed class ContrastAudioManager : MonoBehaviour
    {
        public static ContrastAudioManager Instance { get; private set; }

        private AudioSource audioSource;

        private AudioClip clipJump;
        private AudioClip clipWhiteShift;
        private AudioClip clipGrayShift;
        private AudioClip clipBlackShift;
        private AudioClip clipCheckpoint;
        private AudioClip clipWin;
        private AudioClip clipDeath;
        private AudioClip clipClick;

        private const int SampleRate = 44100;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnLoad()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("ContrastAudioManager");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<ContrastAudioManager>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound
            }

            GenerateClips();
        }

        private void GenerateClips()
        {
            clipJump = CreateFrequencySweepClip("Jump", 260f, 520f, 0.12f, 0.45f);
            clipWhiteShift = CreateToneClip("Shift_White", 660f, 0.08f, 0.4f);
            clipGrayShift = CreateToneClip("Shift_Gray", 440f, 0.08f, 0.4f);
            clipBlackShift = CreateToneClip("Shift_Black", 220f, 0.10f, 0.45f);
            clipCheckpoint = CreateDualToneClip("Checkpoint", 523.25f, 659.25f, 0.35f, 0.5f);
            clipWin = CreateArpeggioClip("Win", new float[] { 261.63f, 329.63f, 392.00f, 523.25f }, 0.6f, 0.55f);
            clipDeath = CreateFrequencySweepClip("Death", 380f, 120f, 0.22f, 0.45f);
            clipClick = CreateToneClip("Click", 880f, 0.02f, 0.25f);
        }

        public void PlayJump()
        {
            PlayOneShot(clipJump);
        }

        public void PlayColorShift(LogicalColor color)
        {
            switch (color)
            {
                case LogicalColor.White:
                    PlayOneShot(clipWhiteShift);
                    break;
                case LogicalColor.Gray:
                    PlayOneShot(clipGrayShift);
                    break;
                case LogicalColor.Black:
                    PlayOneShot(clipBlackShift);
                    break;
            }
        }

        public void PlayCheckpoint()
        {
            PlayOneShot(clipCheckpoint);
        }

        public void PlayWin()
        {
            PlayOneShot(clipWin);
        }

        public void PlayDeath()
        {
            PlayOneShot(clipDeath);
        }

        public void PlayButtonClick()
        {
            PlayOneShot(clipClick);
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        // ── Procedural Sound Synthesizers ────────────────────────────

        private static AudioClip CreateToneClip(string name, float frequency, float duration, float volume)
        {
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float envelope = Mathf.Exp(-5f * t / duration); // Exponential decay
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateFrequencySweepClip(string name, float startFreq, float endFreq, float duration, float volume)
        {
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[sampleCount];

            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float currentFreq = Mathf.Lerp(startFreq, endFreq, t);
                phase += 2f * Mathf.PI * currentFreq / SampleRate;

                // Smooth attack and release envelope
                float envelope = Mathf.Sin(t * Mathf.PI);
                samples[i] = Mathf.Sin(phase) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateDualToneClip(string name, float freq1, float freq2, float duration, float volume)
        {
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float envelope = Mathf.Exp(-4f * t / duration);
                float wave = (Mathf.Sin(2f * Mathf.PI * freq1 * t) + Mathf.Sin(2f * Mathf.PI * freq2 * t)) * 0.5f;
                samples[i] = wave * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateArpeggioClip(string name, float[] notes, float duration, float volume)
        {
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[sampleCount];
            float noteDuration = duration / notes.Length;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                int noteIndex = Mathf.Clamp(Mathf.FloorToInt(t / noteDuration), 0, notes.Length - 1);
                float noteT = t - (noteIndex * noteDuration);

                float envelope = Mathf.Exp(-3f * noteT / noteDuration);
                samples[i] = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * t) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
