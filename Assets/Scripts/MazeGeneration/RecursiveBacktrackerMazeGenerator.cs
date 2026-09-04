using System;
using System.Collections.Generic;

namespace MazeRoller3D.MazeGeneration
{
    /// <summary>
    /// Settings for one maze generation call. Plain data - safe to construct from tests,
    /// from a level-difficulty table, or from the Unity inspector via a wrapper type.
    /// </summary>
    public struct MazeGenerationSettings
    {
        public int Width;
        public int Height;
        public int Seed;

        /// <summary>
        /// Entrance/exit cells. Defaults to opposite corners ((0,0) and (Width-1,Height-1))
        /// when left as (-1,-1).
        /// </summary>
        public (int Column, int Row) Entrance;
        public (int Column, int Row) Exit;

        /// <summary>
        /// Fraction (0-1) of additional walls to knock down after the perfect maze is carved,
        /// creating loops/shortcuts ("braiding"). 0 = a perfect maze: exactly one path between
        /// any two cells, which is what guarantees single-path solvability. Values above 0 are
        /// an intentional future knob (e.g. easier difficulty tiers) and forfeit that single-path
        /// guarantee, so gameplay code should not assume uniqueness when it's non-zero.
        /// </summary>
        public float ExtraConnectionChance;

        public static MazeGenerationSettings Default(int width, int height, int seed) => new MazeGenerationSettings
        {
            Width = width,
            Height = height,
            Seed = seed,
            Entrance = (-1, -1),
            Exit = (-1, -1),
            ExtraConnectionChance = 0f
        };
    }

    /// <summary>
    /// Generates a perfect maze (every cell reachable, exactly one path between any two cells)
    /// using the recursive-backtracker / randomized depth-first-search algorithm, implemented
    /// iteratively with an explicit stack so generation depth is not bounded by the call stack
    /// even for the largest supported grids (~20x20).
    ///
    /// Deliberately has zero UnityEngine dependencies so it is fully covered by fast edit-mode
    /// unit tests; a separate MonoBehaviour is responsible for turning the resulting
    /// <see cref="MazeModel"/> into 3D geometry.
    /// </summary>
    public sealed class RecursiveBacktrackerMazeGenerator
    {
        public MazeModel Generate(MazeGenerationSettings settings)
        {
            if (settings.Width <= 0) throw new ArgumentOutOfRangeException(nameof(settings.Width));
            if (settings.Height <= 0) throw new ArgumentOutOfRangeException(nameof(settings.Height));

            var entrance = settings.Entrance.Column >= 0 ? settings.Entrance : (0, 0);
            var exit = settings.Exit.Column >= 0 ? settings.Exit : (settings.Width - 1, settings.Height - 1);

            var maze = new MazeModel(settings.Width, settings.Height, entrance, exit);
            var random = new Random(settings.Seed);

            Carve(maze, random, entrance);

            if (settings.ExtraConnectionChance > 0f)
            {
                AddExtraConnections(maze, random, settings.ExtraConnectionChance);
            }

            return maze;
        }

        /// <summary>Randomized DFS ("recursive backtracker") carve, iterative via an explicit stack.</summary>
        private static void Carve(MazeModel maze, Random random, (int Column, int Row) start)
        {
            var stack = new Stack<(int Column, int Row)>();
            maze.CellAt(start.Column, start.Row).Visited = true;
            stack.Push(start);

            while (stack.Count > 0)
            {
                var current = stack.Peek();
                var unvisitedNeighbors = UnvisitedNeighbors(maze, current);

                if (unvisitedNeighbors.Count == 0)
                {
                    stack.Pop();
                    continue;
                }

                var next = unvisitedNeighbors[random.Next(unvisitedNeighbors.Count)];
                maze.RemoveWallBetween(current, next);
                maze.CellAt(next.Column, next.Row).Visited = true;
                stack.Push(next);
            }
        }

        private static List<(int Column, int Row)> UnvisitedNeighbors(MazeModel maze, (int Column, int Row) cell)
        {
            var result = new List<(int Column, int Row)>(4);
            foreach (var side in MazeDirections.All)
            {
                var (dc, dr) = MazeDirections.Delta(side);
                var neighbor = (cell.Column + dc, cell.Row + dr);
                if (!maze.InBounds(neighbor.Item1, neighbor.Item2)) continue;
                if (maze.CellAt(neighbor.Item1, neighbor.Item2).Visited) continue;
                result.Add(neighbor);
            }
            return result;
        }

        /// <summary>
        /// Optional post-pass (braiding): for a chance-weighted subset of remaining walls
        /// between adjacent cells, knock the wall down too, creating shortcuts/loops. Used to
        /// soften a maze for easier difficulty tiers; not used at the default (perfect-maze)
        /// settings the generator ships with.
        /// </summary>
        private static void AddExtraConnections(MazeModel maze, Random random, float chance)
        {
            foreach (var cell in maze.AllCoordinates())
            {
                // Only ever consider East/North here to avoid evaluating each wall twice.
                foreach (var side in new[] { WallSide.North, WallSide.East })
                {
                    var (dc, dr) = MazeDirections.Delta(side);
                    var neighbor = (cell.Column + dc, cell.Row + dr);
                    if (!maze.InBounds(neighbor.Item1, neighbor.Item2)) continue;
                    if (maze.IsOpenBetween(cell, neighbor)) continue;
                    if (random.NextDouble() < chance)
                    {
                        maze.RemoveWallBetween(cell, neighbor);
                    }
                }
            }
        }
    }
}
