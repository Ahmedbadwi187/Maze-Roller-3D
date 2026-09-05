using System.Linq;
using RollAndEscape.MazeGeneration;
using NUnit.Framework;

namespace RollAndEscape.Levels.Tests
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

        /// <summary>
        /// Regression test for a real bug found via device testing: System.Random produces
        /// correlated sequences for nearby seed values, so naively using sequential level
        /// indices as seeds (levelIndex + 1) made adjacent levels' mazes look near-identical
        /// near the entrance. GetAllLevels_SeedsAreAllDistinct alone doesn't catch this - two
        /// seeds can be numerically distinct yet still produce near-identical mazes. This
        /// actually builds consecutive levels' mazes and checks a substantial fraction of
        /// cells differ, not just that the seed integers themselves differ.
        /// </summary>
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(10)]
        [TestCase(35)]
        public void ConsecutiveLevels_ProduceSubstantiallyDifferentMazes(int levelIndex)
        {
            var generator = new RecursiveBacktrackerMazeGenerator();
            var level = LevelRepository.GetLevel(levelIndex);
            var nextLevel = LevelRepository.GetLevel(levelIndex + 1);

            var maze = generator.Generate(MazeGenerationSettings.Default(level.Width, level.Height, level.Seed));
            var nextMaze = generator.Generate(MazeGenerationSettings.Default(nextLevel.Width, nextLevel.Height, nextLevel.Seed));

            int sameWallLayoutCells = 0;
            int totalCells = 0;
            foreach (var (column, row) in maze.AllCoordinates())
            {
                totalCells++;
                if (maze.CellAt(column, row).Walls == nextMaze.CellAt(column, row).Walls) sameWallLayoutCells++;
            }

            double sameFraction = (double)sameWallLayoutCells / totalCells;
            Assert.Less(sameFraction, 0.6,
                $"Levels {levelIndex + 1} and {levelIndex + 2} (seeds {level.Seed}, {nextLevel.Seed}) matched on " +
                $"{sameFraction:P0} of cells - too similar to read as different mazes to a player.");
        }

        /// <summary>
        /// Exhaustive check across every actual level in the game (not just a handful of
        /// sample seeds/sizes) - real device testing reported "some levels" having a ball that
        /// can't reach the exit. Builds and checks all 100 real LevelRepository entries, not
        /// just abstract generator inputs, so this can't miss something specific to the actual
        /// level list (a bad size/seed combination, an entrance with zero open sides, etc.).
        /// </summary>
        [Test]
        public void EveryActualLevel_IsFullyConnectedWithASolutionPath()
        {
            var generator = new RecursiveBacktrackerMazeGenerator();

            foreach (var level in LevelRepository.GetAllLevels())
            {
                var maze = generator.Generate(MazeGenerationSettings.Default(level.Width, level.Height, level.Seed));

                Assert.IsTrue(maze.IsFullyConnected(),
                    $"Level {level.LevelIndex + 1} ({level.Width}x{level.Height}, seed {level.Seed}) has an unreachable cell.");

                var entranceOpenSides = maze.CellAt(maze.Entrance.Column, maze.Entrance.Row).OpenSideCount();
                Assert.GreaterOrEqual(entranceOpenSides, 1,
                    $"Level {level.LevelIndex + 1}'s entrance has zero open sides - the ball could never leave it.");

                var path = maze.FindSolutionPath();
                Assert.IsNotEmpty(path, $"Level {level.LevelIndex + 1} ({level.Width}x{level.Height}, seed {level.Seed}) has no path from entrance to exit.");
                Assert.AreEqual(maze.Entrance, path[0]);
                Assert.AreEqual(maze.Exit, path[path.Count - 1]);
            }
        }
    }
}
