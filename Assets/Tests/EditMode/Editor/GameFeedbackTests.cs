using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PipeMuzzle.Feedback;
using UnityEngine;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class GameFeedbackTests
    {
        private const string SoundKey = "PipeMuzzle.Feedback.SoundEnabled";
        private const string HapticsKey = "PipeMuzzle.Feedback.HapticsEnabled";
        private readonly Dictionary<string, (bool exists, int value)> savedPrefs = new();

        [SetUp]
        public void SetUp()
        {
            ResetServiceSettings();
            foreach (string key in new[] { SoundKey, HapticsKey })
            {
                savedPrefs[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key));
                PlayerPrefs.DeleteKey(key);
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var entry in savedPrefs)
            {
                if (entry.Value.exists) PlayerPrefs.SetInt(entry.Key, entry.Value.value);
                else PlayerPrefs.DeleteKey(entry.Key);
            }
            PlayerPrefs.Save();
            savedPrefs.Clear();
            ResetServiceSettings();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PublicSettingsApiReloadsBothPreferencesAfterRuntimeReset(bool enabled)
        {
            GameFeedback.SetSoundEnabled(enabled);
            GameFeedback.SetHapticsEnabled(!enabled);
            ResetServiceSettings();
            Assert.That(GameFeedback.SoundEnabled, Is.EqualTo(enabled));
            Assert.That(GameFeedback.HapticsEnabled, Is.EqualTo(!enabled));
        }

        [Test]
        public void EditModeFeedbackCallsDoNotCreatePersistentSceneObjects()
        {
            int before = Resources.FindObjectsOfTypeAll<GameFeedback>().Length;
            GameFeedback.PlayPipeRotate();
            GameFeedback.PlayLevelComplete();
            GameFeedback.SetAudioClips(null, null);
            GameFeedback.StartFlow(null, "SakuraGarden", .5f);
            GameFeedback.PlayTargetReached(null);
            GameFeedback.StopFlow(null);
            Assert.That(Resources.FindObjectsOfTypeAll<GameFeedback>().Length, Is.EqualTo(before));
            Assert.That(PlayerPrefs.HasKey(SoundKey), Is.False);
            Assert.That(PlayerPrefs.HasKey(HapticsKey), Is.False);
        }

        private static void ResetServiceSettings() => typeof(GameFeedback)
            .GetMethod("ResetStatics", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);

        [TestCase("SakuraGarden", .5f)]
        [TestCase("BambooWorkshop", .8f)]
        [TestCase("MoonShrine", 1.05f)]
        public void FlowClipMatchesTravelTimeAndHasSoftFiniteSamples(string world, float duration)
        {
            AudioClip clip = ProceduralFeedbackClips.CreateFlowClip(world, duration);
            try
            {
                Assert.That(clip.length, Is.EqualTo(duration).Within(1f / 44100));
                AssertSoftSamples(clip);
            }
            finally { Object.DestroyImmediate(clip); }
        }

        [TestCase("SakuraGarden")]
        [TestCase("BambooWorkshop")]
        [TestCase("MoonShrine")]
        public void TargetReachedClipEndsBeforeTheExistingArrivalHold(string world)
        {
            AudioClip clip = ProceduralFeedbackClips.CreateTargetReachedClip(world);
            try
            {
                Assert.That(clip.length, Is.InRange(.05f, .10f), "Leave room before the existing 120 ms completion hold ends.");
                AssertSoftSamples(clip);
            }
            finally { Object.DestroyImmediate(clip); }
        }

        private static void AssertSoftSamples(AudioClip clip)
        {
            Assert.That(clip.channels, Is.EqualTo(1));
            Assert.That(clip.frequency, Is.EqualTo(44100));
            float[] samples = new float[clip.samples];
            Assert.That(clip.GetData(samples, 0), Is.True);
            float peak = 0f;
            float jump = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                Assert.That(float.IsNaN(samples[i]) || float.IsInfinity(samples[i]), Is.False);
                peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
                if (i > 0) jump = Mathf.Max(jump, Mathf.Abs(samples[i] - samples[i - 1]));
            }
            Assert.That(peak, Is.InRange(.01f, .30f));
            Assert.That(jump, Is.LessThan(.05f), "Soft boundaries must not pop.");
            Assert.That(samples[0], Is.Zero);
            Assert.That(samples[samples.Length - 1], Is.Zero);
        }

        [Test]
        public void BothSettingsDefaultToEnabledWithoutCreatingPreferenceKeys()
        {
            FeedbackSettings settings = new();
            Assert.That(settings.SoundEnabled, Is.True);
            Assert.That(settings.HapticsEnabled, Is.True);
            Assert.That(PlayerPrefs.HasKey(SoundKey), Is.False);
            Assert.That(PlayerPrefs.HasKey(HapticsKey), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SoundSettingPersistsAcrossInstancesWithoutChangingHaptics(bool enabled)
        {
            FeedbackSettings settings = new();
            settings.SetSoundEnabled(!enabled);
            settings.SetSoundEnabled(enabled);
            Assert.That(settings.SoundEnabled, Is.EqualTo(enabled));
            Assert.That(new FeedbackSettings().SoundEnabled, Is.EqualTo(enabled));
            Assert.That(PlayerPrefs.GetInt(SoundKey), Is.EqualTo(enabled ? 1 : 0));
            Assert.That(new FeedbackSettings().HapticsEnabled, Is.True);
            Assert.That(PlayerPrefs.HasKey(HapticsKey), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HapticsSettingPersistsAcrossInstancesWithoutChangingSound(bool enabled)
        {
            FeedbackSettings settings = new();
            settings.SetHapticsEnabled(!enabled);
            settings.SetHapticsEnabled(enabled);
            Assert.That(settings.HapticsEnabled, Is.EqualTo(enabled));
            Assert.That(new FeedbackSettings().HapticsEnabled, Is.EqualTo(enabled));
            Assert.That(PlayerPrefs.GetInt(HapticsKey), Is.EqualTo(enabled ? 1 : 0));
            Assert.That(new FeedbackSettings().SoundEnabled, Is.True);
            Assert.That(PlayerPrefs.HasKey(SoundKey), Is.False);
        }

        [TestCase(false, 0.04f, 0.10f)]
        [TestCase(true, 0.30f, 0.70f)]
        public void ProceduralClipsHaveShortDurationsAndSoftFiniteEnvelopes(
            bool completion, float minimumDuration, float maximumDuration)
        {
            AudioClip clip = completion
                ? ProceduralFeedbackClips.CreateLevelComplete()
                : ProceduralFeedbackClips.CreatePipeRotate();
            try
            {
                Assert.That(clip, Is.Not.Null);
                Assert.That(clip.length, Is.InRange(minimumDuration, maximumDuration));
                Assert.That(clip.channels, Is.EqualTo(1));
                Assert.That(clip.frequency, Is.EqualTo(44100));
                float[] samples = new float[clip.samples];
                Assert.That(clip.GetData(samples, 0), Is.True);
                float peak = 0f;
                foreach (float sample in samples)
                {
                    Assert.That(float.IsNaN(sample) || float.IsInfinity(sample), Is.False);
                    peak = Mathf.Max(peak, Mathf.Abs(sample));
                }
                Assert.That(peak, Is.InRange(0.01f, 0.40f), "Quiet but audible PCM without clipping.");
                Assert.That(samples[0], Is.EqualTo(0f).Within(0.0001f));
                Assert.That(samples[samples.Length - 1], Is.EqualTo(0f).Within(0.0001f));
                float maximumJump = 0f;
                for (int i = 1; i < samples.Length; i++)
                    maximumJump = Mathf.Max(maximumJump, Mathf.Abs(samples[i] - samples[i - 1]));
                Assert.That(maximumJump, Is.LessThan(0.05f), "Envelope boundaries must not pop.");
            }
            finally
            {
                if (clip != null) Object.DestroyImmediate(clip);
            }
        }
    }
}
