using RollAndEscape.Levels;
using RollAndEscape.MazeGeneration;
using UnityEditor;
using UnityEngine;

namespace RollAndEscape.EditorTools
{
    public static class MazeDataDiagnostic
    {
        [MenuItem("Roll & Escape/Diagnostics - Check Level 1 Solvability")]
        public static void CheckLevel1()
        {
            var level = LevelRepository.GetLevel(0);
            var generator = new RecursiveBacktrackerMazeGenerator();
            var model = generator.Generate(MazeGenerationSettings.Default(level.Width, level.Height, level.Seed));

            var entranceCell = model.CellAt(model.Entrance.Column, model.Entrance.Row);
            Debug.Log($"[Diag] Level 1: size={level.Width}x{level.Height}, seed={level.Seed}, " +
                      $"Entrance={model.Entrance}, Exit={model.Exit}, Entrance walls bitmask={entranceCell.Walls}, " +
                      $"Entrance open sides count={entranceCell.OpenSideCount()}, FullyConnected={model.IsFullyConnected()}");

            var path = model.FindSolutionPath();
            Debug.Log($"[Diag] Solution path length={path.Count}, first 5 steps={string.Join(" -> ", System.Linq.Enumerable.Take(path, 5))}");

            // Print every wall state for the entrance cell and its immediate neighbors, to spot
            // any rendering-layer mismatch against this raw data.
            foreach (var side in MazeDirections.All)
            {
                bool hasWall = entranceCell.HasWall(side);
                Debug.Log($"[Diag] Entrance {side}: hasWall={hasWall}");
            }
        }
    }
}
