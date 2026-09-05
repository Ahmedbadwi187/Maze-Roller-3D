namespace RollAndEscape.Gameplay
{
    /// <summary>
    /// Carries which level to load across the LevelSelect -> Game scene transition. Plain
    /// width/height/seed rather than a RollAndEscape.Levels.LevelDefinition reference on
    /// purpose - this assembly has no dependency on the Levels assembly, so gameplay code
    /// never needs to know what a "level" is, just what maze to build.
    ///
    /// Static rather than a scene object because it must survive a scene load - the simplest
    /// version of the "service" pattern the project structure calls for; nothing stops this
    /// from being folded into a fuller GameServices locator later without changing callers.
    /// </summary>
    public static class LevelSessionContext
    {
        /// <summary>True until MazeView3D.Start consumes it for the maze-building fields
        /// (width/height/seed) - CurrentLevelIndex/Width/Height stay valid for the rest of the
        /// scene's lifetime so LevelFlowController can record completion against the right
        /// level later, well after consumption.</summary>
        public static bool HasSelectedLevel { get; private set; }

        public static int CurrentLevelIndex { get; private set; } = -1;
        public static int CurrentWidth { get; private set; }
        public static int CurrentHeight { get; private set; }
        private static int _seed;

        public static void SelectLevel(int levelIndex, int width, int height, int seed)
        {
            CurrentLevelIndex = levelIndex;
            CurrentWidth = width;
            CurrentHeight = height;
            _seed = seed;
            HasSelectedLevel = true;
        }

        /// <summary>Reads and clears the pending-selection flag - call once, from the Game
        /// scene's Start. CurrentLevelIndex/Width/Height remain readable afterward.</summary>
        public static (int Width, int Height, int Seed) ConsumeSelectedLevel()
        {
            HasSelectedLevel = false;
            return (CurrentWidth, CurrentHeight, _seed);
        }
    }
}
