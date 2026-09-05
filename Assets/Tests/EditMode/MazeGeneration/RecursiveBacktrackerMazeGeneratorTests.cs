using System.Collections.Generic;
using RollAndEscape.MazeGeneration;
using NUnit.Framework;

namespace RollAndEscape.MazeGeneration.Tests
{
    [TestFixture]
    public class RecursiveBacktrackerMazeGeneratorTests
    {
        private RecursiveBacktrackerMazeGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            _generator = new RecursiveBacktrackerMazeGenerator();
        }

        [TestCase(8, 8)]
        [TestCase(12, 12)]
        [TestCase(20, 20)]
        [TestCase(10, 15)]
        [TestCase(1, 1)]
        [TestCase(1, 5)]
        public void Generate_ProducesGridWithRequestedDimensions(int width, int height)
        {
            var maze = _generator.Generate(MazeGenerationSettings.Default(width, height, seed: 1));

            Assert.AreEqual(width, maze.Width);
            Assert.AreEqual(height, maze.Height);

            foreach (var (column, row) in maze.AllCoordinates())
            {
                Assert.IsTrue(maze.InBounds(column, row));
            }
        }

        [TestCase(8, 8)]
        [TestCase(12, 12)]
        [TestCase(20, 20)]
        [TestCase(10, 15)]
        public void Generate_EveryCellIsReachableFromEveryOtherCell(int width, int height)
        {
            var maze = _generator.Generate(MazeGenerationSettings.Default(width, height, seed: 42));

            Assert.IsTrue(maze.IsFullyConnected(),
                $"Maze {width}x{height} has at least one unreachable cell - generator left an incomplete spanning tree.");
        }

        [TestCase(8, 8)]
        [TestCase(20, 20)]
        [TestCase(13, 7)]
        public void Generate_IsAPerfectMaze_ExactlyOnePathBetweenAnyTwoCells(int width, int height)
        {
            // A perfect maze's carved passages form a spanning tree over the grid graph: with N
            // cells there must be exactly N-1 open connections. That is precisely the condition
            // that guarantees a single, unique path between any two cells (no loops, nothing
            // unreachable) - so counting edges is a direct test of the "single main path"
            // requirement, not just an implementation detail.
            var maze = _generator.Generate(MazeGenerationSettings.Default(width, height, seed: 7));

            int openConnections = CountOpenConnections(maze);
            int cellCount = width * height;

            Assert.AreEqual(cellCount - 1, openConnections,
                "Expected exactly (cellCount - 1) open connections for a perfect (loop-free, fully-connected) maze.");
        }

        [TestCase(8, 8)]
        [TestCase(20, 20)]
        public void Generate_SolutionPathExists_FromEntranceToExit(int width, int height)
        {
            var maze = _generator.Generate(MazeGenerationSettings.Default(width, height, seed: 99));

            var path = maze.FindSolutionPath();

            Assert.IsNotEmpty(path, "Expected a walkable path from entrance to exit.");
            Assert.AreEqual(maze.Entrance, path[0]);
            Assert.AreEqual(maze.Exit, path[path.Count - 1]);
            AssertPathIsWalkable(maze, path);
        }

        [Test]
        public void Generate_DefaultEntranceAndExit_AreOppositeCorners()
        {
            var maze = _generator.Generate(MazeGenerationSettings.Default(8, 8, seed: 1));

            Assert.AreEqual((0, 0), maze.Entrance);
            Assert.AreEqual((7, 7), maze.Exit);
        }

        [Test]
        public void Generate_RespectsCustomEntranceAndExit()
        {
            var settings = MazeGenerationSettings.Default(8, 8, seed: 1);
            settings.Entrance = (2, 3);
            settings.Exit = (5, 6);

            var maze = _generator.Generate(settings);

            Assert.AreEqual((2, 3), maze.Entrance);
            Assert.AreEqual((5, 6), maze.Exit);

            var path = maze.FindSolutionPath();
            Assert.IsNotEmpty(path);
            Assert.AreEqual(maze.Entrance, path[0]);
            Assert.AreEqual(maze.Exit, path[path.Count - 1]);
        }

        [Test]
        public void Generate_SameSeed_ProducesIdenticalLayout()
        {
            var mazeA = _generator.Generate(MazeGenerationSettings.Default(12, 12, seed: 12345));
            var mazeB = _generator.Generate(MazeGenerationSettings.Default(12, 12, seed: 12345));

            foreach (var (column, row) in mazeA.AllCoordinates())
            {
                Assert.AreEqual(mazeA.CellAt(column, row).Walls, mazeB.CellAt(column, row).Walls,
                    $"Cell ({column},{row}) differs between two generations using the same seed.");
            }
        }

        [Test]
        public void Generate_DifferentSeeds_ProduceDifferentLayouts()
        {
            var baseline = _generator.Generate(MazeGenerationSettings.Default(12, 12, seed: 1));

            bool foundDifference = false;
            for (int seed = 2; seed <= 10 && !foundDifference; seed++)
            {
                var candidate = _generator.Generate(MazeGenerationSettings.Default(12, 12, seed: seed));
                foreach (var (column, row) in baseline.AllCoordinates())
                {
                    if (baseline.CellAt(column, row).Walls != candidate.CellAt(column, row).Walls)
                    {
                        foundDifference = true;
                        break;
                    }
                }
            }

            Assert.IsTrue(foundDifference, "Expected at least one of 9 alternate seeds to produce a different layout.");
        }

        [Test]
        public void Generate_EveryNonBoundaryWall_HasMatchingWallOnNeighborsSide()
        {
            var maze = _generator.Generate(MazeGenerationSettings.Default(8, 8, seed: 3));

            foreach (var (column, row) in maze.AllCoordinates())
            {
                foreach (var side in MazeDirections.All)
                {
                    var (dc, dr) = MazeDirections.Delta(side);
                    var neighbor = (column + dc, row + dr);
                    if (!maze.InBounds(neighbor.Item1, neighbor.Item2)) continue;

                    bool openFromHere = !maze.CellAt(column, row).HasWall(side);
                    bool openFromNeighbor = maze.IsOpenBetween(neighbor, (column, row));
                    Assert.AreEqual(openFromHere, openFromNeighbor,
                        $"Wall state mismatch between ({column},{row}) and ({neighbor.Item1},{neighbor.Item2}).");
                }
            }
        }

        [TestCase(0, 8)]
        [TestCase(8, 0)]
        [TestCase(-1, 8)]
        public void Generate_ThrowsForNonPositiveDimensions(int width, int height)
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                _generator.Generate(MazeGenerationSettings.Default(width, height, seed: 1)));
        }

        [Test]
        public void RemoveWallBetween_NonAdjacentCells_Throws()
        {
            var maze = new MazeModel(4, 4, (0, 0), (3, 3));
            Assert.Throws<System.ArgumentException>(() => maze.RemoveWallBetween((0, 0), (2, 2)));
        }

        // -- helpers ---------------------------------------------------------------------

        private static int CountOpenConnections(MazeModel maze)
        {
            int count = 0;
            foreach (var (column, row) in maze.AllCoordinates())
            {
                // Count each undirected edge exactly once by only looking "forward".
                if (maze.InBounds(column + 1, row) && maze.IsOpenBetween((column, row), (column + 1, row))) count++;
                if (maze.InBounds(column, row + 1) && maze.IsOpenBetween((column, row), (column, row + 1))) count++;
            }
            return count;
        }

        private static void AssertPathIsWalkable(MazeModel maze, IReadOnlyList<(int Column, int Row)> path)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                Assert.IsTrue(maze.IsOpenBetween(path[i], path[i + 1]),
                    $"Reported path steps through a wall between ({path[i].Column},{path[i].Row}) and ({path[i + 1].Column},{path[i + 1].Row}).");
            }
        }
    }
}
