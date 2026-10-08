using UnityEngine;

namespace PipeMuzzle.Feedback
{
    /// <summary>Small persistent settings API, independent of playback and scene lifetime.</summary>
    public sealed class FeedbackSettings
    {
        private const string SoundKey = "PipeMuzzle.Feedback.SoundEnabled";
        private const string HapticsKey = "PipeMuzzle.Feedback.HapticsEnabled";
        private const string VolumeKey = "PipeMuzzle.Feedback.SfxVolume";
        private const string MusicKey = "PipeMuzzle.Feedback.MusicVolume";

        public bool SoundEnabled { get; private set; }
        public bool HapticsEnabled { get; private set; }
        public float SfxVolume { get; private set; }
        public float MusicVolume { get; private set; }

        public FeedbackSettings()
        {
            SoundEnabled = PlayerPrefs.GetInt(SoundKey, 1) != 0;
            HapticsEnabled = PlayerPrefs.GetInt(HapticsKey, 1) != 0;
            SfxVolume = SanitizeVolume(PlayerPrefs.GetFloat(VolumeKey, .75f));
            MusicVolume = SanitizeMusicVolume(PlayerPrefs.GetFloat(MusicKey, .4f));
        }

        public void SetSoundEnabled(bool enabled)
        {
            SoundEnabled = enabled;
            PlayerPrefs.SetInt(SoundKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetHapticsEnabled(bool enabled)
        {
            HapticsEnabled = enabled;
            PlayerPrefs.SetInt(HapticsKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = SanitizeVolume(value);
            PlayerPrefs.SetFloat(VolumeKey, SfxVolume);
            PlayerPrefs.Save();
        }

        private static float SanitizeVolume(float value) =>
            float.IsNaN(value) || float.IsInfinity(value) ? .75f : Mathf.Clamp01(value);

        public void SetMusicVolume(float value)
        {
            MusicVolume = SanitizeMusicVolume(value);
            PlayerPrefs.SetFloat(MusicKey, MusicVolume);
            PlayerPrefs.Save();
        }
        private static float SanitizeMusicVolume(float value) =>
            float.IsNaN(value) || float.IsInfinity(value) ? .4f : Mathf.Clamp01(value);
    }
}
