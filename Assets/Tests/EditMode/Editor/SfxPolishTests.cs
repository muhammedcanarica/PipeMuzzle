using System.Reflection;
using NUnit.Framework;
using PipeMuzzle.Feedback;
using UnityEngine;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class SfxPolishTests
    {
        private const string Key = "PipeMuzzle.Feedback.SfxVolume";
        private bool existed;
        private float saved;
        [SetUp] public void Setup() { existed = PlayerPrefs.HasKey(Key); saved = PlayerPrefs.GetFloat(Key); PlayerPrefs.DeleteKey(Key); }
        [TearDown] public void Cleanup() { if (existed) PlayerPrefs.SetFloat(Key, saved); else PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); typeof(GameFeedback).GetMethod("ResetStatics", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null); }

        [TestCase(0f, 0f)] [TestCase(.32f, .32f)] [TestCase(-1f, 0f)] [TestCase(2f, 1f)]
        public void VolumeClampsAndPersistsAcrossSettingsInstances(float input, float expected)
        {
            var settings = new FeedbackSettings();
            MethodInfo setter = typeof(FeedbackSettings).GetMethod("SetSfxVolume");
            Assert.That(setter, Is.Not.Null, "Persistent SFX volume API is required.");
            setter.Invoke(settings, new object[] { input });
            Assert.That(typeof(FeedbackSettings).GetProperty("SfxVolume").GetValue(new FeedbackSettings()), Is.EqualTo(expected));
        }
        [Test] public void DefaultVolumeIsSeventyFivePercentWithoutWritingPrefs()
        {
            PropertyInfo property = typeof(FeedbackSettings).GetProperty("SfxVolume");
            Assert.That(property, Is.Not.Null);
            Assert.That(property.GetValue(new FeedbackSettings()), Is.EqualTo(.75f));
            Assert.That(PlayerPrefs.HasKey(Key), Is.False);
        }
        [TestCase("PipeRotate", .06f, .12f)] [TestCase("Hint", .2f, .4f)]
        [TestCase("UiClick", .04f, .09f)] [TestCase("WaterFlow", .5f, .9f)]
        [TestCase("LevelComplete", .6f, 1.2f)] [TestCase("WorldUnlock", 1f, 1.6f)]
        [TestCase("TargetReached", .05f, .1f)]
        public void ShippedClipsAreShortMonoAndHaveHeadroom(string name, float minimum, float maximum)
        {
            AudioClip clip = Resources.Load<AudioClip>("Audio/SFX/" + name);
            Assert.That(clip, Is.Not.Null, name);
            Assert.That(clip.channels, Is.EqualTo(1));
            Assert.That(clip.length, Is.InRange(minimum, maximum));
            float[] samples = new float[clip.samples];
            Assert.That(clip.GetData(samples, 0), Is.True);
            float peak = 0f;
            foreach (float sample in samples) { Assert.That(float.IsNaN(sample) || float.IsInfinity(sample), Is.False); peak = Mathf.Max(peak, Mathf.Abs(sample)); }
            Assert.That(peak, Is.InRange(.01f, .5f));
            Assert.That(Mathf.Abs(samples[0]) + Mathf.Abs(samples[samples.Length - 1]), Is.LessThan(.001f));
        }

        [TestCase(620f)] [TestCase(930f)]
        public void WaterFlowHasNoSustainedElectronicHum(float frequency)
        {
            AudioClip clip = Resources.Load<AudioClip>("Audio/SFX/WaterFlow");
            float[] samples = new float[clip.samples];
            Assert.That(clip.GetData(samples, 0), Is.True);
            double sine = 0, cosine = 0, energy = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                double phase = 2 * System.Math.PI * frequency * i / clip.frequency;
                sine += samples[i] * System.Math.Sin(phase);
                cosine += samples[i] * System.Math.Cos(phase);
                energy += samples[i] * samples[i];
            }
            double tone = 2 * System.Math.Sqrt(sine * sine + cosine * cosine) / samples.Length;
            double rms = System.Math.Sqrt(energy / samples.Length);
            Assert.That(tone, Is.LessThan(rms * .12), "A continuous pitched tone overwhelms the soft water texture.");
        }
    }
}
