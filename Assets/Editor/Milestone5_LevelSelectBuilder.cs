using System.IO;
using MazeRoller3D.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MazeRoller3D.EditorTools
{
    /// <summary>
    /// Milestone 5 deliverable: a scrollable Level Select grid (Assets/Scenes/LevelSelect.unity)
    /// reading unlock/star state from LevelProgressService, and registers both LevelSelect and
    /// Game in Build Settings so SceneManager.LoadScene("Game") (called from
    /// LevelSelectUI.OnLevelSelected) resolves at runtime.
    ///
    /// Run via the Unity menu: Maze Roller 3D -> Milestone 5 - Build Level Select Scene.
    /// </summary>
    public static class Milestone5_LevelSelectBuilder
    {
        private const string LevelSelectScenePath = "Assets/Scenes/LevelSelect.unity";
        private const string GameScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Maze Roller 3D/Milestone 5 - Build Level Select Scene")]
        public static void Build()
        {
            // Make sure Game.unity exists (milestone 2-4 builders create it) before we
            // register both scenes in Build Settings.
            if (!File.Exists(GameScenePath))
            {
                Milestone4_WinConditionBuilder.Build();
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory(Path.GetDirectoryName(LevelSelectScenePath));
            EditorSceneManager.SaveScene(scene, LevelSelectScenePath);

            Milestone3_BallSceneBuilder.EnsureEventSystem();

            var canvasGO = new GameObject("LevelSelectCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvasGO.transform, false);
            StretchToFill(background.GetComponent<RectTransform>());
            background.GetComponent<Image>().color = new Color(0.97f, 0.97f, 0.98f, 1f);

            var (scrollRectGO, content) = CreateScrollGrid(canvasGO.transform);
            var buttonTemplate = CreateButtonTemplate(content);

            var settingsButton = UIBuilderHelpers.CreateButton("SettingsButton", canvasGO.transform, "Settings", new Vector2(0, 850), new Vector2(260, 90));
            settingsButton.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("Settings"));

            var levelSelectUI = canvasGO.AddComponent<LevelSelectUI>();
            var so = new SerializedObject(levelSelectUI);
            so.FindProperty("content").objectReferenceValue = content;
            so.FindProperty("buttonTemplate").objectReferenceValue = buttonTemplate;
            so.FindProperty("gameSceneName").stringValue = "Game";
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, LevelSelectScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(LevelSelectScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };

            Debug.Log($"Milestone 5: built {LevelSelectScenePath} with {MazeRoller3D.Levels.LevelRepository.TotalLevels} " +
                      "level tiles, registered LevelSelect + Game in Build Settings.");
        }

        private static (GameObject scrollRectGO, RectTransform content) CreateScrollGrid(Transform parent)
        {
            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGO.transform.SetParent(parent, false);
            var viewportRect = viewportGO.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0.05f, 0.05f);
            viewportRect.anchorMax = new Vector2(0.95f, 0.9f);
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f); // Mask needs a raycast-able graphic
            viewportGO.GetComponent<Mask>().showMaskGraphic = false;

            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;

            var grid = contentGO.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(180, 180);
            grid.spacing = new Vector2(20, 20);
            grid.padding = new RectOffset(20, 20, 20, 20);

            var fitter = contentGO.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRectGO = new GameObject("LevelScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollRectGO.transform.SetParent(parent, false);
            var scrollViewRect = scrollRectGO.GetComponent<RectTransform>();
            StretchToFill(scrollViewRect);
            viewportGO.transform.SetParent(scrollRectGO.transform, false);

            var scrollRect = scrollRectGO.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            return (scrollRectGO, contentRect);
        }

        private static LevelSelectButton CreateButtonTemplate(Transform parent)
        {
            var go = new GameObject("LevelButtonTemplate", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = Color.white;

            var levelNumberText = CreateText("LevelNumber", go.transform, "1", new Vector2(0, 20), 56);
            levelNumberText.color = Color.black;

            var starsText = CreateText("Stars", go.transform, "", new Vector2(0, -40), 28);
            starsText.color = new Color(0.85f, 0.65f, 0.13f);

            var button = go.AddComponent<LevelSelectButton>();
            var so = new SerializedObject(button);
            so.FindProperty("button").objectReferenceValue = go.GetComponent<Button>();
            so.FindProperty("background").objectReferenceValue = go.GetComponent<Image>();
            so.FindProperty("levelNumberText").objectReferenceValue = levelNumberText;
            so.FindProperty("starsText").objectReferenceValue = starsText;
            so.ApplyModifiedPropertiesWithoutUndo();

            return button;
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
            rect.sizeDelta = new Vector2(160, 60);

            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            return text;
        }

        private static void StretchToFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
