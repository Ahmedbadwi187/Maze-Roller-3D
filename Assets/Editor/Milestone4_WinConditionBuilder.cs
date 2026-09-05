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

            var panelGO = CreateFullScreenPanel("Panel", canvasGO.transform, new Color(0f, 0f, 0f, 0.6f));
            var timeText = CreateText("TimeText", panelGO.transform, "00:00", new Vector2(0, 150), 64);
            var starsText = CreateText("StarsText", panelGO.transform, "", new Vector2(0, 40), 48);
            var replayButton = CreateButton("ReplayButton", panelGO.transform, "Replay", new Vector2(-150, -150));
            var nextButton = CreateButton("NextLevelButton", panelGO.transform, "Next Level", new Vector2(150, -150));

            var ui = canvasGO.AddComponent<LevelCompleteUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("root").objectReferenceValue = panelGO;
            so.FindProperty("timeText").objectReferenceValue = timeText;
            so.FindProperty("starsText").objectReferenceValue = starsText;
            so.FindProperty("replayButton").objectReferenceValue = replayButton;
            so.FindProperty("nextLevelButton").objectReferenceValue = nextButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(LevelCompleteUIPrefabPath));
            var prefab = PrefabUtility.SaveAsPrefabAsset(canvasGO, LevelCompleteUIPrefabPath);
            Object.DestroyImmediate(canvasGO);
            return prefab;
        }

        private static GameObject CreateFullScreenPanel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static Text CreateText(string name, Transform parent, string content, Vector2 anchoredPosition, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(600, 100);

            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(220, 80);
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);

            var label_ = CreateText(name + "_Label", go.transform, label, Vector2.zero, 36);
            label_.color = Color.black;

            return go.GetComponent<Button>();
        }
    }
}
