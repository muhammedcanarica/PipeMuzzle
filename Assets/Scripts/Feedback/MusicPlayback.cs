using UnityEngine;

namespace PipeMuzzle.Feedback
{
    /// <summary>Two music voices owned by GameFeedback; no singleton, objects or coroutines.</summary>
    public sealed class MusicPlayback
    {
        private const float FadeDuration = 1f;
        private readonly AudioSource[] voices;
        private readonly float[] gains = new float[2];
        private AudioClip desired;
        private int active = -1, incoming = -1, outgoing = -1;
        private float elapsed, volume;
        public MusicPlayback(AudioSource first, AudioSource second)
        {
            voices = new[] { first, second };
            foreach (AudioSource voice in voices)
            {
                voice.playOnAwake = false; voice.spatialBlend = 0f;
                voice.loop = true; voice.pitch = 1f; voice.volume = 0f;
            }
        }
        public void Request(AudioClip clip)
        {
            if (clip == null || clip == desired) return;
            desired = clip;
            // A third track replaces only the pending request. Finish the current
            // smooth mix before reusing a silent voice, avoiding an audible hard cut.
            if (incoming < 0 && (active < 0 || voices[active].clip != desired)) BeginFade();
        }
        private void BeginFade()
        {
            outgoing = active;
            incoming = active == 0 ? 1 : 0;
            AudioSource next = voices[incoming];
            next.clip = desired; next.volume = 0f;
            if (Application.isPlaying) next.Play();
            elapsed = 0f;
        }
        public void Advance(float deltaTime)
        {
            if (incoming < 0 || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            float remaining = Mathf.Max(0f, deltaTime - (FadeDuration - elapsed));
            elapsed = Mathf.Min(FadeDuration, elapsed + deltaTime);
            float blend = Mathf.SmoothStep(0f, 1f, elapsed / FadeDuration);
            gains[incoming] = blend;
            if (outgoing >= 0) gains[outgoing] = 1f - blend;
            ApplyVolume();
            if (elapsed < FadeDuration) return;
            if (outgoing >= 0)
            {
                voices[outgoing].Stop(); voices[outgoing].clip = null; gains[outgoing] = 0f;
            }
            active = incoming; incoming = outgoing = -1;
            if (voices[active].clip != desired)
            {
                BeginFade();
                if (remaining > 0f) Advance(remaining);
            }
        }
        public void SetVolume(float value)
        {
            volume = float.IsNaN(value) || float.IsInfinity(value) ? .4f : Mathf.Clamp01(value);
            ApplyVolume();
        }
        private void ApplyVolume()
        {
            for (int i = 0; i < voices.Length; i++) voices[i].volume = volume * gains[i];
        }
        public void Stop()
        {
            foreach (AudioSource voice in voices) { voice.Stop(); voice.clip = null; voice.volume = 0f; }
            gains[0] = gains[1] = 0f;
            desired = null; active = incoming = outgoing = -1;
        }
    }
}
