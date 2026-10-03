using UnityEngine;

namespace PipeMuzzle.Feedback
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class GameFeedback : MonoBehaviour
    {
        private static GameFeedback instance;
        private static FeedbackSettings settings;

        [Header("Optional clip assets (procedural defaults when empty)")]
        [SerializeField] private AudioClip pipeRotateClip;
        [SerializeField] private AudioClip levelCompleteClip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.45f;

        private AudioSource audioSource;
        private AudioClip generatedRotateClip;
        private AudioClip generatedCompleteClip;
        private AudioSource flowAudioSource;
        private AudioClip generatedFlowClip;
        private readonly System.Collections.Generic.Dictionary<string, AudioClip> targetClips = new();
        private Object flowOwner;
        private string flowWorldId;
        private bool targetReached;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject vibrator;
        private int androidApiLevel;
        private bool hapticsUnavailable;
#endif

        private static FeedbackSettings Settings => settings ??= new FeedbackSettings();
        public static bool SoundEnabled => Settings.SoundEnabled;
        public static bool HapticsEnabled => Settings.HapticsEnabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Also reset when entering Play Mode with domain reload disabled.
            instance = null;
            settings = null;
        }

        public static void SetSoundEnabled(bool enabled)
        {
            Settings.SetSoundEnabled(enabled);
            if (!enabled && instance != null)
            {
                instance.audioSource.Stop();
                instance.StopFlowPlayback();
            }
        }

        public static void SetHapticsEnabled(bool enabled) => Settings.SetHapticsEnabled(enabled);

        public static void PlayPipeRotate()
        {
            if (!SoundEnabled) return;
            GameFeedback feedback = GetOrCreate();
            if (feedback != null) feedback.audioSource.PlayOneShot(feedback.pipeRotateClip, feedback.volume);
        }

        public static void PlayLevelComplete()
        {
            if (!SoundEnabled && !HapticsEnabled) return;
            GameFeedback feedback = GetOrCreate();
            if (feedback == null) return;
            if (SoundEnabled)
                feedback.audioSource.PlayOneShot(feedback.levelCompleteClip, feedback.volume);
            if (HapticsEnabled) feedback.PlayCompletionHaptic();
        }

        public static void StartFlow(Object owner, string worldId, float duration)
        {
            if (owner == null || !SoundEnabled || duration <= 0f ||
                float.IsNaN(duration) || float.IsInfinity(duration)) return;
            GameFeedback feedback = GetOrCreate();
            if (feedback == null || feedback.flowOwner == owner) return;
            feedback.StopFlowPlayback();
            feedback.flowOwner = owner;
            feedback.flowWorldId = worldId ?? "SakuraGarden";
            feedback.generatedFlowClip = ProceduralFeedbackClips.CreateFlowClip(feedback.flowWorldId, duration);
            feedback.flowAudioSource.clip = feedback.generatedFlowClip;
            feedback.flowAudioSource.volume = feedback.volume * .60f;
            feedback.flowAudioSource.Play();
        }

        public static void PlayTargetReached(Object owner)
        {
            if (instance == null || owner == null || instance.flowOwner != owner || instance.targetReached) return;
            instance.targetReached = true;
            instance.flowAudioSource.Stop();
            instance.flowAudioSource.clip = null;
            instance.ReleaseFlowClip();
            if (!SoundEnabled) return;
            if (!instance.targetClips.TryGetValue(instance.flowWorldId, out AudioClip clip))
            {
                clip = ProceduralFeedbackClips.CreateTargetReachedClip(instance.flowWorldId);
                instance.targetClips.Add(instance.flowWorldId, clip);
            }
            instance.flowAudioSource.clip = clip;
            instance.flowAudioSource.volume = instance.volume * .55f;
            instance.flowAudioSource.Play();
        }

        public static void StopFlow(Object owner)
        {
            if (instance != null && owner != null && instance.flowOwner == owner)
                instance.StopFlowPlayback();
        }

        /// <summary>Use real clip assets later; null restores the cached procedural default.</summary>
        public static void SetAudioClips(AudioClip pipeRotate, AudioClip levelComplete)
        {
            GameFeedback feedback = GetOrCreate();
            if (feedback == null) return;
            feedback.pipeRotateClip = pipeRotate != null ? pipeRotate : feedback.GetRotateDefault();
            feedback.levelCompleteClip = levelComplete != null ? levelComplete : feedback.GetCompleteDefault();
        }

        private static GameFeedback GetOrCreate()
        {
            // EditMode controller tests must not create persistent objects or play audio.
            if (!Application.isPlaying) return null;
            if (instance == null)
                new GameObject(nameof(GameFeedback)).AddComponent<GameFeedback>();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;
            flowAudioSource = gameObject.AddComponent<AudioSource>();
            flowAudioSource.playOnAwake = false;
            flowAudioSource.loop = false;
            flowAudioSource.spatialBlend = 0f;
            if (pipeRotateClip == null) pipeRotateClip = GetRotateDefault();
            if (levelCompleteClip == null) levelCompleteClip = GetCompleteDefault();
        }

        private AudioClip GetRotateDefault()
        {
            if (generatedRotateClip == null) generatedRotateClip = ProceduralFeedbackClips.CreatePipeRotate();
            return generatedRotateClip;
        }

        private AudioClip GetCompleteDefault()
        {
            if (generatedCompleteClip == null) generatedCompleteClip = ProceduralFeedbackClips.CreateLevelComplete();
            return generatedCompleteClip;
        }

        private void StopFlowPlayback()
        {
            if (flowAudioSource != null)
            {
                flowAudioSource.Stop();
                flowAudioSource.clip = null;
            }
            ReleaseFlowClip();
            flowOwner = null;
            flowWorldId = null;
            targetReached = false;
        }

        private void ReleaseFlowClip()
        {
            if (generatedFlowClip != null) Destroy(generatedFlowClip);
            generatedFlowClip = null;
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
            StopFlowPlayback();
            if (instance == this) instance = null;
            if (generatedRotateClip != null) Destroy(generatedRotateClip);
            if (generatedCompleteClip != null) Destroy(generatedCompleteClip);
            foreach (AudioClip clip in targetClips.Values)
                if (clip != null) Destroy(clip);
            targetClips.Clear();
#if UNITY_ANDROID && !UNITY_EDITOR
            vibrator?.Dispose();
#endif
        }
    }
}
