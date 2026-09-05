using System.IO;
using RollAndEscape.Gameplay;
using RollAndEscape.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEscape.EditorTools
{
    /// <summary>
    /// Milestone 4 deliverable: an invisible trigger volume at the maze exit
    /// (Assets/Prefabs/ExitTrigger.prefab, matching the project's prefab list), a Level
    /// Complete overlay (time + Replay/Next Level buttons), and a LevelFlowController wiring
    /// the two together on top of the Milestone 3 ball scene.
    ///
    /// Run via the Unity menu: Roll & Escape -> Milestone 4 - Build Win Condition Test Scene.
    /// </summary>
    public static class Milestone4_WinConditionBuilder
    {
        private const string ExitTriggerPrefabPath = "Assets/Prefabs/ExitTrigger.prefab";
        private const string LevelCompleteUIPrefabPath = "Assets/Prefabs/UI/LevelCompleteUI.prefab";

        [MenuItem("Roll & Escape/Milestone 4 - Build Win Condition Test Scene")]
        public static void Build()
        {
            Milestone3_BallSceneBuilder.Build();

            var mazeRootGO = GameObject.Find("MazeRoot");
            var mazeView = mazeRootGO.GetComponent<MazeView3D>();
            var ballGO = GameObject.Find("Ball");
            Debug.Log($"[Diag] mazeRootGO={mazeRootGO.GetInstanceID()}, mazeView.LastBuiltExit={mazeView.LastBuiltExit}, mazeView.LastBuiltEntrance={mazeView.LastBuiltEntrance}");

            var exitTriggerGO = SpawnExitTrigger(mazeView);
            var levelCompleteUIGO = (GameObject)PrefabUtility.InstantiatePrefab(GetOrCreateLevelCompleteUIPrefab());

            var flowGO = new GameObject("GameFlow");
            var flow = flowGO.AddComponent<LevelFlowController>();
            var so = new SerializedObject(flow);
            so.FindProperty("exitTrigger").objectReferenceValue = exitTriggerGO.GetComponent<LevelExitTrigger>();
            so.FindProperty("ballController").objectReferenceValue = ballGO.GetComponent<BallController>();
            so.FindProperty("levelCompleteUI").objectReferenceValue = levelCompleteUIGO.GetComponent<LevelCompleteUI>();
            so.ApplyModifiedPropertiesWithoutUndo();

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"Milestone 4: exit trigger at {exitTriggerGO.transform.position}, Level Complete overlay and " +
                      $"LevelFlowController wired, into {scene.path}.");
        }

        // Note: whether entering this trigger actually fires LevelCompleted/shows the overlay
        // is NOT verifiable from edit-time editor scripting like Build() above -
        // MonoBehaviour lifecycle messages (Awake, OnTriggerEnter) only run in real Play mode.
        // See Assets/Tests/PlayMode/LevelFlowPlayModeTests.cs for that regression coverage.

        private static GameObject SpawnExitTrigger(MazeView3D mazeView)
        {
            var prefab = GetOrCreateExitTriggerPrefab();
            var (column, row) = mazeView.LastBuiltExit;
            var position = mazeView.CellToWorldPosition(column, row) + new Vector3(0f, 0.5f, 0f);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = position;
            return instance;
        }

        private static GameObject GetOrCreateExitTriggerPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ExitTriggerPrefabPath);
            if (existing != null)
            {
                // Re-stamp the collider size every run, not just on first creation - a tuning
                // change here (like the exit-trigger size fix) must reach an already-cached
                // prefab, not silently no-op against it forever.
                var existingCollider = existing.GetComponent<BoxCollider>();
                if (existingCollider != null) existingCollider.size = new Vector3(1.95f, 2f, 1.95f);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var go = new GameObject("ExitTrigger");
            var collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(1.95f, 2f, 1.95f); // nearly the full 2-unit cell - a ball that rolls in and settles against a wall (not dead-center) must still count as reaching the exit
            go.AddComponent<LevelExitTrigger>();

            Directory.CreateDirectory(Path.GetDirectoryName(ExitTriggerPrefabPath));
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, ExitTriggerPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private const string CompleteBackgroundPath = "Assets/Art/CompleteBackground.png";
        private const string CheckmarkCirclePath = "Assets/Art/CheckmarkCircle.png";

        private static GameObject GetOrCreateLevelCompleteUIPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(LevelCompleteUIPrefabPath);
            if (existing != null) return existing;

            var canvasGO = new GameObject("LevelCompleteCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;

            // Purple gradient panel per the approved "Buze" mockup (Claude Design project
            // d6305a3a, Buze.dc.html) - was a flat dark scrim with plain text.
            var panelGO = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(canvasGO.transform, false);
            StretchToFill(panelGO.GetComponent<RectTransform>());
            var panelImage = panelGO.GetComponent<Image>();
            panelImage.sprite = UIBuilderHelpers.GenerateSprite(CompleteBackgroundPath, 270, 480, (x, y) =>
            {
                float t = UIBuilderHelpers.LinearGradientT(x, y, 270, 480, 180f);
                return UIBuilderHelpers.LerpStops(t, (0f, RollAndEscapePalette.CompleteBgTop), (1f, RollAndEscapePalette.CompleteBgBottom));
            });
            panelImage.preserveAspect = false;

            var checkmarkBg = new GameObject("CheckmarkCircle", typeof(RectTransform), typeof(Image));
            checkmarkBg.transform.SetParent(panelGO.transform, false);
            var checkmarkRect = checkmarkBg.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = checkmarkRect.anchorMax = new Vector2(0.5f, 0.5f);
            checkmarkRect.anchoredPosition = new Vector2(0, 280);
            checkmarkRect.sizeDelta = new Vector2(180, 180);
            checkmarkBg.GetComponent<Image>().sprite = UIBuilderHelpers.GenerateCircleSprite(CheckmarkCirclePath, 128, RollAndEscapePalette.White);

            // Drawn as two rotated bars rather than a "✓" text glyph - Nunito (like most body
            // text faces) doesn't include a checkmark character, so a glyph would render as a
            // missing-tofu box instead of an actual check.
            CreateCheckmarkBar(checkmarkBg.transform, new Vector2(-22, -6), 46, -45f);
            CreateCheckmarkBar(checkmarkBg.transform, new Vector2(14, 10), 78, 45f);

            var headingText = UIBuilderHelpers.CreateText("HeadingText", panelGO.transform, "Maze solved!", new Vector2(0, 130), 46, UIBuilderHelpers.NunitoBlack);
            headingText.color = RollAndEscapePalette.White;

            var starDots = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var dotGO = new GameObject($"Star{i}", typeof(RectTransform), typeof(Image));
                dotGO.transform.SetParent(panelGO.transform, false);
                var dotRect = dotGO.GetComponent<RectTransform>();
                dotRect.anchorMin = dotRect.anchorMax = new Vector2(0.5f, 0.5f);
                dotRect.sizeDelta = new Vector2(46, 46);
                dotRect.anchoredPosition = new Vector2((i - 1) * 56f, 50);
                var dotImage = dotGO.GetComponent<Image>();
                dotImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                dotImage.color = RollAndEscapePalette.StarDim;
                starDots[i] = dotImage;
            }

            var timeText = UIBuilderHelpers.CreateText("TimeText", panelGO.transform, "Solved in 00:00", new Vector2(0, -30), 26);
            timeText.color = new Color(1f, 1f, 1f, 0.85f);

            var nextButton = UIBuilderHelpers.CreateButton("NextLevelButton", panelGO.transform, "Next maze →", new Vector2(0, -130), new Vector2(760, 96));
            nextButton.GetComponent<Image>().color = RollAndEscapePalette.White;
            var nextLabel = nextButton.GetComponentInChildren<Text>();
            nextLabel.color = RollAndEscapePalette.NextMazeButtonText;
            nextLabel.font = UIBuilderHelpers.NunitoBlack;

            var replayButton = UIBuilderHelpers.CreateButton("ReplayButton", panelGO.transform, "Play again", new Vector2(0, -250), new Vector2(760, 90));
            var replayImage = replayButton.GetComponent<Image>();
            replayImage.color = new Color(0f, 0f, 0f, 0f); // transparent fill - only the border reads, per the mockup's "Back to map" secondary-button style
            replayImage.sprite = UIBuilderHelpers.GenerateRoundedRectOutlineSprite("Assets/Art/CompleteSecondaryButtonBorder.png", 256, 32f, 4f, RollAndEscapePalette.White);
            var replayLabel = replayButton.GetComponentInChildren<Text>();
            replayLabel.color = RollAndEscapePalette.White;
            replayLabel.font = UIBuilderHelpers.NunitoBold;

            var ui = canvasGO.AddComponent<LevelCompleteUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("root").objectReferenceValue = panelGO;
            so.FindProperty("headingText").objectReferenceValue = headingText;
            so.FindProperty("timeText").objectReferenceValue = timeText;
            var starDotsProp = so.FindProperty("starDots");
            starDotsProp.arraySize = 3;
            for (int i = 0; i < 3; i++) starDotsProp.GetArrayElementAtIndex(i).objectReferenceValue = starDots[i];
            so.FindProperty("replayButton").objectReferenceValue = replayButton;
            so.FindProperty("nextLevelButton").objectReferenceValue = nextButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(LevelCompleteUIPrefabPath));
            var prefab = PrefabUtility.SaveAsPrefabAsset(canvasGO, LevelCompleteUIPrefabPath);
            Object.DestroyImmediate(canvasGO);
            return prefab;
        }

        private static void StretchToFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>One stroke of the checkmark badge - a thin rounded bar rotated in place.</summary>
        private static void CreateCheckmarkBar(Transform parent, Vector2 anchoredPosition, float length, float rotationDegrees)
        {
            var go = new GameObject("CheckmarkBar", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(10, length);
            rect.localRotation = Quaternion.Euler(0, 0, rotationDegrees);
            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            image.type = Image.Type.Sliced;
            image.color = RollAndEscapePalette.CheckmarkColor;
        }
    }
}
