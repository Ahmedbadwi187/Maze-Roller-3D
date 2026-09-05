using NUnit.Framework;

namespace RollAndEscape.Levels.Tests
{
    [TestFixture]
    public class StarCalculatorTests
    {
        [Test]
        public void VeryFastCompletion_Earns3Stars()
        {
            Assert.AreEqual(3, StarCalculator.CalculateStars(8, 8, elapsedSeconds: 1f));
        }

        [Test]
        public void ExactlyAtThreeStarThreshold_StillEarns3Stars()
        {
            // 8x8 = 64 cells * 1.2s/cell = 76.8s threshold.
            Assert.AreEqual(3, StarCalculator.CalculateStars(8, 8, elapsedSeconds: 76.8f));
        }

        [Test]
        public void JustOverThreeStarThreshold_Earns2Stars()
        {
            Assert.AreEqual(2, StarCalculator.CalculateStars(8, 8, elapsedSeconds: 76.9f));
        }

        [Test]
        public void WayOverThreshold_StillEarnsAtLeast1Star()
        {
            Assert.AreEqual(1, StarCalculator.CalculateStars(8, 8, elapsedSeconds: 10000f));
        }

        [Test]
        public void LargerMaze_AllowsProportionallyMoreTimeForThreeStars()
        {
            // Same elapsed time; a 20x20 maze's threshold is far higher than 8x8's, so a time
            // that only earns 1-2 stars on the small maze should still earn 3 on the large one.
            const float elapsed = 200f;
            int smallMazeStars = StarCalculator.CalculateStars(8, 8, elapsed);
            int largeMazeStars = StarCalculator.CalculateStars(20, 20, elapsed);

            Assert.Less(smallMazeStars, largeMazeStars);
        }
    }
}
