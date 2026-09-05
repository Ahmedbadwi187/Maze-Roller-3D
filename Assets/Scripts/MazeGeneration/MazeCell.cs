using System;

namespace RollAndEscape.MazeGeneration
{
    /// <summary>
    /// Cardinal directions a maze cell can have a wall on. Deliberately a [Flags] enum so a
    /// cell's wall state is a single bitmask (e.g. "walls on North and East").
    /// </summary>
    [Flags]
    public enum WallSide
    {
        None = 0,
        North = 1 << 0,
        East = 1 << 1,
        South = 1 << 2,
        West = 1 << 3,
        All = North | East | South | West
    }

    /// <summary>
    /// One grid cell in a maze. Plain data holder - no Unity dependency - so it can be
    /// constructed and asserted on directly from edit-mode unit tests.
    /// </summary>
    public struct MazeCell
    {
        public int Column;
        public int Row;
        public WallSide Walls;
        public bool Visited;

        public MazeCell(int column, int row)
        {
            Column = column;
            Row = row;
            Walls = WallSide.All;
            Visited = false;
        }

        public bool HasWall(WallSide side) => (Walls & side) == side;

        public void RemoveWall(WallSide side) => Walls &= ~side;

        /// <summary>Number of open sides (i.e. missing walls) on this cell.</summary>
        public int OpenSideCount()
        {
            int count = 0;
            foreach (WallSide side in MazeDirections.All)
            {
                if (!HasWall(side)) count++;
            }
            return count;
        }
    }

    /// <summary>Shared helpers for reasoning about the four cardinal directions.</summary>
    public static class MazeDirections
    {
        public static readonly WallSide[] All = { WallSide.North, WallSide.East, WallSide.South, WallSide.West };

        public static WallSide Opposite(WallSide side)
        {
            switch (side)
            {
                case WallSide.North: return WallSide.South;
                case WallSide.South: return WallSide.North;
                case WallSide.East: return WallSide.West;
                case WallSide.West: return WallSide.East;
                default: throw new ArgumentOutOfRangeException(nameof(side), side, "Expected a single cardinal direction.");
            }
        }

        /// <summary>Column/row delta for stepping one cell in the given direction.</summary>
        public static (int dc, int dr) Delta(WallSide side)
        {
            switch (side)
            {
                case WallSide.North: return (0, 1);
                case WallSide.South: return (0, -1);
                case WallSide.East: return (1, 0);
                case WallSide.West: return (-1, 0);
                default: throw new ArgumentOutOfRangeException(nameof(side), side, "Expected a single cardinal direction.");
            }
        }
    }
}
