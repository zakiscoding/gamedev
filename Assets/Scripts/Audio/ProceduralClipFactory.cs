using UnityEngine;

namespace Cmis3150.Fps.Audio
{
    /// <summary>Generates simple beep/noise AudioClips at runtime so SFX wiring is audible without importing WAV files.</summary>
    public static class ProceduralClipFactory
    {
        public static AudioClip Beep(string name, float frequency, float duration, float volume = 0.4f)
        {
            int sampleRate = 22050;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float env = 1f - (t / duration);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * env;
            }

            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip NoiseBurst(string name, float duration, float volume = 0.35f)
        {
            int sampleRate = 22050;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float env = 1f - (t / duration);
                data[i] = (Random.value * 2f - 1f) * volume * env;
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
