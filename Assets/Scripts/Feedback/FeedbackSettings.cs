using UnityEngine;

namespace PipeMuzzle.Feedback
{
    /// <summary>Small persistent settings API, independent of playback and scene lifetime.</summary>
    public sealed class FeedbackSettings
    {
        private const string SoundKey = "PipeMuzzle.Feedback.SoundEnabled";
        private const string HapticsKey = "PipeMuzzle.Feedback.HapticsEnabled";

        public bool SoundEnabled { get; private set; }
        public bool HapticsEnabled { get; private set; }

        public FeedbackSettings()
        {
            SoundEnabled = PlayerPrefs.GetInt(SoundKey, 1) != 0;
            HapticsEnabled = PlayerPrefs.GetInt(HapticsKey, 1) != 0;
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
    }
}
