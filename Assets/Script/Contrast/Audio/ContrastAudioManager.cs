using System;
using System.Collections;
using System.IO;
using Contrast.Color;
using UnityEngine;
using UnityEngine.Networking;

namespace Contrast.Audio
{
    /// <summary>
    /// Audio dispatcher and background music controller for Contrast.
    /// Manages looping MP3 background music (Menu: "Calima - Full Moon",
    /// Levels L01-L03: "Aventure - Evening Tone"), procedural optical SFX,
    /// and persistent Music / SFX volume settings.
    /// </summary>
    public sealed class ContrastAudioManager : MonoBehaviour
    {
        public static ContrastAudioManager Instance { get; private set; }

        public const string MenuMusicResource = "Sound/Calima - Full Moon (freetouse.com)";
        public const string GameplayMusicResource = "Sound/Aventure - Evening Tone (freetouse.com)";
        public const string MenuMusicFileName = "Calima - Full Moon (freetouse.com).mp3";
        public const string GameplayMusicFileName = "Aventure - Evening Tone (freetouse.com).mp3";

        private const string PrefKeyMusicVol = "Contrast_MusicVolume";
        private const string PrefKeySfxVol = "Contrast_SfxVolume";
        public const float DefaultMusicVolume = 0.65f;
        public const float DefaultSfxVolume = 0.80f;

        private AudioSource sfxSource;
        private AudioSource bgmSource;

        private AudioClip clipMenuBgm;
        private AudioClip clipGameplayBgm;
        private string activeTrackKey = string.Empty;
        private Coroutine bgmLoadCoroutine;

        private AudioClip clipJump;
        private AudioClip clipWhiteShift;
        private AudioClip clipGrayShift;
        private AudioClip clipBlackShift;
        private AudioClip clipCheckpoint;
        private AudioClip clipWin;
        private AudioClip clipDeath;
        private AudioClip clipClick;

        private float musicVolume = DefaultMusicVolume;
        private float sfxVolume = DefaultSfxVolume;

        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;
        public string ActiveTrackKey => activeTrackKey;
        public string CurrentTrackDisplayName => activeTrackKey == "gameplay"
            ? "Aventure — Evening Tone"
            : "Calima — Full Moon";

        public event Action<float, float> OnVolumeChanged;

        private const int SampleRate = 44100;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnLoad()
        {
            EnsureInstance();
        }

        public static ContrastAudioManager EnsureInstance()
        {
            if (Instance == null)
            {
                Instance = FindAnyObjectByType<ContrastAudioManager>();
                if (Instance == null)
                {
                    GameObject go = new GameObject("ContrastAudioManager");
                    if (Application.isPlaying)
                        DontDestroyOnLoad(go);
                    Instance = go.AddComponent<ContrastAudioManager>();
                }
            }
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
                return;
            }
            Instance = this;

            EnsureAudioSources();
            LoadSavedVolumes();
            GenerateClips();
        }

        private void EnsureAudioSources()
        {
            AudioSource[] existing = GetComponents<AudioSource>();
            if (existing != null && existing.Length >= 1)
                sfxSource = existing[0];
            if (existing != null && existing.Length >= 2)
                bgmSource = existing[1];

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;

            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.spatialBlend = 0f;
        }

        private void LoadSavedVolumes()
        {
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefKeyMusicVol, DefaultMusicVolume));
            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefKeySfxVol, DefaultSfxVolume));
            ApplyVolumesToSources();
        }

        private void ApplyVolumesToSources()
        {
            if (bgmSource != null)
                bgmSource.volume = musicVolume;
            if (sfxSource != null)
                sfxSource.volume = sfxVolume;
        }

        public void SetMusicVolume(float value)
        {
            EnsureAudioSources();
            musicVolume = Mathf.Clamp01(value);
            if (bgmSource != null)
                bgmSource.volume = musicVolume;
            PlayerPrefs.SetFloat(PrefKeyMusicVol, musicVolume);
            PlayerPrefs.Save();
            OnVolumeChanged?.Invoke(musicVolume, sfxVolume);
        }

        public void SetSfxVolume(float value)
        {
            EnsureAudioSources();
            sfxVolume = Mathf.Clamp01(value);
            if (sfxSource != null)
                sfxSource.volume = sfxVolume;
            PlayerPrefs.SetFloat(PrefKeySfxVol, sfxVolume);
            PlayerPrefs.Save();
            OnVolumeChanged?.Invoke(musicVolume, sfxVolume);
        }

        public void ResetDefaultVolumes()
        {
            SetMusicVolume(DefaultMusicVolume);
            SetSfxVolume(DefaultSfxVolume);
        }

        /// <summary>
        /// Plays "Calima - Full Moon" for Main Menu / Waiting Screen.
        /// </summary>
        public void PlayMenuMusic()
        {
            EnsureAudioSources();
            if (activeTrackKey == "menu" && bgmSource != null && bgmSource.isPlaying && bgmSource.clip != null)
                return;

            activeTrackKey = "menu";
            if (clipMenuBgm == null)
                clipMenuBgm = Resources.Load<AudioClip>(MenuMusicResource);

            if (clipMenuBgm != null)
            {
                StartLoopingClip(clipMenuBgm);
            }
            else if (Application.isPlaying)
            {
                if (bgmLoadCoroutine != null)
                    StopCoroutine(bgmLoadCoroutine);
                bgmLoadCoroutine = StartCoroutine(LoadExternalMp3AndPlayRoutine("menu", MenuMusicFileName));
            }
        }

        /// <summary>
        /// Plays "Aventure - Evening Tone" across Gameplay Levels L01, L02, and L03.
        /// </summary>
        public void PlayGameplayMusic()
        {
            EnsureAudioSources();
            if (activeTrackKey == "gameplay" && bgmSource != null && bgmSource.isPlaying && bgmSource.clip != null)
                return;

            activeTrackKey = "gameplay";
            if (clipGameplayBgm == null)
                clipGameplayBgm = Resources.Load<AudioClip>(GameplayMusicResource);

            if (clipGameplayBgm != null)
            {
                StartLoopingClip(clipGameplayBgm);
            }
            else if (Application.isPlaying)
            {
                if (bgmLoadCoroutine != null)
                    StopCoroutine(bgmLoadCoroutine);
                bgmLoadCoroutine = StartCoroutine(LoadExternalMp3AndPlayRoutine("gameplay", GameplayMusicFileName));
            }
        }

        private void StartLoopingClip(AudioClip clip)
        {
            if (bgmSource == null || clip == null)
                return;

            if (bgmSource.clip == clip && bgmSource.isPlaying)
            {
                bgmSource.volume = musicVolume;
                return;
            }

            bgmSource.Stop();
            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.volume = musicVolume;
            bgmSource.Play();
        }

        private IEnumerator LoadExternalMp3AndPlayRoutine(string trackKey, string fileName)
        {
            string candidatePath = ResolveMp3DiskPath(fileName);
            if (string.IsNullOrEmpty(candidatePath) || !File.Exists(candidatePath))
                yield break;

            string uri = new Uri(candidatePath).AbsoluteUri;
            using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[AUDIO] Could not stream MP3 '{fileName}': {req.error}");
                    yield break;
                }

                AudioClip loaded = DownloadHandlerAudioClip.GetContent(req);
                if (loaded == null)
                    yield break;

                loaded.name = Path.GetFileNameWithoutExtension(fileName);
                if (trackKey == "menu")
                    clipMenuBgm = loaded;
                else
                    clipGameplayBgm = loaded;

                if (activeTrackKey == trackKey)
                {
                    StartLoopingClip(loaded);
                }
            }
        }

        private static string ResolveMp3DiskPath(string fileName)
        {
            string resPath = Path.Combine(Application.dataPath, "Resources", "Sound", fileName);
            if (File.Exists(resPath))
                return resPath;

            DirectoryInfo parentDir = Directory.GetParent(Application.dataPath);
            if (parentDir != null)
            {
                string rootSound = Path.Combine(parentDir.FullName, "Sound", fileName);
                if (File.Exists(rootSound))
                    return rootSound;
            }

            return null;
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
            clipClick = CreateToneClip("Click", 880f, 0.025f, 0.30f);
        }

        public void PlayJump() => PlayOneShot(clipJump);

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

        public void PlayCheckpoint() => PlayOneShot(clipCheckpoint);
        public void PlayWin() => PlayOneShot(clipWin);
        public void PlayDeath() => PlayOneShot(clipDeath);
        public void PlayButtonClick() => PlayOneShot(clipClick);

        private void PlayOneShot(AudioClip clip)
        {
            if (sfxSource != null && clip != null && sfxVolume > 0.0001f)
            {
                sfxSource.PlayOneShot(clip, sfxVolume);
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
                float envelope = Mathf.Exp(-5f * t / duration);
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
