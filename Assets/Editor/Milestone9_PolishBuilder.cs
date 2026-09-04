using System.IO;
using MazeRoller3D.UI;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MazeRoller3D.EditorTools
{
    /// <summary>
    /// Milestone 9 deliverable: swaps the Game scene's plain follow camera for a Cinemachine
    /// rig, adds a particle burst + procedurally-synthesized chime on level completion, a
    /// generated placeholder app icon, and a Splash scene that fades in a logo then loads
    /// Level Select. Real SFX/art assets are a drop-in replacement for these later - see each
    /// piece's own doc comment for exactly what to swap.
    ///
    /// Run via the Unity menu: Maze Roller 3D -> Milestone 9 - Build Polish.
    /// </summary>
    public static class Milestone9_PolishBuilder
    {
        private const string SplashScenePath = "Assets/Scenes/Splash.unity";
        private const string LevelSelectScenePath = "Assets/Scenes/LevelSelect.unity";
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string SettingsScenePath = "Assets/Scenes/Settings.unity";
        private const string ParticlePrefabPath = "Assets/Prefabs/LevelCompleteBurst.prefab";
        private const string AppIconPath = "Assets/Art/AppIcon.png";

        [MenuItem("Maze Roller 3D/Milestone 9 - Build Polish")]
        public static void Build()
        {
            Milestone7_SettingsBuilder.Build(); // ensures LevelSelect/Game/Settings all exist and are current

            UpgradeGameSceneCamera();
            AddCompletionPolishToGameScene();
            BuildSplashScene();
            GenerateAppIcon();

            Debug.Log("Milestone 9: Cinemachine camera, level-complete particles/SFX, app icon, and Splash scene all built.");
        }

        /// <summary>Replaces BallFollowCamera (milestone 3's plain script) with a Cinemachine
        /// rig using the same offset/tilt values, so the framing doesn't jump.</summary>
        private static void UpgradeGameSceneCamera()
        {
            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            var cameraGO = GameObject.Find("Main Camera");
            var plainFollow = cameraGO.GetComponent<Gameplay.BallFollowCamera>();
            if (plainFollow != null) Object.DestroyImmediate(plainFollow);

            if (cameraGO.GetComponent<CinemachineBrain>() == null) cameraGO.AddComponent<CinemachineBrain>();

            var ballGO = GameObject.Find("Ball");

            var cmGO = GameObject.Find("CM Ball Camera");
            if (cmGO == null) cmGO = new GameObject("CM Ball Camera", typeof(CinemachineCamera));

            var cmCamera = cmGO.GetComponent<CinemachineCamera>();
            if (cmCamera == null) cmCamera = cmGO.AddComponent<CinemachineCamera>();
            cmCamera.Follow = ballGO.transform;
            var lens = cmCamera.Lens;
            lens.FieldOfView = 45f;
            cmCamera.Lens = lens;

            var follow = cmGO.GetComponent<CinemachineFollow>();
            if (follow == null) follow = cmGO.AddComponent<CinemachineFollow>();
            follow.FollowOffset = new Vector3(0f, 6f, -5f); // same offset BallFollowCamera used

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void AddCompletionPolishToGameScene()
        {
            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            var exitTriggerGO = GameObject.Find("ExitTrigger");
            var particlePrefab = GetOrCreateParticlePrefab();
            var particleGO = (GameObject)PrefabUtility.InstantiatePrefab(particlePrefab, exitTriggerGO.transform);
            particleGO.transform.localPosition = Vector3.zero;
            var particleSystem = particleGO.GetComponent<ParticleSystem>();

            var flowGO = GameObject.Find("GameFlow");
            var audioSource = flowGO.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = flowGO.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            var flow = flowGO.GetComponent<UI.LevelFlowController>();
            var so = new SerializedObject(flow);
            so.FindProperty("completionParticles").objectReferenceValue = particleSystem;
            so.FindProperty("completionAudioSource").objectReferenceValue = audioSource;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static GameObject GetOrCreateParticlePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ParticlePrefabPath);
            if (existing != null) return existing;

            var go = new GameObject("LevelCompleteBurst", typeof(ParticleSystem));
            var ps = go.GetComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 1.2f;
            main.startSpeed = 4f;
            main.startSize = 0.2f;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color32(0xE0, 0x7A, 0x5F, 0xFF), new Color32(0xF4, 0xD5, 0x8D, 0xFF)); // coral <-> gold, matching the palette

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            Directory.CreateDirectory(Path.GetDirectoryName(ParticlePrefabPath));
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, ParticlePrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void BuildSplashScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory(Path.GetDirectoryName(SplashScenePath));
            EditorSceneManager.SaveScene(scene, SplashScenePath);

            var canvasGO = new GameObject("SplashCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvasGO.transform, false);
            UIBuilderHelpers.StretchToFill(background.GetComponent<RectTransform>());
            background.GetComponent<Image>().color = new Color(0.97f, 0.97f, 0.98f, 1f);

            var logoGO = new GameObject("Logo", typeof(RectTransform), typeof(CanvasGroup));
            logoGO.transform.SetParent(canvasGO.transform, false);
            var logoRect = logoGO.GetComponent<RectTransform>();
            logoRect.anchorMin = logoRect.anchorMax = new Vector2(0.5f, 0.5f);
            logoRect.sizeDelta = new Vector2(800, 300);
            var logoGroup = logoGO.GetComponent<CanvasGroup>();

            UIBuilderHelpers.CreateText("Title", logoGO.transform, "Maze Roller 3D", Vector2.zero, 72);

            var splash = canvasGO.AddComponent<SplashAnimator>();
            var so = new SerializedObject(splash);
            so.FindProperty("logoGroup").objectReferenceValue = logoGroup;
            so.FindProperty("logoTransform").objectReferenceValue = logoRect;
            so.FindProperty("nextSceneName").stringValue = "LevelSelect";
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SplashScenePath);

            // Splash first so it's the app's actual startup scene.
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SplashScenePath, true),
                new EditorBuildSettingsScene(LevelSelectScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
                new EditorBuildSettingsScene(SettingsScenePath, true)
            };
        }

        /// <summary>
        /// Generates a simple placeholder icon (coral ball on the wall-blue background) rather
        /// than shipping with no icon at all - swap Assets/Art/AppIcon.png for real artwork
        /// whenever it exists; this method only needs to not be re-run afterward.
        /// </summary>
        private static void GenerateAppIcon()
        {
            const int size = 512;
            var backgroundColor = new Color32(0x8F, 0xB8, 0xDE, 0xFF);
            var ballColor = new Color32(0xE0, 0x7A, 0x5F, 0xFF);
            var ballHighlight = new Color32(0xF4, 0xD5, 0x8D, 0xFF);

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            var center = new Vector2(size / 2f, size / 2f);
            float ballRadius = size * 0.3f;
            var highlightCenter = center + new Vector2(-ballRadius * 0.35f, ballRadius * 0.35f);
            float highlightRadius = ballRadius * 0.35f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var point = new Vector2(x, y);
                    Color32 pixel = backgroundColor;
                    if (Vector2.Distance(point, center) <= ballRadius)
                    {
                        pixel = Vector2.Distance(point, highlightCenter) <= highlightRadius ? ballHighlight : ballColor;
                    }
                    pixels[y * size + x] = pixel;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(AppIconPath));
            File.WriteAllBytes(AppIconPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(AppIconPath);

            if (AssetImporter.GetAtPath(AppIconPath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            var iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new[] { iconTexture });
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, new[] { iconTexture });
        }
    }
}
