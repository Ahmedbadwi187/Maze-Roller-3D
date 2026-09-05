using System.Collections.Generic;
using System.IO;
using RollAndEscape.Levels;
using RollAndEscape.MazeGeneration;
using UnityEditor;
using UnityEngine;

namespace RollAndEscape.EditorTools
{
    /// <summary>
    /// Draws a maze straight from its data (MazeModel), not from the 3D scene - thin wall
    /// lines on a plain floor, entrance/exit tinted, and the actual solution path highlighted.
    /// Replaces MazeTopDownDiagnostic's screenshot for level1_topdown.png: that tool rendered
    /// the real 3D wall geometry from directly above, which is accurate but reads as a grid of
    /// solid boxes rather than a maze (walls are chunky 3D prisms, not thin lines) - repeated
    /// user reports of "all boxes closed" against that image were a readability problem with
    /// the render style, not a bug in the maze data (EveryActualLevel_IsFullyConnectedWithASolutionPath
    /// already proves every one of the 100 real levels has a solution path). This tool can't be
    /// ambiguous about openings the way a 3D render can: it draws exactly what MazeCell.Walls
    /// says and nothing else, with the actual BFS solution path traced in a distinct color.
    /// </summary>
    public static class MazeBlueprintDiagnostic
    {
        private const int CellPx = 64;
        private const int Margin = 24;
        private const int WallThickness = 8;

        private static readonly Color FloorColor = new Color(0.93f, 0.93f, 0.95f);
        private static readonly Color PathColor = new Color(0.55f, 0.78f, 1f);
        private static readonly Color EntranceColor = new Color(0.25f, 0.75f, 0.35f);
        private static readonly Color ExitColor = new Color(0.95f, 0.72f, 0.15f);
        private static readonly Color WallColor = new Color(0.10f, 0.12f, 0.18f);
        private static readonly Color BallColor = new Color(0.88f, 0.28f, 0.22f);

        [MenuItem("Roll & Escape/Diagnostics - Level 1 Blueprint (clear 2D)")]
        public static void CaptureLevel1() => Capture(0, "Docs/Screenshots/level1_topdown.png");

        public static void Capture(int levelIndex, string relativeOutputPath)
        {
            var level = LevelRepository.GetLevel(levelIndex);
            var generator = new RecursiveBacktrackerMazeGenerator();
            var model = generator.Generate(MazeGenerationSettings.Default(level.Width, level.Height, level.Seed));

            int texWidth = Margin * 2 + level.Width * CellPx;
            int texHeight = Margin * 2 + level.Height * CellPx;
            var tex = new Texture2D(texWidth, texHeight, TextureFormat.RGB24, false);

            var background = new Color[texWidth * texHeight];
            for (int i = 0; i < background.Length; i++) background[i] = FloorColor;
            tex.SetPixels(background);

            var path = model.FindSolutionPath();
            var pathSet = new HashSet<(int, int)>(path);
            foreach (var (c, r) in pathSet)
            {
                FillCell(tex, c, r, PathColor);
            }

            FillCell(tex, model.Entrance.Column, model.Entrance.Row, EntranceColor);
            FillCell(tex, model.Exit.Column, model.Exit.Row, ExitColor);

            foreach (var (c, r) in model.AllCoordinates())
            {
                DrawCellWalls(tex, model, c, r);
            }

            DrawBall(tex, model.Entrance.Column, model.Entrance.Row);

            tex.Apply();

            var fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativeOutputPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());

            Debug.Log($"[Diag] Level {levelIndex + 1} ({level.Width}x{level.Height}, seed {level.Seed}): " +
                      $"solution path has {path.Count} cells, fully connected={model.IsFullyConnected()}. Saved {fullPath}");

            Object.DestroyImmediate(tex);
        }

        private static (int x0, int x1, int y0, int y1) CellRect(int column, int row)
        {
            int x0 = Margin + column * CellPx;
            int y0 = Margin + row * CellPx;
            return (x0, x0 + CellPx, y0, y0 + CellPx);
        }

        private static void FillCell(Texture2D tex, int column, int row, Color color)
        {
            var (x0, x1, y0, y1) = CellRect(column, row);
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                    tex.SetPixel(x, y, color);
        }

        private static void DrawCellWalls(Texture2D tex, MazeModel model, int column, int row)
        {
            var (x0, x1, y0, y1) = CellRect(column, row);
            var cell = model.CellAt(column, row);
            int t = WallThickness;

            if (cell.HasWall(WallSide.South)) FillRect(tex, x0 - t, x1 + t, y0 - t / 2, y0 + t / 2);
            if (cell.HasWall(WallSide.North)) FillRect(tex, x0 - t, x1 + t, y1 - t / 2, y1 + t / 2);
            if (cell.HasWall(WallSide.West)) FillRect(tex, x0 - t / 2, x0 + t / 2, y0 - t, y1 + t);
            if (cell.HasWall(WallSide.East)) FillRect(tex, x1 - t / 2, x1 + t / 2, y0 - t, y1 + t);
        }

        private static void FillRect(Texture2D tex, int x0, int x1, int y0, int y1)
        {
            x0 = Mathf.Clamp(x0, 0, tex.width - 1);
            x1 = Mathf.Clamp(x1, 0, tex.width - 1);
            y0 = Mathf.Clamp(y0, 0, tex.height - 1);
            y1 = Mathf.Clamp(y1, 0, tex.height - 1);
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                    tex.SetPixel(x, y, WallColor);
        }

        private static void DrawBall(Texture2D tex, int column, int row)
        {
            var (x0, x1, y0, y1) = CellRect(column, row);
            float cx = (x0 + x1) / 2f;
            float cy = (y0 + y1) / 2f;
            float radius = CellPx * 0.3f;

            int minX = Mathf.Clamp(Mathf.FloorToInt(cx - radius), 0, tex.width - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(cx + radius), 0, tex.width - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(cy - radius), 0, tex.height - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(cy + radius), 0, tex.height - 1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    if (dx * dx + dy * dy <= radius * radius) tex.SetPixel(x, y, BallColor);
                }
            }
        }
    }
}
