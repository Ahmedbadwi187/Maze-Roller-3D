using UnityEngine;

namespace MazeRoller3D.Gameplay
{
    /// <summary>
    /// Synthesizes simple placeholder sound effects at runtime - no audio asset files needed.
    /// Milestone 9 polish item ("sound effects") without waiting on real SFX assets; swapping
    /// in an authored clip later is a one-line change wherever this is called.
    /// </summary>
    public static class ProceduralSfx
    {
        /// <summary>A short two-note ascending chime ("ding-ding!") for level completion.</summary>
        public static AudioClip CreateSuccessChime()
        {
            const int sampleRate = 44100;
            const float duration = 0.35f;
            const float note1 = 880f;      // A5
            const float note2 = 1108.73f;  // C#6 - a bright major third above note1

            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];
            int splitSample = sampleCount / 2;

            for (int i = 0; i < sampleCount; i++)
            {
                bool secondNote = i >= splitSample;
                float freq = secondNote ? note2 : note1;
                int localIndex = secondNote ? i - splitSample : i;
                float t = localIndex / (float)sampleRate;
                float envelope = Mathf.Exp(-6f * t); // quick decay so each note doesn't cut off abruptly
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.5f;
            }

            var clip = AudioClip.Create("SuccessChime", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
