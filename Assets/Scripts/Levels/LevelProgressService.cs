using RollAndEscape.Persistence;
using UnityEngine;

namespace RollAndEscape.Levels
{
    /// <summary>
    /// Reads/writes per-level completion, stars, and best time via <see cref="SaveSystem"/>.
    /// Level 0 is always unlocked; completing level N unlocks level N+1 - a simple linear
    /// progression, no branching paths. Loads once at construction and saves immediately on
    /// every write (levels complete rarely enough that this isn't a perf concern).
    /// </summary>
    public class LevelProgressService
    {
        private readonly GameSaveData _data;

        public LevelProgressService() : this(SaveSystem.Load<GameSaveData>()) { }

        /// <summary>Takes a shared GameSaveData instance (see GameServices) rather than
        /// loading its own copy, so a change made here and a change made through
        /// SettingsService in the same session don't clobber each other on save.</summary>
        public LevelProgressService(GameSaveData data)
        {
            _data = data;
        }

        public bool IsUnlocked(int levelIndex)
        {
            if (levelIndex <= 0) return true;
            return GetEntry(levelIndex - 1).Completed;
        }

        public bool IsCompleted(int levelIndex) => GetEntry(levelIndex).Completed;
        public int GetStars(int levelIndex) => GetEntry(levelIndex).Stars;

        /// <summary>0 means "never completed" - callers should check IsCompleted first.</summary>
        public float GetBestTimeSeconds(int levelIndex) => GetEntry(levelIndex).BestTimeSeconds;

        /// <summary>Records a completion, keeping the best (highest) star count and best
        /// (lowest) time seen across attempts, then persists immediately.</summary>
        public void RecordCompletion(int levelIndex, int stars, float timeSeconds)
        {
            var entry = GetEntry(levelIndex);
            bool isFirstCompletion = !entry.Completed;

            entry.Completed = true;
            entry.Stars = Mathf.Max(entry.Stars, stars);
            entry.BestTimeSeconds = isFirstCompletion ? timeSeconds : Mathf.Min(entry.BestTimeSeconds, timeSeconds);

            _data.LevelProgress[levelIndex] = entry;
            SaveSystem.Save(_data);
        }

        private LevelProgressEntry GetEntry(int levelIndex) =>
            _data.LevelProgress.TryGetValue(levelIndex, out var entry) ? entry : default;
    }
}
