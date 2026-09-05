using RollAndEscape.Gameplay;
using RollAndEscape.Levels;

namespace RollAndEscape.UI
{
    /// <summary>
    /// Shared by LevelFlowController and PauseUI, both of which need to reload the Game scene
    /// for the *same* level currently being played (Restart), not a fresh/next one.
    /// </summary>
    public static class LevelSessionHelper
    {
        /// <summary>
        /// Re-arms LevelSessionContext for the level currently being played, before reloading
        /// the Game scene. LevelSessionContext.HasSelectedLevel is a one-shot flag - MazeView3D
        /// already consumed it when this level first loaded, so simply reloading the scene
        /// without this would make MazeView3D fall back to its own hardcoded preview maze
        /// instead of the real level in progress - a real bug found via device testing
        /// ("Restart returns to a different level than the one being played"). A no-op when
        /// there's no known current level (e.g. testing Game.unity directly in-editor).
        /// </summary>
        public static void RearmCurrentLevel()
        {
            if (LevelSessionContext.CurrentLevelIndex < 0) return;

            var current = LevelRepository.GetLevel(LevelSessionContext.CurrentLevelIndex);
            LevelSessionContext.SelectLevel(current.LevelIndex, current.Width, current.Height, current.Seed);
        }
    }
}
