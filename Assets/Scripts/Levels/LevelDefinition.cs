namespace MazeRoller3D.Levels
{
    /// <summary>
    /// Static description of one level: grid size and the seed its maze is generated from.
    /// Deliberately has no dependency on MazeRoller3D.MazeGeneration - it's just the recipe;
    /// whatever loads the Game scene is responsible for actually calling
    /// RecursiveBacktrackerMazeGenerator with these values.
    /// </summary>
    public struct LevelDefinition
    {
        public int LevelIndex;
        public int Width;
        public int Height;
        public int Seed;

        public LevelDefinition(int levelIndex, int width, int height, int seed)
        {
            LevelIndex = levelIndex;
            Width = width;
            Height = height;
            Seed = seed;
        }
    }
}
