using System.IO;
using RollAndEscape.Gameplay;
using RollAndEscape.MazeGeneration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace RollAndEscape.EditorTools
{
    /// <summary>
    /// Milestone 2 deliverable: builds the clean-minimalist maze materials, prefabs, lighting,
    /// camera framing, and a fully instantiated preview maze - entirely at Editor time, no
    /// Play mode required - so the visual style/lighting/camera angle can be judged just by
    /// opening the resulting scene. See Docs/SETUP.md section 6 for why generator scripts
    /// like this one are how scene/prefab milestones are delivered on a machine with no
    /// Unity Editor available to author or validate raw .unity/.prefab files by hand.
    ///
    /// Run via the Unity menu: Roll & Escape -> Milestone 2 - Build Maze Preview Scene.
    /// Re-running it is safe: existing materials/prefabs are reused rather than duplicated
    /// (looked up via AssetDatabase.LoadAssetAtPath), while the scene itself is always
    /// rebuilt from a brand new empty scene (rather than reopening whatever was last saved
    /// at ScenePath) and then overwrites that file - so a previous run left in a partial or
    /// unexpected state can never leak stale GameObjects into a fresh run.
    /// </summary>
    public static class Milestone2_MazeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Game.unity";
        private const string MaterialsFolder = "Assets/Materials";
        private const string PrefabsFolder = "Assets/Prefabs/Maze";

        // Clean-minimalist palette: soft, flat, few materials.
        private static readonly Color FloorColor = new Color32(0xED, 0xEF, 0xF2, 0xFF);
        private static readonly Color WallColor = new Color32(0x8F, 0xB8, 0xDE, 0xFF);
        private static readonly Color EntranceColor = new Color32(0xA8, 0xD8, 0xB9, 0xFF);
        private static readonly Color ExitColor = new Color32(0xF4, 0xD5, 0x8D, 0xFF);
        private static readonly Color BackgroundColor = new Color32(0xF7, 0xF8, 0xFA, 0xFF);

        private const int PreviewWidth = 8;
        private const int PreviewHeight = 8;
        private const int PreviewSeed = 1;
        private const float CellSize = 2f;

        [MenuItem("Roll & Escape/Milestone 2 - Build Maze Preview Scene")]
        public static void Build()
        {
            ConfigureColorSpace();
            ConfigureRenderPipeline();

            var floorMat = GetOrCreateMaterial("Floor.mat", FloorColor);
            var wallMat = GetOrCreateMaterial("Wall.mat", WallColor);
            var entranceMat = GetOrCreateMaterial("EntranceMarker.mat", EntranceColor);
            var exitMat = GetOrCreateMaterial("ExitMarker.mat", ExitColor);

            var floorPrefab = GetOrCreatePrimitivePrefab("FloorTile.prefab", PrimitiveType.Cube, floorMat, keepCollider: true);
            var wallPrefab = GetOrCreatePrimitivePrefab("WallSegment.prefab", PrimitiveType.Cube, wallMat, keepCollider: true);
            var entrancePrefab = GetOrCreateMarkerPrefab("EntranceMarker.prefab", entranceMat);
            var exitPrefab = GetOrCreateMarkerPrefab("ExitMarker.prefab", exitMat);

            var scene = CreateFreshScene();

            ConfigureLighting();
            ConfigureCamera(out var cameraFramer);

            var mazeRoot = ConfigureMazeRoot(floorPrefab, wallPrefab, entrancePrefab, exitPrefab);

            // Build immediately at edit time (deterministic, pure C# generator - no Play mode
            // needed) so opening this scene already shows the finished maze.
            var generator = new RecursiveBacktrackerMazeGenerator();
            var model = generator.Generate(MazeGenerationSettings.Default(PreviewWidth, PreviewHeight, PreviewSeed));
            mazeRoot.BuildMaze(model);
            Debug.Log($"[Diag] mazeRoot(instance {mazeRoot.GetInstanceID()}, GO {mazeRoot.gameObject.GetInstanceID()}).LastBuiltExit={mazeRoot.LastBuiltExit} right after BuildMaze (model.Exit={model.Exit})");

            cameraFramer.Frame(PreviewWidth, PreviewHeight, CellSize, mazeRoot.transform.position);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            Selection.activeGameObject = mazeRoot.gameObject;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();

            Debug.Log($"Milestone 2: built a {PreviewWidth}x{PreviewHeight} preview maze (seed {PreviewSeed}) into {ScenePath}.");
        }

        /// <summary>
        /// Same as <see cref="Build"/>, plus renders the framed Main Camera to a PNG under
        /// Docs/Screenshots/. This is the entry point meant for headless verification - e.g.
        /// `Unity.exe -batchmode -quit -projectPath &lt;path&gt; -executeMethod
        /// RollAndEscape.EditorTools.Milestone2_MazeSceneBuilder.BuildAndCaptureScreenshot` -
        /// so the result can be inspected as an image without a human clicking through the
        /// Editor UI.
        /// </summary>
        [MenuItem("Roll & Escape/Milestone 2 - Build And Capture Screenshot")]
        public static void BuildAndCaptureScreenshot()
        {
            Build();

            var cameraGO = GameObject.Find("Main Camera");
            var camera = cameraGO != null ? cameraGO.GetComponent<Camera>() : null;
            if (camera == null)
            {
                Debug.LogError("BuildAndCaptureScreenshot: no Main Camera found after Build().");
                return;
            }

            CaptureScreenshot(camera, "Docs/Screenshots/milestone2_preview.png", 1280, 800);
        }

        private static void CaptureScreenshot(Camera camera, string projectRelativePath, int width, int height)
        {
            var renderTexture = new RenderTexture(width, height, 24);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;

            try
            {
                // Deliberately not camera.Render(): under a Scriptable Render Pipeline (URP),
                // a manual Camera.Render() call bypasses the SRP's own render-request path and
                // silently produces an under-evaluated render - correct geometry/shading, but
                // material albedo never actually reaches the frame (everything reads as flat
                // grayscale, no hue at all) - which is exactly the bug this replaced.
                // RenderPipeline.SubmitRenderRequest is URP's supported way to render a single
                // camera to a texture outside the normal per-frame camera loop.
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = renderTexture };
                RenderPipeline.SubmitRenderRequest(camera, request);

                RenderTexture.active = renderTexture;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                var fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", projectRelativePath));
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                File.WriteAllBytes(fullPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);

                Debug.Log($"Saved screenshot to {fullPath}");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
            }
        }

        /// <summary>
        /// Assigns whichever URP pipeline asset exists in the project (searched by type
        /// rather than a hardcoded path, since asset naming is a one-time manual step done
        /// in-editor) as the active render pipeline in both Graphics and Quality settings.
        /// Without this, URP-shader materials render as Unity's magenta/pink error shader
        /// under the still-default Built-in pipeline - that gap is exactly what shipped
        /// broken the first two times this generator ran.
        /// </summary>
        /// <summary>
        /// URP requires Linear color space - running it under Gamma (this project's
        /// auto-generated default, since it wasn't created from Unity's URP template) is
        /// unsupported and is a very plausible cause of the washed-out/desaturated rendering
        /// every material showed regardless of its actual assigned color. Switching mid-process
        /// may not fully re-evaluate already-loaded lighting state, so this also logs a warning
        /// telling you to run the build command a second time if colors still look wrong.
        /// </summary>
        private static void ConfigureColorSpace()
        {
            if (PlayerSettings.colorSpace == ColorSpace.Linear) return;

            PlayerSettings.colorSpace = ColorSpace.Linear;
            AssetDatabase.SaveAssets();
            Debug.LogWarning("Switched project Color Space from Gamma to Linear (required for correct URP " +
                              "rendering). If colors still look wrong in this run's output, run the build " +
                              "command again - some lighting state only fully re-evaluates on a fresh process.");
        }

        private const string RendererDataPath = "Assets/Settings/RollAndEscape_Renderer.asset";
        private const string RenderPipelineAssetPath = "Assets/Settings/RollAndEscape_URP.asset";

        /// <summary>
        /// Ensures a URP pipeline asset exists and is assigned as the active render pipeline
        /// (Graphics + Quality settings) - creating it via URP's own public scripting API if
        /// it's missing, or if the one on disk turns out to be orphaned (its serialized script
        /// reference doesn't match any type in the currently installed package - this happens
        /// if the asset was authored against a different URP version, and leaves it
        /// unloadable as a RenderPipelineAsset even though the .asset file is right there).
        /// Without a properly assigned pipeline, URP-shader materials render as Unity's
        /// magenta/pink error shader under the Built-in pipeline.
        /// </summary>
        private static void ConfigureRenderPipeline()
        {
            var pipelineAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(RenderPipelineAssetPath);

            if (pipelineAsset == null)
            {
                if (File.Exists(RenderPipelineAssetPath))
                {
                    Debug.LogWarning($"{RenderPipelineAssetPath} exists but isn't loadable as a RenderPipelineAsset " +
                                      "(likely created against a different URP version) - recreating it.");
                    AssetDatabase.DeleteAsset(RenderPipelineAssetPath);
                    AssetDatabase.DeleteAsset(RendererDataPath);
                }

                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                Directory.CreateDirectory(Path.GetDirectoryName(RendererDataPath));
                AssetDatabase.CreateAsset(rendererData, RendererDataPath);

                pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipelineAsset, RenderPipelineAssetPath);
                AssetDatabase.SaveAssets();

                Debug.Log($"Created a new URP pipeline asset at {RenderPipelineAssetPath}.");
            }

            GraphicsSettings.defaultRenderPipeline = pipelineAsset;
            QualitySettings.renderPipeline = pipelineAsset;
            AssetDatabase.SaveAssets();

            Debug.Log($"Assigned {RenderPipelineAssetPath} as the active render pipeline (Graphics + Quality settings).");
        }

        private static Scene CreateFreshScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            return scene;
        }

        private static void ConfigureLighting()
        {
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 0.85f;
            light.shadows = LightShadows.Soft;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Flat ambient rather than a skybox - keeps the minimalist look free of reflections.
            // Kept deliberately dim: this palette's albedos are already light/pastel, so
            // ambient + direct light stacking past ~1.0 total per channel clips every surface
            // to flat white and erases all hue - that's what "washed out, no color at all"
            // turned out to be, not a material/shader bug.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.28f, 0.29f, 0.31f);
        }

        private static void ConfigureCamera(out MazeCameraFramer framer)
        {
            var cameraGO = new GameObject("Main Camera") { tag = "MainCamera" };

            var camera = cameraGO.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.fieldOfView = 45f;

            framer = cameraGO.AddComponent<MazeCameraFramer>();
        }

        private static MazeView3D ConfigureMazeRoot(GameObject floorPrefab, GameObject wallPrefab, GameObject entrancePrefab, GameObject exitPrefab)
        {
            var rootGO = new GameObject("MazeRoot");
            var mazeView = rootGO.AddComponent<MazeView3D>();

            var so = new SerializedObject(mazeView);
            so.FindProperty("floorTilePrefab").objectReferenceValue = floorPrefab;
            so.FindProperty("wallSegmentPrefab").objectReferenceValue = wallPrefab;
            so.FindProperty("entranceMarkerPrefab").objectReferenceValue = entrancePrefab;
            so.FindProperty("exitMarkerPrefab").objectReferenceValue = exitPrefab;
            so.FindProperty("cellSize").floatValue = CellSize;
            so.FindProperty("buildPreviewOnStart").boolValue = true;
            so.FindProperty("previewWidth").intValue = PreviewWidth;
            so.FindProperty("previewHeight").intValue = PreviewHeight;
            so.FindProperty("previewSeed").intValue = PreviewSeed;
            so.ApplyModifiedPropertiesWithoutUndo();

            return mazeView;
        }

        private static Material GetOrCreateMaterial(string fileName, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("Could not find the URP Lit shader - is the Universal RP package installed? " +
                                "(Window > Package Manager > Unity Registry > search \"Universal RP\" > Install.) " +
                                "Materials will render as the pink/magenta error shader until it is.");
                shader = Shader.Find("Standard"); // still create *something* rather than a null material reference
            }

            string path = $"{MaterialsFolder}/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                // Re-stamp shader/color every run (not just on first creation) so a material
                // created before URP finished installing - which silently fell back to a
                // shader that's now incompatible with the active pipeline - self-heals on the
                // next run instead of staying broken forever.
                if (existing.shader != shader) existing.shader = shader;
                ApplyColor(existing, color);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var material = new Material(shader);
            ApplyColor(material, color);
            Directory.CreateDirectory(MaterialsFolder);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>
        /// Sets the material's tint directly on URP Lit's actual backing property
        /// (_BaseColor) rather than relying only on the generic Material.color property -
        /// when a material's shader was just switched (e.g. self-healing from Standard back
        /// to URP Lit above), Material.color's "which property is this shader's main color"
        /// resolution isn't reliably reflecting the just-changed shader in the same call, so
        /// setting _BaseColor explicitly is what actually made the color show up instead of
        /// every surface rendering as flat white/gray regardless of the intended palette.
        /// </summary>
        private static void ApplyColor(Material material, Color color)
        {
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);

            // Flat, matte "clean minimalist" look - also avoids URP Lit's Fresnel/environment-
            // reflection term (strong at the shallow, grazing viewing angles this maze is seen
            // from) tinting every surface toward the skybox's near-white color and washing out
            // the intended palette.
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0f);
            if (material.HasProperty("_EnvironmentReflections")) material.SetFloat("_EnvironmentReflections", 0f);
            material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        }

        private static GameObject GetOrCreatePrimitivePrefab(string fileName, PrimitiveType primitiveType, Material material, bool keepCollider)
        {
            string path = $"{PrefabsFolder}/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var instance = GameObject.CreatePrimitive(primitiveType);
            if (material != null) instance.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
            {
                var collider = instance.GetComponent<Collider>();
                if (collider != null) Object.DestroyImmediate(collider);
            }

            Directory.CreateDirectory(PrefabsFolder);
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        /// <summary>Entrance/exit markers: a thin, non-colliding disc sitting just above the floor.</summary>
        private static GameObject GetOrCreateMarkerPrefab(string fileName, Material material)
        {
            string path = $"{PrefabsFolder}/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var instance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            instance.transform.localScale = new Vector3(1.1f, 0.03f, 1.1f);
            if (material != null) instance.GetComponent<Renderer>().sharedMaterial = material;
            var collider = instance.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);

            Directory.CreateDirectory(PrefabsFolder);
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }
    }
}
