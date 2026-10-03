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
            if (!enabled && instance != null) instance.audioSource.Stop();
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
            if (instance == this) instance = null;
            if (generatedRotateClip != null) Destroy(generatedRotateClip);
            if (generatedCompleteClip != null) Destroy(generatedCompleteClip);
#if UNITY_ANDROID && !UNITY_EDITOR
            vibrator?.Dispose();
#endif
        }
    }
}
