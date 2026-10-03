using UnityEngine;

namespace PipeMuzzle.Feedback
{
    /// <summary>Creates soft mono PCM defaults. Call once and retain the resulting clips.</summary>
    public static class ProceduralFeedbackClips
    {
        private const int SampleRate = 44100;
        private const float TwoPi = 2f * Mathf.PI;

        public static AudioClip CreatePipeRotate()
        {
            const float duration = 0.065f;
            float[] samples = new float[Mathf.RoundToInt(SampleRate * duration)];
            for (int i = 0; i < samples.Length; i++)
            {
                float time = i / (float)SampleRate;
                // A descending pitch with a quiet harmonic gives a rounded wooden tick.
                float phase = TwoPi * (760f * time - 3000f * time * time);
                float tone = 0.8f * Mathf.Sin(phase) + 0.2f * Mathf.Sin(2f * phase);
                samples[i] = 0.22f * tone * Envelope(time, duration, 0.003f, 0.015f, 48f);
            }
            return CreateClip("Pipe rotate (procedural)", samples);
        }

        public static AudioClip CreateLevelComplete()
        {
            const float duration = 0.48f;
            const float noteDuration = 0.28f;
            float[] samples = new float[Mathf.RoundToInt(SampleRate * duration)];
            for (int i = 0; i < samples.Length; i++)
            {
                float time = i / (float)SampleRate;
                samples[i] = ChimeNote(time, 523.25f, noteDuration)
                    + ChimeNote(time - 0.10f, 659.25f, noteDuration)
                    + ChimeNote(time - 0.20f, 783.99f, noteDuration);
            }
            return CreateClip("Level complete (procedural)", samples);
        }

        public static AudioClip CreateFlowClip(string worldId, float duration)
        {
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
                throw new System.ArgumentOutOfRangeException(nameof(duration));
            duration = Mathf.Clamp(duration, .05f, 2f);
            bool bamboo = worldId == "BambooWorkshop";
            bool moon = worldId == "MoonShrine";
            float frequency = bamboo ? 430f : moon ? 880f : 650f;
            float noiseLevel = bamboo ? .075f : moon ? .035f : .055f;
            float[] samples = new float[Mathf.RoundToInt(SampleRate * duration)];
            uint noiseState = 317u;
            float filteredNoise = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float time = i / (float)SampleRate;
                noiseState = unchecked(noiseState * 1664525u + 1013904223u);
                float noise = (noiseState >> 8) / 8388607.5f - 1f;
                filteredNoise = Mathf.Lerp(filteredNoise, noise, .045f);
                // Rounded droplets over low-pass water noise, with a faint airy Moon overtone.
                float droplets = Mathf.Pow(.5f + .5f * Mathf.Sin(TwoPi * (bamboo ? 7f : 9f) * time), 4f);
                float phase = TwoPi * (frequency * time + 8f * Mathf.Sin(TwoPi * 2f * time));
                float water = .045f * Mathf.Sin(phase) * droplets + noiseLevel * filteredNoise;
                if (moon) water += .012f * Mathf.Sin(TwoPi * 1320f * time);
                samples[i] = water * Envelope(time, duration, .04f, .07f, 0f);
            }
            return CreateClip("Flow " + worldId + " (procedural)", samples);
        }

        public static AudioClip CreateTargetReachedClip(string worldId)
        {
            const float duration = .08f;
            float frequency = worldId == "BambooWorkshop" ? 700f : worldId == "MoonShrine" ? 1100f : 900f;
            float[] samples = new float[Mathf.RoundToInt(SampleRate * duration)];
            for (int i = 0; i < samples.Length; i++)
            {
                float time = i / (float)SampleRate;
                float phase = TwoPi * (frequency * time - 900f * time * time);
                float drop = .10f * Mathf.Sin(phase) + .012f * Mathf.Sin(2f * phase);
                samples[i] = drop * Envelope(time, duration, .004f, .025f, 18f);
            }
            return CreateClip("Target reached " + worldId + " (procedural)", samples);
        }

        private static float ChimeNote(float time, float frequency, float duration)
        {
            if (time < 0f || time >= duration) return 0f;
            float phase = TwoPi * frequency * time;
            float tone = 0.85f * Mathf.Sin(phase) + 0.15f * Mathf.Sin(2f * phase);
            return 0.12f * tone * Envelope(time, duration, 0.012f, 0.14f, 7f);
        }

        private static float Envelope(float time, float duration, float attack, float release, float decay)
        {
            float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / attack));
            float fadeOut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((duration - time) / release));
            return fadeIn * fadeOut * Mathf.Exp(-decay * time);
        }

        private static AudioClip CreateClip(string name, float[] samples)
        {
            samples[0] = 0f;
            samples[samples.Length - 1] = 0f;
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
