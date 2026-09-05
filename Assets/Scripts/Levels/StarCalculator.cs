using System;

namespace RollAndEscape.Levels
{
    /// <summary>
    /// Converts a completion time into a 1-5 star rating. Par scales with maze size (the grid's
    /// longer side, so non-square levels still get a sensible par) so bigger/harder mazes get
    /// proportionally more time to earn top stars, rather than one fixed time working equally
    /// for an 8x8 and a 20x20 maze. Completing a level at all always earns at least 1 star.
    /// Pure C#, no Unity dependency, so it's testable outside Unity entirely.
    /// </summary>
    public static class StarCalculator
    {
        private const float SecondsPerCellForPar = 1.8f;
        private const float FourStarMultiplier = 1.4f;
        private const float ThreeStarMultiplier = 1.9f;
        private const float TwoStarMultiplier = 2.6f;

        public static int CalculateStars(int width, int height, float elapsedSeconds)
        {
            int gridSize = Math.Max(width, height);
            float par = gridSize * SecondsPerCellForPar;

            if (elapsedSeconds <= par) return 5;
            if (elapsedSeconds <= par * FourStarMultiplier) return 4;
            if (elapsedSeconds <= par * ThreeStarMultiplier) return 3;
            if (elapsedSeconds <= par * TwoStarMultiplier) return 2;
            return 1;
        }
    }
}
