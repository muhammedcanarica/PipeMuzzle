using System.Collections;
using PipeMuzzle.Data;
using UnityEngine;

namespace PipeMuzzle.Feedback
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class GameFeedback : MonoBehaviour
    {
        private static GameFeedback instance;
        private static FeedbackSettings settings;
        [Header("Optional world clip overrides")]
        [SerializeField] private AudioClip pipeRotateClip;
        [SerializeField] private AudioClip levelCompleteClip;
        [SerializeField, Range(0f, 1f)] private float volume = .45f;
        [Header("Music (defaults to Resources/Audio/MusicTracks)")]
        [SerializeField] private MusicTracks musicTracks;
        private MusicPlayback music;
        private readonly System.Collections.Generic.HashSet<string> missingMusic = new();

        // Four bounded voices, all owned by this one persistent feedback object.
        private AudioSource audioSource, flowAudioSource, uiAudioSource, cueAudioSource;
        private AudioClip rotateDefault, completeDefault, hintClip, uiClip, flowClip, targetClip, unlockClip;
        private Object flowOwner;
        private bool targetReached;
        private float nextRotateAt, nextUiAt, cueEndsAt;
        private float rotateGain, flowGain, uiGain, cueGain;
        private Coroutine unlockRoutine;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject vibrator;
        private int androidApiLevel;
        private bool hapticsUnavailable;
#endif

        private static FeedbackSettings Settings => settings ??= new FeedbackSettings();
        public static bool SoundEnabled => Settings.SoundEnabled;
        public static bool HapticsEnabled => Settings.HapticsEnabled;
        public static float SfxVolume => Settings.SfxVolume;
        public static float MusicVolume => Settings.MusicVolume;
        private static bool Audible => SoundEnabled && SfxVolume > 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance = null; settings = null; }

        public static void SetSoundEnabled(bool enabled)
        {
            Settings.SetSoundEnabled(enabled);
            if (instance != null) instance.ApplyVolume();
        }
        public static void SetHapticsEnabled(bool enabled) => Settings.SetHapticsEnabled(enabled);
        public static void SetSfxVolume(float value)
        {
            Settings.SetSfxVolume(value);
            if (instance != null) instance.ApplyVolume();
        }

        public static void SetMusicVolume(float value)
        {
            Settings.SetMusicVolume(value);
            if (instance != null) instance.music.SetVolume(Settings.MusicVolume);
        }
        public static void PlayMainMusic()
        {
            GameFeedback feedback = GetOrCreate();
            if (feedback != null) feedback.RequestMusic(feedback.musicTracks != null ? feedback.musicTracks.Main : null, "Main / World Map");
        }
        public static void PlayWorldMusic(WorldId world)
        {
            GameFeedback feedback = GetOrCreate();
            if (feedback != null) feedback.RequestMusic(feedback.musicTracks != null ? feedback.musicTracks.ForWorld(world) : null, world.ToString());
        }
        private void RequestMusic(AudioClip clip, string context)
        {
            if (clip != null) music.Request(clip);
            else if (missingMusic.Add(context)) Debug.LogWarning("Missing Ruilay music clip: " + context + ". Assign it in Audio/MusicTracks; gameplay will continue.");
        }
        private void Update() => music?.Advance(Time.unscaledDeltaTime);

        public static void PlayPipeRotate()
        {
            if (!Audible) return;
            GameFeedback feedback = GetOrCreate();
            if (feedback == null || Time.unscaledTime < feedback.nextRotateAt) return;
            feedback.nextRotateAt = Time.unscaledTime + .06f;
            feedback.rotateGain = .8f;
            feedback.Play(feedback.audioSource, feedback.pipeRotateClip, feedback.rotateGain, Random.Range(.96f, 1.04f));
        }
        public static void PlayUiClick()
        {
            if (!Audible) return;
            GameFeedback feedback = GetOrCreate();
            if (feedback == null || Time.unscaledTime < feedback.nextUiAt) return;
            feedback.nextUiAt = Time.unscaledTime + .04f;
            feedback.uiGain = .5f;
            feedback.Play(feedback.uiAudioSource, feedback.uiClip, feedback.uiGain);
        }
        public static void PlayHint()
        {
            if (!Audible) return;
            GameFeedback feedback = GetOrCreate();
            if (feedback != null) feedback.PlayCue(feedback.hintClip, 1.2f);
        }
        public static void PlayLevelComplete()
        {
            if (!Audible && !HapticsEnabled) return;
            GameFeedback feedback = GetOrCreate();
            if (feedback == null) return;
            if (Audible) feedback.PlayCue(feedback.levelCompleteClip, 1.3f);
            if (HapticsEnabled) feedback.PlayCompletionHaptic();
        }
        public static void PlayWorldUnlock()
        {
            if (!Audible) return;
            GameFeedback feedback = GetOrCreate();
            if (feedback == null || feedback.unlockRoutine != null) return;
            feedback.unlockRoutine = feedback.StartCoroutine(feedback.UnlockAfterCompletion());
        }
        private IEnumerator UnlockAfterCompletion()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, cueEndsAt - Time.unscaledTime) + .06f);
            unlockRoutine = null;
            if (Audible) PlayCue(unlockClip, 1.5f);
        }
        public static void StartFlow(Object owner, string worldId, float duration)
        {
            if (owner == null || !Audible || duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration)) return;
            GameFeedback feedback = GetOrCreate();
            if (feedback == null || feedback.flowOwner == owner) return;
            feedback.StopFlowPlayback();
            feedback.flowOwner = owner;
            feedback.flowGain = .7f;
            // Adapt the cached 800 ms waveform to the existing visual travel clock.
            if (feedback.flowClip != null)
                feedback.Play(feedback.flowAudioSource, feedback.flowClip, feedback.flowGain, feedback.flowClip.length / duration);
        }
        public static void PlayTargetReached(Object owner)
        {
            if (instance == null || owner == null || instance.flowOwner != owner || instance.targetReached) return;
            instance.targetReached = true;
            instance.flowAudioSource.Stop();
            instance.flowGain = .85f;
            if (Audible) instance.Play(instance.flowAudioSource, instance.targetClip, instance.flowGain);
        }
        public static void StopFlow(Object owner)
        {
            if (instance != null && owner != null && instance.flowOwner == owner) instance.StopFlowPlayback();
        }
        /// <summary>Keep existing world overrides; null restores the shipped local WAV.</summary>
        public static void SetAudioClips(AudioClip pipeRotate, AudioClip levelComplete)
        {
            GameFeedback feedback = GetOrCreate();
            if (feedback == null) return;
            feedback.pipeRotateClip = pipeRotate != null ? pipeRotate : feedback.rotateDefault;
            feedback.levelCompleteClip = levelComplete != null ? levelComplete : feedback.completeDefault;
        }
        private static GameFeedback GetOrCreate()
        {
            if (!Application.isPlaying) return null;
            if (instance == null) new GameObject(nameof(GameFeedback)).AddComponent<GameFeedback>();
            return instance;
        }
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
            ConfigureSource(audioSource);
            flowAudioSource = NewSource(); uiAudioSource = NewSource(); cueAudioSource = NewSource();
            rotateDefault = Load("PipeRotate"); completeDefault = Load("LevelComplete");
            hintClip = Load("Hint"); uiClip = Load("UiClick"); flowClip = Load("WaterFlow");
            targetClip = Load("TargetReached"); unlockClip = Load("WorldUnlock");
            if (pipeRotateClip == null) pipeRotateClip = rotateDefault;
            if (levelCompleteClip == null) levelCompleteClip = completeDefault;
            ApplyVolume();
            if (musicTracks == null) musicTracks = Resources.Load<MusicTracks>("Audio/MusicTracks");
            music = new MusicPlayback(NewSource(), NewSource());
            music.SetVolume(Settings.MusicVolume);
        }
        private static AudioClip Load(string name)
        {
            AudioClip clip = Resources.Load<AudioClip>("Audio/SFX/" + name);
            if (clip == null) Debug.LogError("Missing Ruilay SFX: " + name);
            return clip;
        }
        private AudioSource NewSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            ConfigureSource(source);
            return source;
        }
        private static void ConfigureSource(AudioSource source)
        { source.playOnAwake = false; source.loop = false; source.spatialBlend = 0f; }
        private void Play(AudioSource source, AudioClip clip, float gain, float pitch = 1f)
        {
            if (clip == null || source == null) return;
            source.Stop(); source.clip = clip; source.pitch = pitch;
            source.volume = Mathf.Clamp01(volume * SfxVolume * gain);
            source.Play();
        }
        private void PlayCue(AudioClip clip, float gain)
        {
            cueGain = gain;
            Play(cueAudioSource, clip, gain);
            cueEndsAt = Time.unscaledTime + (clip != null ? clip.length : 0f);
        }
        private void ApplyVolume()
        {
            audioSource.volume = Audible ? Mathf.Clamp01(volume * SfxVolume * rotateGain) : 0f;
            flowAudioSource.volume = Audible ? Mathf.Clamp01(volume * SfxVolume * flowGain) : 0f;
            uiAudioSource.volume = Audible ? Mathf.Clamp01(volume * SfxVolume * uiGain) : 0f;
            cueAudioSource.volume = Audible ? Mathf.Clamp01(volume * SfxVolume * cueGain) : 0f;
            if (!Audible)
            {
                audioSource.Stop(); uiAudioSource.Stop(); cueAudioSource.Stop(); StopFlowPlayback();
                if (unlockRoutine != null) StopCoroutine(unlockRoutine);
                unlockRoutine = null;
            }
        }
        private void StopFlowPlayback()
        {
            if (flowAudioSource != null) { flowAudioSource.Stop(); flowAudioSource.clip = null; }
            flowOwner = null; targetReached = false;
        }
        private void OnDisable() => StopFlowPlayback();
        private void PlayCompletionHaptic()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (hapticsUnavailable) return;
            try
            {
                if (vibrator == null)
                {
                    using (AndroidJavaClass player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
                        androidApiLevel = version.GetStatic<int>("SDK_INT");
                }
                if (vibrator == null || !vibrator.Call<bool>("hasVibrator"))
                {
                    hapticsUnavailable = true;
                    return;
                }
                const long durationMilliseconds = 40;
                if (androidApiLevel >= 26)
                {
                    int amplitude = vibrator.Call<bool>("hasAmplitudeControl") ? 64 : -1;
                    using (AndroidJavaClass effects = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (AndroidJavaObject effect = effects.CallStatic<AndroidJavaObject>(
                        "createOneShot", durationMilliseconds, amplitude))
                        vibrator.Call("vibrate", effect);
                }
                else
                {
                    vibrator.Call("vibrate", durationMilliseconds);
                }
            }
            catch (System.Exception)
            {
                // Unsupported devices, permission/Java failures must not interrupt gameplay.
                hapticsUnavailable = true;
                vibrator?.Dispose();
                vibrator = null;
            }
#endif
        }

        private void OnDestroy()
        {
            music?.Stop();
            StopFlowPlayback();
            if (instance == this) instance = null;
#if UNITY_ANDROID && !UNITY_EDITOR
            vibrator?.Dispose();
#endif
        }
    }
}
