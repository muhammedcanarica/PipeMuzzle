using System.IO;
using System.Xml;
using UnityEditor.Android;

namespace PipeMuzzle.Editor
{
    /// <summary>Direct Java vibration needs an explicit permission in the generated manifest.</summary>
    public sealed class FeedbackAndroidManifest : IPostGenerateGradleAndroidProject
    {
        private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
        private const string VibrationPermission = "android.permission.VIBRATE";
        public int callbackOrder => 100;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            XmlDocument manifest = new() { PreserveWhitespace = true };
            manifest.Load(manifestPath);
            if (AddVibrationPermission(manifest)) manifest.Save(manifestPath);
        }

        internal static bool AddVibrationPermission(XmlDocument manifest)
        {
            XmlElement root = manifest.DocumentElement;
            if (root == null || root.Name != "manifest")
                throw new InvalidDataException("Android manifest root is missing.");
            foreach (XmlNode node in root.SelectNodes("uses-permission"))
            {
                if (node is XmlElement permission &&
                    permission.GetAttribute("name", AndroidNamespace) == VibrationPermission)
                    return false;
            }
            XmlElement entry = manifest.CreateElement("uses-permission");
            entry.SetAttribute("name", AndroidNamespace, VibrationPermission);
            root.AppendChild(entry);
            return true;
        }
    }
}
