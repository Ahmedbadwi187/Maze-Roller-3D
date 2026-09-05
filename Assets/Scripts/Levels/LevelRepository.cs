using System;
using System.Collections.Generic;

namespace RollAndEscape.Levels
{
    /// <summary>
    /// Generates the level list: procedural mazes only need a size and a seed (see
    /// LevelDefinition), so "the level list" is really just a difficulty curve formula rather
    /// than authored data. Grid size starts at 8x8 and grows by 2 every 10 levels, capped at
    /// 20x20, matching the spec's "8x8 scaling up to ~20x20, every 10 levels" difficulty curve.
    /// Pure C#, no Unity dependency - trivially unit-testable.
    /// </summary>
    public static class LevelRepository
    {
        // 100 levels: size still reaches the 20x20 cap at tier 6 (level 60) and holds there for
        // the remaining 40 levels (60-99) - still meaningfully harder for longer since a 20x20
        // maze has a much longer solution path than an 8x8 one, just without growing further.
        public const int TotalLevels = 100;
        private const int BaseSize = 8;
        private const int MaxSize = 20;
        private const int LevelsPerTier = 10;

        public static LevelDefinition GetLevel(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= TotalLevels)
                throw new ArgumentOutOfRangeException(nameof(levelIndex), levelIndex, $"Expected 0..{TotalLevels - 1}.");

            int tier = levelIndex / LevelsPerTier;
            int size = Math.Min(MaxSize, BaseSize + 2 * tier);
            int seed = ScrambleSeed(levelIndex);

            return new LevelDefinition(levelIndex, size, size, seed);
        }

        public static IEnumerable<LevelDefinition> GetAllLevels()
        {
            for (int i = 0; i < TotalLevels; i++) yield return GetLevel(i);
        }

        /// <summary>
        /// Turns a sequential level index into a well-mixed seed for System.Random.
        /// System.Random is known to produce correlated sequences for nearby seed values
        /// (especially for the first several draws) - using raw sequential seeds
        /// (levelIndex + 1) meant adjacent levels' mazes looked near-identical near the
        /// entrance, which is exactly what real device testing caught. This is a standard
        /// integer bit-mixing hash (Thomas Wang style) that decorrelates adjacent inputs.
        /// </summary>
        private static int ScrambleSeed(int levelIndex)
        {
            unchecked
            {
                uint x = (uint)(levelIndex + 1);
                x = ((x >> 16) ^ x) * 0x45d9f3bu;
                x = ((x >> 16) ^ x) * 0x45d9f3bu;
                x = (x >> 16) ^ x;
                return (int)(x & 0x7FFFFFFF);
            }
        }
    }
}
