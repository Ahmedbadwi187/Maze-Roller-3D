using System;
using System.Collections.Generic;

namespace MazeRoller3D.Levels
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
        // 7 tiers of 10 levels: size reaches the 20x20 cap at tier 6 (levels 60-69) and holds
        // there - a lower TotalLevels would cap out below 20x20 despite MaxSize saying 20.
        public const int TotalLevels = 70;
        private const int BaseSize = 8;
        private const int MaxSize = 20;
        private const int LevelsPerTier = 10;

        public static LevelDefinition GetLevel(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= TotalLevels)
                throw new ArgumentOutOfRangeException(nameof(levelIndex), levelIndex, $"Expected 0..{TotalLevels - 1}.");

            int tier = levelIndex / LevelsPerTier;
            int size = Math.Min(MaxSize, BaseSize + 2 * tier);
            int seed = levelIndex + 1; // arbitrary but deterministic and distinct per level

            return new LevelDefinition(levelIndex, size, size, seed);
        }

        public static IEnumerable<LevelDefinition> GetAllLevels()
        {
            for (int i = 0; i < TotalLevels; i++) yield return GetLevel(i);
        }
    }
}
