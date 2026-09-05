using System.IO;
using RollAndEscape.Gameplay;
using RollAndEscape.Levels;
using RollAndEscape.MazeGeneration;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RollAndEscape.EditorTools
{
    /// <summary>Straight-down bird's-eye screenshot of the real Level 1 maze - removes all
    /// camera-tilt ambiguity when checking whether a specific wall opening is actually
    /// rendered where the data says it should be.</summary>
    public static class MazeTopDownDiagnostic
    {
        [MenuItem("Roll & Escape/Diagnostics - Top-Down Level 1 Screenshot")]
        public static void CaptureTopDown()
        {
            Milestone9_PolishBuilder.Build();
            EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);

            var mazeRootGO = GameObject.Find("MazeRoot");
            var mazeView = mazeRootGO.GetComponent<MazeView3D>();

            var level = LevelRepository.GetLevel(0);
            var generator = new RecursiveBacktrackerMazeGenerator();
            var model = generator.Generate(MazeGenerationSettings.Default(level.Width, level.Height, level.Seed));
            mazeView.BuildMaze(model);

            var cameraGO = GameObject.Find("Main Camera");
            var pipelineAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/RollAndEscape_URP.asset");
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = pipelineAsset;
            QualitySettings.renderPipeline = pipelineAsset;

            var camera = cameraGO.GetComponent<Camera>();
            float centerX = (level.Width - 1) * mazeView.CellSize / 2f;
            float centerZ = (level.Height - 1) * mazeView.CellSize / 2f;
            camera.transform.position = new Vector3(centerX, 20f, centerZ);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // straight down
            camera.orthographic = true;
            camera.orthographicSize = level.Height * mazeView.CellSize / 2f + 1f;

            int w = 1024, h = 1024;
            var rt = new RenderTexture(w, h, 24);
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
            RenderPipeline.SubmitRenderRequest(camera, request);

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            var fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs/Screenshots/level1_topdown.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());

            Debug.Log($"[Diag] Entrance={model.Entrance}, world pos={mazeView.CellToWorldPosition(model.Entrance.Column, model.Entrance.Row)}, " +
                      $"camera center=({centerX},{centerZ}), saved {fullPath}");

            RenderTexture.active = null;
            rt.Release();
        }
    }
}
