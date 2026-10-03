using System.Xml;
using NUnit.Framework;
using PipeMuzzle.Editor;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class FeedbackAndroidManifestTests
    {
        [TestCase("android")]
        [TestCase("a")]
        public void VibrationPermissionIsAddedOnceAndPreservesOtherManifestContent(string prefix)
        {
            XmlDocument manifest = new();
            manifest.LoadXml($"<manifest xmlns:{prefix}='http://schemas.android.com/apk/res/android' package='test.game'>"
                + $"<uses-permission {prefix}:name='android.permission.INTERNET'/><application {prefix}:label='PipeMuzzle'/>"
                + "</manifest>");
            string application = manifest.DocumentElement.SelectSingleNode("application").OuterXml;
            Assert.That(FeedbackAndroidManifest.AddVibrationPermission(manifest), Is.True);
            Assert.That(FeedbackAndroidManifest.AddVibrationPermission(manifest), Is.False);
            Assert.That(manifest.DocumentElement.SelectNodes("uses-permission").Count, Is.EqualTo(2));
            Assert.That(manifest.DocumentElement.SelectSingleNode("application").OuterXml, Is.EqualTo(application));
        }

        [Test]
        public void ExistingVibrationPermissionIsLeftUntouched()
        {
            XmlDocument manifest = new();
            manifest.LoadXml("<manifest xmlns:android='http://schemas.android.com/apk/res/android'>"
                + "<uses-permission android:name='android.permission.VIBRATE'/><application/></manifest>");
            string original = manifest.OuterXml;
            Assert.That(FeedbackAndroidManifest.AddVibrationPermission(manifest), Is.False);
            Assert.That(manifest.OuterXml, Is.EqualTo(original));
        }
    }
}
