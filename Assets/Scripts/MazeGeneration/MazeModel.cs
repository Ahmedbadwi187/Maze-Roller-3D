using System;
using System.Collections.Generic;

namespace MazeRoller3D.MazeGeneration
{
    /// <summary>
    /// Immutable-after-construction grid of <see cref="MazeCell"/>s plus the entrance/exit
    /// coordinates. Pure C# - no UnityEngine dependency - so it can be built and inspected
    /// entirely from edit-mode unit tests, and later consumed by a MonoBehaviour that
    /// instantiates the 3D geometry from it.
    /// </summary>
    public sealed class MazeModel
    {
        public int Width { get; }
        public int Height { get; }
        public (int Column, int Row) Entrance { get; }
        public (int Column, int Row) Exit { get; }

        private readonly MazeCell[,] _cells;

        public MazeModel(int width, int height, (int Column, int Row) entrance, (int Column, int Row) exit)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "Maze width must be positive.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "Maze height must be positive.");

            Width = width;
            Height = height;
            Entrance = entrance;
            Exit = exit;

            _cells = new MazeCell[width, height];
            for (int c = 0; c < width; c++)
            {
                for (int r = 0; r < height; r++)
                {
                    _cells[c, r] = new MazeCell(c, r);
                }
            }
        }

        public bool InBounds(int column, int row) => column >= 0 && column < Width && row >= 0 && row < Height;

        public ref MazeCell CellAt(int column, int row)
        {
            if (!InBounds(column, row))
                throw new ArgumentOutOfRangeException($"Cell ({column},{row}) is outside a {Width}x{Height} maze.");
            return ref _cells[column, row];
        }

        /// <summary>Removes the wall between two orthogonally-adjacent cells on both sides.</summary>
        public void RemoveWallBetween((int Column, int Row) a, (int Column, int Row) b)
        {
            int dc = b.Column - a.Column;
            int dr = b.Row - a.Row;

            WallSide sideFromA = (dc, dr) switch
            {
                (0, 1) => WallSide.North,
                (0, -1) => WallSide.South,
                (1, 0) => WallSide.East,
                (-1, 0) => WallSide.West,
                _ => throw new ArgumentException($"Cells ({a.Column},{a.Row}) and ({b.Column},{b.Row}) are not orthogonally adjacent.")
            };

            CellAt(a.Column, a.Row).RemoveWall(sideFromA);
            CellAt(b.Column, b.Row).RemoveWall(MazeDirections.Opposite(sideFromA));
        }

        /// <summary>True if there is no wall between two orthogonally-adjacent cells.</summary>
        public bool IsOpenBetween((int Column, int Row) a, (int Column, int Row) b)
        {
            int dc = b.Column - a.Column;
            int dr = b.Row - a.Row;
            WallSide sideFromA = (dc, dr) switch
            {
                (0, 1) => WallSide.North,
                (0, -1) => WallSide.South,
                (1, 0) => WallSide.East,
                (-1, 0) => WallSide.West,
                _ => throw new ArgumentException($"Cells ({a.Column},{a.Row}) and ({b.Column},{b.Row}) are not orthogonally adjacent.")
            };
            return !CellAt(a.Column, a.Row).HasWall(sideFromA);
        }

        public IEnumerable<(int Column, int Row)> AllCoordinates()
        {
            for (int c = 0; c < Width; c++)
                for (int r = 0; r < Height; r++)
                    yield return (c, r);
        }

        /// <summary>Coordinates orthogonally reachable from <paramref name="from"/> with no wall in between.</summary>
        public IEnumerable<(int Column, int Row)> OpenNeighbors((int Column, int Row) from)
        {
            foreach (WallSide side in MazeDirections.All)
            {
                if (CellAt(from.Column, from.Row).HasWall(side)) continue;
                var (dc, dr) = MazeDirections.Delta(side);
                var neighbor = (from.Column + dc, from.Row + dr);
                if (InBounds(neighbor.Item1, neighbor.Item2)) yield return neighbor;
            }
        }

        /// <summary>
        /// Breadth-first search from <see cref="Entrance"/> to <see cref="Exit"/> through open
        /// passages only. Returns the ordered path (inclusive of both ends), or an empty list
        /// if no path exists. Also used at runtime to drive the "reveal part of the solution"
        /// hint feature.
        /// </summary>
        public IReadOnlyList<(int Column, int Row)> FindSolutionPath()
        {
            return FindPath(Entrance, Exit);
        }

        public IReadOnlyList<(int Column, int Row)> FindPath((int Column, int Row) from, (int Column, int Row) to)
        {
            var cameFrom = new Dictionary<(int, int), (int, int)>();
            var visited = new HashSet<(int, int)> { from };
            var queue = new Queue<(int, int)>();
            queue.Enqueue(from);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == to) return ReconstructPath(cameFrom, from, to);

                foreach (var neighbor in OpenNeighbors(current))
                {
                    if (visited.Contains(neighbor)) continue;
                    visited.Add(neighbor);
                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }

            return Array.Empty<(int Column, int Row)>();
        }

        private static IReadOnlyList<(int Column, int Row)> ReconstructPath(
            Dictionary<(int, int), (int, int)> cameFrom, (int Column, int Row) from, (int Column, int Row) to)
        {
            var path = new List<(int, int)> { to };
            var current = to;
            while (current != from)
            {
                current = cameFrom[current];
                path.Add(current);
            }
            path.Reverse();
            return path;
        }

        /// <summary>True if every cell in the grid is reachable from every other cell.</summary>
        public bool IsFullyConnected()
        {
            if (Width == 0 || Height == 0) return true;

            var start = (0, 0);
            var visited = new HashSet<(int, int)> { start };
            var stack = new Stack<(int, int)>();
            stack.Push(start);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                foreach (var neighbor in OpenNeighbors(current))
                {
                    if (visited.Add(neighbor)) stack.Push(neighbor);
                }
            }

            return visited.Count == Width * Height;
        }
    }
}
