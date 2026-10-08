using TMPro;
using UnityEngine;

namespace PipeMuzzle.UI
{
    public enum UiFontRole { Body, Label, Emphasis, Heading, Title }

    /// <summary>Real font weights shared by serialized and runtime-created UI.</summary>
    public static class UiTypography
    {
        private static readonly TMP_FontAsset[] Fonts = new TMP_FontAsset[5];
        private static readonly string[] Names =
        {
            "SourGummy-Regular", "SourGummy-Medium", "SourGummy-SemiBold",
            "Grandstander-SemiBold", "Grandstander-Bold"
        };

        public static TMP_FontAsset Font(UiFontRole role)
        {
            int index = (int)role;
            if (Fonts[index] == null)
                Fonts[index] = Resources.Load<TMP_FontAsset>("Fonts/" + Names[index]);
            return Fonts[index];
        }

        public static void Apply(TMP_Text text, UiFontRole role = UiFontRole.Body)
        {
            if (text == null) return;
            TMP_FontAsset font = Font(role);
            if (font == null)
            {
                Debug.LogError("Missing bundled typography font for " + role, text);
                return;
            }
            text.font = font;
            // Weight is baked into the TTF. Avoid adding TMP's synthetic bold on top.
            text.fontStyle &= ~FontStyles.Bold;
            text.fontWeight = FontWeight.Regular;
        }
    }
}
