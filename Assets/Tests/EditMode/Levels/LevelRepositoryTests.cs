using System.Linq;
using NUnit.Framework;

namespace MazeRoller3D.Levels.Tests
{
    [TestFixture]
    public class LevelRepositoryTests
    {
        [TestCase(0, 8)]
        [TestCase(9, 8)]
        [TestCase(10, 10)]
        [TestCase(19, 10)]
        [TestCase(20, 12)]
        [TestCase(59, 18)]
        [TestCase(60, 20)]
        [TestCase(69, 20)]
        public void GetLevel_SizeMatchesDifficultyCurve(int levelIndex, int expectedSize)
        {
            var level = LevelRepository.GetLevel(levelIndex);

            Assert.AreEqual(expectedSize, level.Width);
            Assert.AreEqual(expectedSize, level.Height);
        }

        [Test]
        public void GetLevel_NeverExceedsMaxSizeEvenForHighTiers()
        {
            var last = LevelRepository.GetLevel(LevelRepository.TotalLevels - 1);
            Assert.LessOrEqual(last.Width, 20);
            Assert.LessOrEqual(last.Height, 20);
        }

        [Test]
        public void GetLevel_OutOfRange_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => LevelRepository.GetLevel(-1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => LevelRepository.GetLevel(LevelRepository.TotalLevels));
        }

        [Test]
        public void GetAllLevels_ReturnsExactlyTotalLevelsCount()
        {
            Assert.AreEqual(LevelRepository.TotalLevels, LevelRepository.GetAllLevels().Count());
        }

        [Test]
        public void GetAllLevels_SeedsAreAllDistinct()
        {
            var seeds = LevelRepository.GetAllLevels().Select(l => l.Seed).ToList();
            Assert.AreEqual(seeds.Count, seeds.Distinct().Count(), "Expected every level to have a distinct seed.");
        }
    }
}
