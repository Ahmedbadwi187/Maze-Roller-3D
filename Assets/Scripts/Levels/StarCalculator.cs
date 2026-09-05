namespace RollAndEscape.Levels
{
    /// <summary>
    /// Converts a completion time into a 1-3 star rating. Thresholds scale with maze size
    /// (cellCount = width*height) so bigger/harder mazes get proportionally more time to earn
    /// 3 stars, rather than one fixed time working equally for an 8x8 and a 20x20 maze.
    /// Completing a level at all always earns at least 1 star. Pure C#, no Unity dependency.
    /// </summary>
    public static class StarCalculator
    {
        private const float SecondsPerCellForThreeStars = 1.2f;
        private const float TwoStarMultiplier = 1.75f;

        public static int CalculateStars(int width, int height, float elapsedSeconds)
        {
            int cellCount = width * height;
            float threeStarThreshold = cellCount * SecondsPerCellForThreeStars;
            float twoStarThreshold = threeStarThreshold * TwoStarMultiplier;

            if (elapsedSeconds <= threeStarThreshold) return 3;
            if (elapsedSeconds <= twoStarThreshold) return 2;
            return 1;
        }
    }
}
