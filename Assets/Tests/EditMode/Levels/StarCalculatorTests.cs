using NUnit.Framework;

namespace RollAndEscape.Levels.Tests
{
    [TestFixture]
    public class StarCalculatorTests
    {
        // 8x8 -> gridSize 8 -> par = 8 * 1.8 = 14.4s.
        private const float Par8x8 = 14.4f;

        [Test]
        public void AtOrUnderPar_Earns5Stars()
        {
            Assert.AreEqual(5, StarCalculator.CalculateStars(8, 8, elapsedSeconds: 1f));
            Assert.AreEqual(5, StarCalculator.CalculateStars(8, 8, elapsedSeconds: Par8x8 - 0.01f));
        }

        [Test]
        public void JustOverPar_Earns4Stars()
        {
            Assert.AreEqual(4, StarCalculator.CalculateStars(8, 8, elapsedSeconds: Par8x8 + 0.1f));
        }

        [Test]
        public void AtFourStarThreshold_StillEarns4Stars()
        {
            // A hair under the exact threshold rather than the exact product - the test and
            // StarCalculator each compute par*1.4 independently, and float multiplication isn't
            // perfectly associative, so the two can round to adjacent representable values;
            // asserting exact equality at the boundary was flaky on that last bit.
            Assert.AreEqual(4, StarCalculator.CalculateStars(8, 8, elapsedSeconds: Par8x8 * 1.4f - 0.01f));
        }

        [Test]
        public void JustOverFourStarThreshold_Earns3Stars()
        {
            Assert.AreEqual(3, StarCalculator.CalculateStars(8, 8, elapsedSeconds: Par8x8 * 1.4f + 0.1f));
        }

        [Test]
        public void AtThreeStarThreshold_StillEarns3Stars()
        {
            Assert.AreEqual(3, StarCalculator.CalculateStars(8, 8, elapsedSeconds: Par8x8 * 1.9f - 0.01f));
        }

        [Test]
        public void JustOverThreeStarThreshold_Earns2Stars()
        {
            Assert.AreEqual(2, StarCalculator.CalculateStars(8, 8, elapsedSeconds: Par8x8 * 1.9f + 0.1f));
        }

        [Test]
        public void AtTwoStarThreshold_StillEarns2Stars()
        {
            Assert.AreEqual(2, StarCalculator.CalculateStars(8, 8, elapsedSeconds: Par8x8 * 2.6f - 0.01f));
        }

        [Test]
        public void JustOverTwoStarThreshold_Earns1Star()
        {
            Assert.AreEqual(1, StarCalculator.CalculateStars(8, 8, elapsedSeconds: Par8x8 * 2.6f + 0.1f));
        }

        [Test]
        public void WayOverThreshold_StillEarnsAtLeast1Star()
        {
            Assert.AreEqual(1, StarCalculator.CalculateStars(8, 8, elapsedSeconds: 10000f));
        }

        [Test]
        public void LargerMaze_AllowsProportionallyMoreTimeForTopStars()
        {
            // Same elapsed time; a 20x20 maze's par is far higher than 8x8's, so a time that
            // only earns 1-2 stars on the small maze should still earn more on the large one.
            const float elapsed = 40f;
            int smallMazeStars = StarCalculator.CalculateStars(8, 8, elapsed);
            int largeMazeStars = StarCalculator.CalculateStars(20, 20, elapsed);

            Assert.Less(smallMazeStars, largeMazeStars);
        }

        [Test]
        public void NonSquareLevel_ParUsesTheLongerSide()
        {
            // A 6x12 level's par should match a 12x12's (both use gridSize=12), not an 8x8's.
            Assert.AreEqual(
                StarCalculator.CalculateStars(12, 12, elapsedSeconds: 30f),
                StarCalculator.CalculateStars(6, 12, elapsedSeconds: 30f));
        }
    }
}
