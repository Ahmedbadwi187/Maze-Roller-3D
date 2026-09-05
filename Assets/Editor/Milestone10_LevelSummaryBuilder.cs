using System.IO;
using RollAndEscape.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEscape.EditorTools
{
    /// <summary>
    /// Level Summary screen (Assets/Scenes/LevelSummary.unity) - a scrollable list of all 100
    /// levels' real per-level star rating and best completion time, per the user's request for
    /// a "points per level" screen reachable from the home page's star chip. Reads live from
    /// LevelProgressService (LevelSummaryUI), never fabricated placeholder data.
    ///
    /// Run via the Unity menu: Roll & Escape -> Milestone 10 - Build Level Summary Scene.
    /// </summary>
    public static class Milestone10_LevelSummaryBuilder
    {
        private const string LevelSummaryScenePath = "Assets/Scenes/LevelSummary.unity";
        private const string SplashScenePath = "Assets/Scenes/Splash.unity";
        private const string LevelSelectScenePath = "Assets/Scenes/LevelSelect.unity";
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string SettingsScenePath = "Assets/Scenes/Settings.unity";

        [MenuItem("Roll & Escape/Milestone 10 - Build Level Summary Scene")]
        public static void Build()
        {
            Milestone9_PolishBuilder.Build();
            BuildLevelSummaryScene();
        }

        /// <summary>Builds just this scene without re-running the rest of the chain - callers
        /// that already rebuilt everything else (e.g. Milestone9's own Build()) should call
        /// this directly instead of Build() above, to avoid redundant work.</summary>
        public static void BuildLevelSummaryScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory(Path.GetDirectoryName(LevelSummaryScenePath));
            EditorSceneManager.SaveScene(scene, LevelSummaryScenePath);

            Milestone3_BallSceneBuilder.EnsureEventSystem();

            var canvasGO = new GameObject("LevelSummaryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvasGO.transform, false);
            UIBuilderHelpers.StretchToFill(background.GetComponent<RectTransform>());
            background.GetComponent<Image>().color = new Color32(0xF2, 0xF0, 0xF3, 0xFF);

            var topSafeArea = UIBuilderHelpers.CreateSafeArea(canvasGO.transform);

            var backButton = UIBuilderHelpers.CreateButton("BackButton", topSafeArea, "‹", Vector2.zero, new Vector2(72, 72));
            var backRect = backButton.GetComponent<RectTransform>();
            backRect.pivot = new Vector2(0f, 0.5f);
            UIBuilderHelpers.AnchorToTop(backRect, 0f, 70f);
            backRect.anchoredPosition = new Vector2(40f, backRect.anchoredPosition.y);
            backButton.GetComponent<Image>().sprite = UIBuilderHelpers.GenerateCircleSprite("Assets/Art/SummaryBackButtonBg.png", 128, RollAndEscapePalette.White);
            var backLabel = backButton.GetComponentInChildren<Text>();
            backLabel.fontSize = 40;
            backLabel.color = RollAndEscapePalette.BackButtonText;
            var backLoader = backButton.gameObject.AddComponent<LoadSceneOnClick>();
            var backLoaderSo = new SerializedObject(backLoader);
            backLoaderSo.FindProperty("sceneName").stringValue = "LevelSelect";
            backLoaderSo.ApplyModifiedPropertiesWithoutUndo();

            var title = UIBuilderHelpers.CreateText("Title", topSafeArea, "Your Progress", Vector2.zero, 40, UIBuilderHelpers.NunitoBlack);
            title.alignment = TextAnchor.MiddleLeft;
            title.color = RollAndEscapePalette.CardTitleText;
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.pivot = new Vector2(0f, 0.5f);
            UIBuilderHelpers.AnchorToTop(titleRect, 0f, 70f);
            titleRect.anchoredPosition = new Vector2(130f, titleRect.anchoredPosition.y);

            var (content, viewport) = CreateScrollList(canvasGO.transform);
            var rowTemplate = CreateRowTemplate(content);

            var summaryUI = canvasGO.AddComponent<LevelSummaryUI>();
            var so = new SerializedObject(summaryUI);
            so.FindProperty("content").objectReferenceValue = content;
            so.FindProperty("rowTemplate").objectReferenceValue = rowTemplate;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, LevelSummaryScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SplashScenePath, true),
                new EditorBuildSettingsScene(LevelSelectScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
                new EditorBuildSettingsScene(SettingsScenePath, true),
                new EditorBuildSettingsScene(LevelSummaryScenePath, true)
            };

            Debug.Log($"Milestone 10: built {LevelSummaryScenePath} with {RollAndEscape.Levels.LevelRepository.TotalLevels} " +
                      "level rows, registered in Build Settings.");
        }

        private static (RectTransform content, RectTransform viewport) CreateScrollList(Transform parent)
        {
            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            var viewportRect = viewportGO.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0.02f, 0.02f);
            viewportRect.anchorMax = new Vector2(0.98f, 0.9f);
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewportGO.GetComponent<Mask>().showMaskGraphic = false;

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;

            var scrollRectGO = new GameObject("SummaryScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollRectGO.transform.SetParent(parent, false);
            UIBuilderHelpers.StretchToFill(scrollRectGO.GetComponent<RectTransform>());
            viewportGO.transform.SetParent(scrollRectGO.transform, false);

            var scrollRect = scrollRectGO.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            return (contentRect, viewportRect);
        }

        private static LevelSummaryRow CreateRowTemplate(Transform parent)
        {
            var go = new GameObject("RowTemplate", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1020, 80);
            go.GetComponent<Image>().sprite = UIBuilderHelpers.GenerateRoundedRectSprite("Assets/Art/SummaryRowBg.png", 256, 24f, RollAndEscapePalette.White);
            go.GetComponent<Image>().color = Color.white;

            var levelNumberText = UIBuilderHelpers.CreateText("LevelNumber", go.transform, "Level 1", new Vector2(-380, 0), 28, UIBuilderHelpers.NunitoBold);
            levelNumberText.alignment = TextAnchor.MiddleLeft;
            levelNumberText.color = RollAndEscapePalette.CardTitleText;
            var levelNumberRect = levelNumberText.GetComponent<RectTransform>();
            levelNumberRect.pivot = new Vector2(0f, 0.5f);
            levelNumberRect.anchorMin = levelNumberRect.anchorMax = new Vector2(0.5f, 0.5f);
            levelNumberRect.anchoredPosition = new Vector2(-490, 0);
            levelNumberRect.sizeDelta = new Vector2(220, 70);

            var starDots = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                var dotGO = new GameObject($"Star{i}", typeof(RectTransform), typeof(Image));
                dotGO.transform.SetParent(go.transform, false);
                var dotRect = dotGO.GetComponent<RectTransform>();
                dotRect.anchorMin = dotRect.anchorMax = new Vector2(0.5f, 0.5f);
                dotRect.sizeDelta = new Vector2(20, 20);
                dotRect.anchoredPosition = new Vector2(-100 + i * 24f, 0);
                var dotImage = dotGO.GetComponent<Image>();
                dotImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                dotImage.color = RollAndEscapePalette.StarDim;
                starDots[i] = dotImage;
            }

            var timeText = UIBuilderHelpers.CreateText("TimeText", go.transform, "Not played yet", new Vector2(300, 0), 22);
            timeText.alignment = TextAnchor.MiddleRight;
            timeText.color = RollAndEscapePalette.CardSubtext;
            var timeRect = timeText.GetComponent<RectTransform>();
            timeRect.pivot = new Vector2(1f, 0.5f);
            timeRect.anchorMin = timeRect.anchorMax = new Vector2(0.5f, 0.5f);
            timeRect.anchoredPosition = new Vector2(490, 0);
            timeRect.sizeDelta = new Vector2(260, 70);

            var row = go.AddComponent<LevelSummaryRow>();
            var so = new SerializedObject(row);
            so.FindProperty("levelNumberText").objectReferenceValue = levelNumberText;
            var starDotsProp = so.FindProperty("starDots");
            starDotsProp.arraySize = starDots.Length;
            for (int i = 0; i < starDots.Length; i++) starDotsProp.GetArrayElementAtIndex(i).objectReferenceValue = starDots[i];
            so.FindProperty("timeText").objectReferenceValue = timeText;
            so.ApplyModifiedPropertiesWithoutUndo();

            return row;
        }
    }
}
