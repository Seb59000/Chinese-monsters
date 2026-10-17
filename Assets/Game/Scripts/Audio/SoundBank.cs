using UnityEngine;

namespace Game
{
    /// <summary>Synthesised placeholder sounds, so the slice has audio without any asset.</summary>
    public static class SoundBank
    {
        const int Rate = 44100;
        static AudioClip gunshot, hit, screech, squelch, reload, radio, rain, drone, whoosh, impact, click;

        public static AudioClip Gunshot => gunshot ??= Make("Gunshot", 0.45f, (t, r) =>
            Noise(r) * Mathf.Exp(-t * 18f) * 0.9f + Mathf.Sin(t * 2f * Mathf.PI * 70f) * Mathf.Exp(-t * 12f) * 0.6f);

        public static AudioClip Hit => hit ??= Make("Hit", 0.15f, (t, r) =>
            Mathf.Sin(t * 2f * Mathf.PI * (180f - t * 600f)) * Mathf.Exp(-t * 30f) + Noise(r) * Mathf.Exp(-t * 60f) * 0.3f);

        public static AudioClip Screech => screech ??= Make("Screech", 1.1f, (t, r) =>
        {
            float f = 520f + Mathf.Sin(t * 37f) * 90f + t * 300f;
            float env = Mathf.Clamp01(t * 12f) * Mathf.Exp(-t * 2.2f);
            return (Saw(t * f) * 0.5f + Mathf.Sin(t * 2f * Mathf.PI * f * 1.51f) * 0.3f + Noise(r) * 0.25f) * env;
        });

        public static AudioClip Squelch => squelch ??= Make("Squelch", 0.7f, (t, r) =>
            (Noise(r) * 0.5f + Mathf.Sin(t * 2f * Mathf.PI * (90f + Mathf.Sin(t * 40f) * 40f))) * Mathf.Exp(-t * 4f) * 0.7f);

        public static AudioClip Reload => reload ??= Make("Reload", 0.5f, (t, r) =>
        {
            float a = t < 0.05f ? 1f : 0f, b = t > 0.32f && t < 0.37f ? 1f : 0f;
            return Noise(r) * (a + b) * 0.6f + Mathf.Sin(t * 2f * Mathf.PI * 2400f) * (a + b) * 0.2f;
        });

        public static AudioClip Radio => radio ??= Make("Radio", 0.25f, (t, r) =>
            (Mathf.Sin(t * 2f * Mathf.PI * 1200f) * (t < 0.08f ? 1f : 0f) + Noise(r) * 0.25f) * Mathf.Exp(-t * 6f) * 0.5f);

        public static AudioClip Whoosh => whoosh ??= Make("Whoosh", 0.5f, (t, r) =>
            Noise(r) * Mathf.Sin(t / 0.5f * Mathf.PI) * 0.5f);

        public static AudioClip Impact => impact ??= Make("Impact", 0.9f, (t, r) =>
            (Mathf.Sin(t * 2f * Mathf.PI * (55f - t * 20f)) + Noise(r) * Mathf.Exp(-t * 10f) * 0.6f) * Mathf.Exp(-t * 4f));

        public static AudioClip Click => click ??= Make("Click", 0.06f, (t, r) =>
            Mathf.Sin(t * 2f * Mathf.PI * 1800f) * Mathf.Exp(-t * 80f) * 0.5f);

        /// <summary>Looping rain bed (seamless because it is only noise).</summary>
        public static AudioClip Rain => rain ??= Make("Rain", 4f, (t, r) =>
        {
            float n = Noise(r);
            lowpass = lowpass + (n - lowpass) * 0.15f;
            return lowpass * 0.35f + (r.NextDouble() < 0.0015 ? 0.4f : 0f);
        });

        /// <summary>Dark ambient drone, 8 s loop (frequencies chosen to loop seamlessly).</summary>
        public static AudioClip Drone => drone ??= Make("Drone", 8f, (t, r) =>
        {
            float lfo = 0.6f + 0.4f * Mathf.Sin(t * 2f * Mathf.PI * 0.125f);
            return (Mathf.Sin(t * 2f * Mathf.PI * 55f) * 0.4f + Mathf.Sin(t * 2f * Mathf.PI * 55.5f) * 0.35f
                    + Mathf.Sin(t * 2f * Mathf.PI * 82.5f) * 0.18f * lfo + Mathf.Sin(t * 2f * Mathf.PI * 110.25f) * 0.08f) * 0.5f;
        });

        static float lowpass;

        static float Noise(System.Random r) => (float)(r.NextDouble() * 2.0 - 1.0);
        static float Saw(float x) => 2f * (x - Mathf.Floor(x + 0.5f));

        static AudioClip Make(string name, float seconds, System.Func<float, System.Random, float> fn)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            var rnd = new System.Random(name.GetHashCode());
            lowpass = 0f;
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(fn(i / (float)Rate, rnd), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
