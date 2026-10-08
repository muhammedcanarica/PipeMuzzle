using System;
using System.Reflection;
using NUnit.Framework;
using PipeMuzzle.Feedback;
using PipeMuzzle.Data;
using UnityEngine;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class MusicTests
    {
        private const string MusicKey = "PipeMuzzle.Feedback.MusicVolume";
        private const string SfxKey = "PipeMuzzle.Feedback.SfxVolume";
        private bool musicExisted, sfxExisted;
        private float savedMusic, savedSfx;
        private GameObject owner;
        private AudioClip a, b, c;
        private AudioSource first, second;
        private object playback;
        private Type playbackType;
        [SetUp] public void Setup()
        {
            musicExisted = PlayerPrefs.HasKey(MusicKey); savedMusic = PlayerPrefs.GetFloat(MusicKey);
            sfxExisted = PlayerPrefs.HasKey(SfxKey); savedSfx = PlayerPrefs.GetFloat(SfxKey);
            PlayerPrefs.DeleteKey(MusicKey); PlayerPrefs.DeleteKey(SfxKey);
        }
        [TearDown] public void Cleanup()
        {
            if (musicExisted) PlayerPrefs.SetFloat(MusicKey, savedMusic); else PlayerPrefs.DeleteKey(MusicKey);
            if (sfxExisted) PlayerPrefs.SetFloat(SfxKey, savedSfx); else PlayerPrefs.DeleteKey(SfxKey);
            PlayerPrefs.Save();
            foreach (UnityEngine.Object item in new UnityEngine.Object[] {owner, a, b, c}) if (item != null) UnityEngine.Object.DestroyImmediate(item);
            typeof(GameFeedback).GetMethod("ResetStatics", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        }
        [TestCase(0f, 0f)] [TestCase(.23f, .23f)] [TestCase(2f, 1f)] [TestCase(-1f, 0f)] [TestCase(float.NaN, .4f)]
        public void MusicVolumePersistsIndependentlyOfSfx(float input, float expected)
        {
            var settings = new FeedbackSettings(); settings.SetSfxVolume(.67f);
            MethodInfo setter = typeof(FeedbackSettings).GetMethod("SetMusicVolume"); Assert.That(setter, Is.Not.Null);
            setter.Invoke(settings, new object[] {input});
            var reloaded = new FeedbackSettings();
            Assert.That(typeof(FeedbackSettings).GetProperty("MusicVolume").GetValue(reloaded), Is.EqualTo(expected));
            Assert.That(reloaded.SfxVolume, Is.EqualTo(.67f));
        }
        [Test] public void DefaultMusicVolumeDoesNotWritePreference()
        {
            PropertyInfo property = typeof(FeedbackSettings).GetProperty("MusicVolume"); Assert.That(property, Is.Not.Null);
            Assert.That(property.GetValue(new FeedbackSettings()), Is.EqualTo(.4f));
            Assert.That(PlayerPrefs.HasKey(MusicKey), Is.False);
        }
        private void BuildPlayback()
        {
            playbackType = typeof(GameFeedback).Assembly.GetType("PipeMuzzle.Feedback.MusicPlayback");
            Assert.That(playbackType, Is.Not.Null, "Music must share the existing audio owner.");
            owner = new GameObject("MusicTests"); first = owner.AddComponent<AudioSource>(); second = owner.AddComponent<AudioSource>();
            a = AudioClip.Create("A", 44100 * 8, 1, 44100, false); b = AudioClip.Create("B", 44100 * 8, 1, 44100, false); c = AudioClip.Create("C", 44100 * 8, 1, 44100, false);
            playback = Activator.CreateInstance(playbackType, first, second);
        }
        private void Request(AudioClip clip) => playbackType.GetMethod("Request").Invoke(playback, new object[] {clip});
        private void Advance(float dt) => playbackType.GetMethod("Advance").Invoke(playback, new object[] {dt});
        private void SetVolume(float value) => playbackType.GetMethod("SetVolume").Invoke(playback, new object[] {value});
        [TestCase("Main", "turning_pages-level-select-screen-602555")]
        [TestCase("SakuraGarden", "pianocafe_kumi-japanese-calm-pianomoon-411035")]
        [TestCase("BambooWorkshop", "solarflex-cozy-chill-lounge-music-491499")]
        [TestCase("MoonShrine", "tomomi_kato-crescent-moon-173121")]
        public void ShippedCatalogueMapsAllFourStreamingTracks(string context, string expected)
        {
            MusicTracks tracks = Resources.Load<MusicTracks>("Audio/MusicTracks");
            Assert.That(tracks, Is.Not.Null);
            AudioClip clip = context == "Main" ? tracks.Main : tracks.ForWorld((WorldId)Enum.Parse(typeof(WorldId), context));
            Assert.That(clip, Is.Not.Null); Assert.That(clip.name, Is.EqualTo(expected));
            Assert.That(clip.loadType, Is.EqualTo(AudioClipLoadType.Streaming));
            Assert.That(clip.length, Is.GreaterThan(20f));
        }
        [Test] public void MissingClipRequestLeavesCurrentMusicIntact()
        {
            BuildPlayback(); SetVolume(.4f); Request(a); Advance(1f); Request(null); Advance(1f);
            Assert.That(first.clip, Is.SameAs(a)); Assert.That(first.volume, Is.EqualTo(.4f)); Assert.That(second.clip, Is.Null);
        }
        [Test] public void RepeatedTrackRequestDoesNotRestartFadeOrAssignSecondVoice()
        {
            BuildPlayback(); SetVolume(.4f); Request(a); Advance(.5f);
            float gain = first.volume;
            Request(a); Advance(.5f);
            Assert.That(first.clip, Is.SameAs(a)); Assert.That(first.volume, Is.GreaterThan(gain));
            Assert.That(second.clip, Is.Null); Assert.That(first.loop && second.loop, Is.True);
        }
        [Test] public void RapidThirdTrackRequestKeepsCurrentMixAndOnlyLatestPendingTrack()
        {
            BuildPlayback(); SetVolume(.4f); Request(a); Advance(1f); Request(b); Advance(.25f);
            float left = first.volume, right = second.volume;
            Request(c); Request(a); Request(c);
            Assert.That(first.volume, Is.EqualTo(left)); Assert.That(second.volume, Is.EqualTo(right));
            Advance(.75f); Advance(1f);
            Assert.That(first.clip, Is.SameAs(c)); Assert.That(first.volume, Is.EqualTo(.4f).Within(.001f));
            Assert.That(second.volume, Is.Zero); Assert.That(second.clip, Is.Null);
        }
        [Test] public void ZeroVolumeKeepsClipsAndCrossfadeState()
        {
            BuildPlayback(); SetVolume(.4f); Request(a); Advance(1f);
            Request(b); Advance(.3f); SetVolume(0f);
            Assert.That(first.volume + second.volume, Is.Zero); Assert.That(first.clip, Is.SameAs(a));
            Advance(.7f); SetVolume(.2f);
            Assert.That(second.clip, Is.SameAs(b)); Assert.That(second.volume, Is.EqualTo(.2f).Within(.001f));
        }
    }
}
