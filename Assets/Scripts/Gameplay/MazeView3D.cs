using System.Collections.Generic;
using RollAndEscape.MazeGeneration;
using UnityEngine;

namespace RollAndEscape.Gameplay
{
    /// <summary>
    /// Turns a <see cref="MazeModel"/> into 3D geometry: one floor tile per cell, wall
    /// segments wherever the model has a wall, and entrance/exit markers. Reused for both
    /// the milestone-2 in-editor preview (Editor/Milestone2_MazeSceneBuilder.cs calls
    /// BuildMaze directly, no Play mode needed) and, later, the real Game scene's level
    /// loader (LevelRepository will hand this a generated MazeModel per level).
    ///
    /// A single wall prefab is reused for all four wall orientations - each spawned instance
    /// is just scaled/rotated/positioned per-call rather than needing four separate prefabs.
    /// Interior walls are shared between two cells (verified symmetric by
    /// RecursiveBacktrackerMazeGeneratorTests.Generate_EveryNonBoundaryWall_...), so only a
    /// cell's North/East walls are spawned from its own data, plus South walls for row 0 and
    /// West walls for column 0 to cover the two boundary edges North/East don't reach -
    /// that covers every wall exactly once with no duplicates and no gaps.
    /// </summary>
    public class MazeView3D : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject floorTilePrefab;
        [SerializeField] private GameObject wallSegmentPrefab;
        [SerializeField] private GameObject entranceMarkerPrefab;
        [SerializeField] private GameObject exitMarkerPrefab;

        [Header("Grid dimensions")]
        [SerializeField] private float cellSize = 2f;
        [SerializeField] private float wallHeight = 1.5f;
        [SerializeField] private float wallThickness = 0.2f;
        [SerializeField] private float floorThickness = 0.2f;

        [Header("Preview generation (Milestone 2 only)")]
        [Tooltip("If enabled, builds a test maze from the fields below on Start. Real levels " +
                 "will instead call BuildMaze(model) directly with a level-specific MazeModel " +
                 "from LevelRepository and leave this off.")]
        [SerializeField] private bool buildPreviewOnStart = true;
        [SerializeField] private int previewWidth = 8;
        [SerializeField] private int previewHeight = 8;
        [SerializeField] private int previewSeed = 1;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        public float CellSize => cellSize;

        private void Start()
        {
            if (!buildPreviewOnStart) return;

            var generator = new RecursiveBacktrackerMazeGenerator();

            // A level chosen in Level Select (milestone 5) takes priority over this
            // component's own preview fields - those exist for milestones 2-4's standalone
            // test scenes and for opening Game.unity directly in-editor during development.
            if (LevelSessionContext.HasSelectedLevel)
            {
                var (width, height, seed) = LevelSessionContext.ConsumeSelectedLevel();
                BuildMaze(generator.Generate(MazeGenerationSettings.Default(width, height, seed)));
                return;
            }

            var model = generator.Generate(MazeGenerationSettings.Default(previewWidth, previewHeight, previewSeed));
            BuildMaze(model);
        }

        /// <summary>Destroys any previously spawned maze geometry under this view.</summary>
        public void Clear()
        {
            foreach (var go in _spawned)
            {
                if (go == null) continue;
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
            _spawned.Clear();
        }

        /// <summary>World-space position of the given cell's floor center.</summary>
        public Vector3 CellToWorldPosition(int column, int row) =>
            transform.position + new Vector3(column * cellSize, 0f, row * cellSize);

        /// <summary>Entrance/exit cells from the most recent BuildMaze call - lets other scripts
        /// (ball spawn placement, exit trigger placement) avoid hardcoding maze coordinates.</summary>
        public (int Column, int Row) LastBuiltEntrance { get; private set; }
        public (int Column, int Row) LastBuiltExit { get; private set; }

        public void BuildMaze(MazeModel model)
        {
            Clear();
            LastBuiltEntrance = model.Entrance;
            LastBuiltExit = model.Exit;

            foreach (var (column, row) in model.AllCoordinates())
            {
                SpawnFloor(column, row);

                ref readonly var cell = ref model.CellAt(column, row);
                if (cell.HasWall(WallSide.North)) SpawnWall(column, row, WallSide.North);
                if (cell.HasWall(WallSide.East)) SpawnWall(column, row, WallSide.East);
                if (row == 0 && cell.HasWall(WallSide.South)) SpawnWall(column, row, WallSide.South);
                if (column == 0 && cell.HasWall(WallSide.West)) SpawnWall(column, row, WallSide.West);
            }

            SpawnMarker(entranceMarkerPrefab, model.Entrance, "EntranceMarker");
            SpawnMarker(exitMarkerPrefab, model.Exit, "ExitMarker");
        }

        private void SpawnFloor(int column, int row)
        {
            if (floorTilePrefab == null) return;

            var position = CellToWorldPosition(column, row) + new Vector3(0f, -floorThickness / 2f, 0f);
            var go = Spawn(floorTilePrefab, position, Quaternion.identity, $"Floor_{column}_{row}");
            go.transform.localScale = new Vector3(cellSize, floorThickness, cellSize);
        }

        private void SpawnWall(int column, int row, WallSide side)
        {
            if (wallSegmentPrefab == null) return;

            var basePosition = CellToWorldPosition(column, row) + new Vector3(0f, wallHeight / 2f, 0f);
            Vector3 offset;
            Quaternion rotation;

            switch (side)
            {
                case WallSide.North:
                    offset = new Vector3(0f, 0f, cellSize / 2f);
                    rotation = Quaternion.identity;
                    break;
                case WallSide.South:
                    offset = new Vector3(0f, 0f, -cellSize / 2f);
                    rotation = Quaternion.identity;
                    break;
                case WallSide.East:
                    offset = new Vector3(cellSize / 2f, 0f, 0f);
                    rotation = Quaternion.Euler(0f, 90f, 0f);
                    break;
                case WallSide.West:
                default:
                    offset = new Vector3(-cellSize / 2f, 0f, 0f);
                    rotation = Quaternion.Euler(0f, 90f, 0f);
                    break;
            }

            var go = Spawn(wallSegmentPrefab, basePosition + offset, rotation, $"Wall_{column}_{row}_{side}");
            go.transform.localScale = new Vector3(cellSize, wallHeight, wallThickness);
        }

        private void SpawnMarker(GameObject prefab, (int Column, int Row) cell, string name)
        {
            if (prefab == null) return;

            var position = CellToWorldPosition(cell.Column, cell.Row) + new Vector3(0f, 0.03f, 0f);
            Spawn(prefab, position, Quaternion.identity, name);
        }

        private GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, string name)
        {
            var go = Instantiate(prefab, position, rotation, transform);
            go.name = name;
            _spawned.Add(go);
            return go;
        }
    }
}
