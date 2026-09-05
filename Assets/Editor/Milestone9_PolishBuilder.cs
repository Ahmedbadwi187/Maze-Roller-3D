using System.IO;
using RollAndEscape.UI;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEscape.EditorTools
{
    /// <summary>
    /// Milestone 9 deliverable: swaps the Game scene's plain follow camera for a Cinemachine
    /// rig, adds a particle burst + procedurally-synthesized chime on level completion, a
    /// generated placeholder app icon, and a Splash scene that fades in a logo then loads
    /// Level Select. Real SFX/art assets are a drop-in replacement for these later - see each
    /// piece's own doc comment for exactly what to swap.
    ///
    /// Run via the Unity menu: Roll & Escape -> Milestone 9 - Build Polish.
    /// </summary>
    public static class Milestone9_PolishBuilder
    {
        private const string SplashScenePath = "Assets/Scenes/Splash.unity";
        private const string LevelSelectScenePath = "Assets/Scenes/LevelSelect.unity";
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string SettingsScenePath = "Assets/Scenes/Settings.unity";
        private const string ParticlePrefabPath = "Assets/Prefabs/LevelCompleteBurst.prefab";
        private const string AppIconPath = "Assets/Art/AppIcon.png";

        [MenuItem("Roll & Escape/Milestone 9 - Build Polish")]
        public static void Build()
        {
            Milestone7_SettingsBuilder.Build(); // ensures LevelSelect/Game/Settings all exist and are current

            UpgradeGameSceneCamera();
            AddCompletionPolishToGameScene();
            AddPauseMenuToGameScene();
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
            cmCamera.LookAt = ballGO.transform;
            var lens = cmCamera.Lens;
            lens.FieldOfView = 45f;
            cmCamera.Lens = lens;

            var follow = cmGO.GetComponent<CinemachineFollow>();
            if (follow == null) follow = cmGO.AddComponent<CinemachineFollow>();
            follow.FollowOffset = new Vector3(0f, 13f, -11f); // pulled back further than BallFollowCamera's original offset so more of the maze is visible around the ball, same ~50 degree tilt ratio (13:11)

            // Body (CinemachineFollow) only controls position - an explicit Aim behaviour is
            // needed for rotation, recomputed correctly every frame by Cinemachine itself
            // rather than relying on whatever the transform happened to start with. Aiming at
            // the ball with this fixed offset naturally produces ~50 degrees of downward tilt
            // (atan(6/5)), matching the intended ~52 degree angle almost exactly.
            var composer = cmGO.GetComponent<CinemachineRotationComposer>();
            if (composer == null) composer = cmGO.AddComponent<CinemachineRotationComposer>();

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

        /// <summary>Adds a visible Pause button plus a full pause overlay (Resume/Restart/Quit
        /// to Menu) - real device testing showed there was no way to back out of a level once
        /// inside it (no pause menu, and the Android hardware Back button did nothing useful).</summary>
        private static void AddPauseMenuToGameScene()
        {
            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            Milestone3_BallSceneBuilder.EnsureEventSystem();

            var canvasGO = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;

            // Always-visible Pause button, top-right corner - not nested under the overlay root.
            // Chrome only (button shape/color/font) - the maze/ball themselves are untouched,
            // per explicit instruction not to change gameplay colors.
            var topSafeArea = UIBuilderHelpers.CreateSafeArea(canvasGO.transform);
            var pauseButton = UIBuilderHelpers.CreateButton("PauseButton", topSafeArea, "II", new Vector2(-80, 0), new Vector2(100, 100));
            UIBuilderHelpers.AnchorToTop(pauseButton.GetComponent<RectTransform>(), 1f, 100f);
            var pauseButtonImage = pauseButton.GetComponent<Image>();
            pauseButtonImage.sprite = UIBuilderHelpers.GenerateCircleSprite("Assets/Art/PauseButtonBg.png", 128, RollAndEscapePalette.White);
            pauseButtonImage.color = Color.white;
            pauseButton.GetComponentInChildren<Text>().color = RollAndEscapePalette.BackButtonText;

            // Always-visible "Level N" indicator, top-left - per the spec's Game-scene HUD.
            var levelText = UIBuilderHelpers.CreateText("LevelIndicator", topSafeArea, "Level -", new Vector2(100, 0), 40, UIBuilderHelpers.NunitoBlack);
            UIBuilderHelpers.AnchorToTop(levelText.GetComponent<RectTransform>(), 0f, 100f);
            levelText.color = RollAndEscapePalette.LevelEyebrow;
            var hud = canvasGO.AddComponent<LevelHudUI>();
            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("levelText").objectReferenceValue = levelText;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            var overlayRoot = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            overlayRoot.transform.SetParent(canvasGO.transform, false);
            UIBuilderHelpers.StretchToFill(overlayRoot.GetComponent<RectTransform>());
            overlayRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            UIBuilderHelpers.CreateText("PausedTitle", overlayRoot.transform, "Paused", new Vector2(0, 300), 64, UIBuilderHelpers.NunitoBlack).color = Color.white;

            var resumeButton = UIBuilderHelpers.CreateButton("ResumeButton", overlayRoot.transform, "Resume", new Vector2(0, 100), new Vector2(320, 100));
            resumeButton.GetComponent<Image>().color = RollAndEscapePalette.ContinueButtonBg;
            var resumeLabel = resumeButton.GetComponentInChildren<Text>();
            resumeLabel.color = RollAndEscapePalette.White;
            resumeLabel.font = UIBuilderHelpers.NunitoBlack;

            var restartButton = UIBuilderHelpers.CreateButton("RestartButton", overlayRoot.transform, "Restart", new Vector2(0, -30), new Vector2(320, 100));
            StyleAsSecondaryButton(restartButton);

            var quitButton = UIBuilderHelpers.CreateButton("QuitToMenuButton", overlayRoot.transform, "Quit to Menu", new Vector2(0, -160), new Vector2(320, 100));
            StyleAsSecondaryButton(quitButton);

            var pauseUI = canvasGO.AddComponent<PauseUI>();
            var so = new SerializedObject(pauseUI);
            so.FindProperty("root").objectReferenceValue = overlayRoot;
            so.FindProperty("pauseButton").objectReferenceValue = pauseButton;
            so.FindProperty("resumeButton").objectReferenceValue = resumeButton;
            so.FindProperty("restartButton").objectReferenceValue = restartButton;
            so.FindProperty("quitToMenuButton").objectReferenceValue = quitButton;
            so.FindProperty("levelSelectSceneName").stringValue = "LevelSelect";
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        /// <summary>Transparent-fill, white-bordered button style - the mockup's secondary
        /// action look (vs. a solid-fill primary button).</summary>
        private static void StyleAsSecondaryButton(Button button)
        {
            var image = button.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.sprite = UIBuilderHelpers.GenerateRoundedRectOutlineSprite($"Assets/Art/SecondaryBorder_{button.name}.png", 256, 28f, 4f, RollAndEscapePalette.White);
            var label = button.GetComponentInChildren<Text>();
            label.color = RollAndEscapePalette.White;
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

        private const string SplashBackgroundPath = "Assets/Art/SplashBackground.png";
        private const string SplashIconBackgroundPath = "Assets/Art/SplashIconBackground.png";

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

            // 160deg purple linear gradient, per the approved "Buze" mockup (Claude Design
            // project d6305a3a, Buze.dc.html's splash screen) - replacing the earlier radial
            // approximation with the mockup's actual gradient angle/colors.
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvasGO.transform, false);
            UIBuilderHelpers.StretchToFill(background.GetComponent<RectTransform>());
            var backgroundImage = background.GetComponent<Image>();
            backgroundImage.sprite = UIBuilderHelpers.GenerateSprite(SplashBackgroundPath, 270, 480, (x, y) =>
            {
                float t = UIBuilderHelpers.LinearGradientT(x, y, 270, 480, 160f);
                return UIBuilderHelpers.LerpStops(t, (0f, RollAndEscapePalette.SplashBgTop), (1f, RollAndEscapePalette.SplashBgBottom));
            });
            backgroundImage.color = Color.white;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = false;

            var logoGO = new GameObject("Logo", typeof(RectTransform), typeof(CanvasGroup));
            logoGO.transform.SetParent(canvasGO.transform, false);
            var logoRect = logoGO.GetComponent<RectTransform>();
            logoRect.anchorMin = logoRect.anchorMax = new Vector2(0.5f, 0.5f);
            logoRect.sizeDelta = new Vector2(800, 700);
            var logoGroup = logoGO.GetComponent<CanvasGroup>();

            // Green rounded-square icon "chip", per the mockup - a container the ball sits in
            // rather than the ball floating bare on the background like the old design.
            var iconBgGO = new GameObject("IconBackground", typeof(RectTransform), typeof(Image));
            iconBgGO.transform.SetParent(logoGO.transform, false);
            var iconBgRect = iconBgGO.GetComponent<RectTransform>();
            iconBgRect.anchorMin = iconBgRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconBgRect.anchoredPosition = new Vector2(0, 150);
            iconBgRect.sizeDelta = new Vector2(320, 320);
            var iconBgImage = iconBgGO.GetComponent<Image>();
            iconBgImage.sprite = UIBuilderHelpers.GenerateRoundedRectSprite(SplashIconBackgroundPath, 256, 64f, RollAndEscapePalette.IconSquareGreen);
            iconBgImage.color = Color.white; // color baked into the generated sprite itself

            // The ball, gold/amber with a highlight - matches the mockup's radial-gradient ball
            // (approximated as a solid tint since uGUI Image can't do a 2-stop radial fill).
            var ballIconGO = new GameObject("BallIcon", typeof(RectTransform), typeof(Image));
            ballIconGO.transform.SetParent(iconBgGO.transform, false);
            var ballIconRect = ballIconGO.GetComponent<RectTransform>();
            ballIconRect.anchorMin = ballIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            ballIconRect.anchoredPosition = Vector2.zero;
            ballIconRect.sizeDelta = new Vector2(130, 130);
            var ballIconImage = ballIconGO.GetComponent<Image>();
            ballIconImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            ballIconImage.color = RollAndEscapePalette.BallHighlight;

            var title = UIBuilderHelpers.CreateText("Title", logoGO.transform, "Roll & Escape", new Vector2(0, -100), 76, UIBuilderHelpers.NunitoBlack);
            title.color = Color.white;

            var subtitle = UIBuilderHelpers.CreateText("Subtitle", logoGO.transform, "100 MAZES TO SOLVE", new Vector2(0, -190), 26);
            subtitle.color = new Color(1f, 1f, 1f, 0.75f);

            var tapToStart = UIBuilderHelpers.CreateText("TapToStart", logoGO.transform, "Tap to start", new Vector2(0, -300), 28, UIBuilderHelpers.NunitoBlack);
            tapToStart.color = new Color(1f, 1f, 1f, 0.6f);

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
