using System.IO;
using RollAndEscape.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RollAndEscape.EditorTools
{
    /// <summary>
    /// Milestone 7 deliverable: a Settings scene (Assets/Scenes/Settings.unity) with sound,
    /// music, and control-scheme toggles plus Restore Purchases / Remove Ads buttons, all wired
    /// to SettingsUI. Registers Settings in Build Settings alongside LevelSelect and Game.
    ///
    /// Redesigned per the approved "Buze" mockup (Claude Design project d6305a3a,
    /// Buze.dc.html) - was a plain centered list with default Unity toggles and a bottom "Back"
    /// button; now a back-chevron header (matching the rest of the app) and a left-aligned
    /// checkbox list, restyled outline buttons.
    ///
    /// Run via the Unity menu: Roll & Escape -> Milestone 7 - Build Settings Scene.
    /// </summary>
    public static class Milestone7_SettingsBuilder
    {
        private const string SettingsScenePath = "Assets/Scenes/Settings.unity";
        private const string LevelSelectScenePath = "Assets/Scenes/LevelSelect.unity";
        private const string GameScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Roll & Escape/Milestone 7 - Build Settings Scene")]
        public static void Build()
        {
            // Always rebuild fresh - see Milestone5's Build() for why "only if missing" was
            // wrong (it let stale scene state accumulate across a growing milestone chain).
            Milestone5_LevelSelectBuilder.Build();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsScenePath));
            EditorSceneManager.SaveScene(scene, SettingsScenePath);

            Milestone3_BallSceneBuilder.EnsureEventSystem();

            var canvasGO = new GameObject("SettingsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

            // Back-chevron circle + title, matching the small round icon-button language used
            // on Level Select and in-game (was a wide "Back" button pinned to the bottom).
            var backButton = UIBuilderHelpers.CreateButton("BackButton", topSafeArea, "‹", Vector2.zero, new Vector2(72, 72));
            var backRect = backButton.GetComponent<RectTransform>();
            backRect.pivot = new Vector2(0f, 0.5f);
            UIBuilderHelpers.AnchorToTop(backRect, 0f, 70f);
            backRect.anchoredPosition = new Vector2(40f, backRect.anchoredPosition.y);
            backButton.GetComponent<Image>().sprite = UIBuilderHelpers.GenerateCircleSprite("Assets/Art/BackButtonBg.png", 128, RollAndEscapePalette.White);
            var backLabel = backButton.GetComponentInChildren<Text>();
            backLabel.fontSize = 40;
            backLabel.color = RollAndEscapePalette.BackButtonText;
            var backLoader = backButton.gameObject.AddComponent<LoadSceneOnClick>();
            var backLoaderSo = new SerializedObject(backLoader);
            backLoaderSo.FindProperty("sceneName").stringValue = "LevelSelect";
            backLoaderSo.ApplyModifiedPropertiesWithoutUndo();

            var title = UIBuilderHelpers.CreateText("Title", topSafeArea, "Settings", Vector2.zero, 44, UIBuilderHelpers.NunitoBlack);
            title.alignment = TextAnchor.MiddleLeft;
            title.color = RollAndEscapePalette.CardTitleText;
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.pivot = new Vector2(0f, 0.5f);
            UIBuilderHelpers.AnchorToTop(titleRect, 0f, 70f);
            titleRect.anchoredPosition = new Vector2(130f, titleRect.anchoredPosition.y);

            var soundToggle = CreateCheckboxRow("SoundToggle", topSafeArea, "Sound", 220f);
            var musicToggle = CreateCheckboxRow("MusicToggle", topSafeArea, "Music", 310f);
            var controlSchemeToggle = CreateCheckboxRow("ControlSchemeToggle", topSafeArea, "Joystick (off = Tilt)", 400f);

            var restoreButton = CreateOutlineButton("RestorePurchasesButton", topSafeArea, "Restore Purchases", 540f);
            var removeAdsButton = CreateOutlineButton("RemoveAdsButton", topSafeArea, "Remove Ads", 660f);

            var settingsUI = canvasGO.AddComponent<SettingsUI>();
            var so = new SerializedObject(settingsUI);
            so.FindProperty("soundToggle").objectReferenceValue = soundToggle;
            so.FindProperty("musicToggle").objectReferenceValue = musicToggle;
            so.FindProperty("controlSchemeToggle").objectReferenceValue = controlSchemeToggle;
            so.FindProperty("restorePurchasesButton").objectReferenceValue = restoreButton;
            so.FindProperty("removeAdsButton").objectReferenceValue = removeAdsButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SettingsScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(LevelSelectScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
                new EditorBuildSettingsScene(SettingsScenePath, true)
            };

            Debug.Log($"Milestone 7: built {SettingsScenePath} with sound/music/control-scheme toggles, registered in Build Settings.");
        }

        /// <summary>A left-aligned checkbox + label row, matching the mockup's small square
        /// checkbox style (built on UIBuilderHelpers.CreateToggle's existing checkbox visuals,
        /// just recolored/repositioned rather than the old default-Unity-toggle look).</summary>
        private static Toggle CreateCheckboxRow(string name, Transform parent, string label, float insetFromTop)
        {
            var toggle = UIBuilderHelpers.CreateToggle(name, parent, label, Vector2.zero);
            var rect = toggle.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0f, 0.5f);
            UIBuilderHelpers.AnchorToTop(rect, 0f, insetFromTop);
            rect.anchoredPosition = new Vector2(70f, rect.anchoredPosition.y);

            // CreateToggle's Background child is left-pivoted at the toggle's own origin
            // already, so no extra repositioning needed there - just recolor the checkmark to
            // match the app palette instead of the old plain green.
            var checkmark = toggle.graphic as Image;
            if (checkmark != null) checkmark.color = RollAndEscapePalette.CardAccentPurple;

            var labelText = toggle.GetComponentInChildren<Text>();
            labelText.color = RollAndEscapePalette.CardTitleText;
            labelText.fontSize = 30;
            labelText.font = UIBuilderHelpers.NunitoBold;

            return toggle;
        }

        /// <summary>White-fill, thin-bordered button - the mockup's Restore Purchases/Remove
        /// Ads style (distinct from the solid-purple primary buttons used elsewhere).</summary>
        private static Button CreateOutlineButton(string name, Transform parent, string label, float insetFromTop)
        {
            var button = UIBuilderHelpers.CreateButton(name, parent, label, Vector2.zero, new Vector2(920, 90));
            var rect = button.GetComponent<RectTransform>();
            UIBuilderHelpers.AnchorToTop(rect, 0.5f, insetFromTop);

            var image = button.GetComponent<Image>();
            image.color = Color.white;
            image.sprite = UIBuilderHelpers.GenerateRoundedRectSprite($"Assets/Art/SettingsOutlineFill_{name}.png", 256, 20f, RollAndEscapePalette.White);

            var text = button.GetComponentInChildren<Text>();
            text.color = RollAndEscapePalette.CardTitleText;
            text.font = UIBuilderHelpers.NunitoBold;
            text.fontSize = 28;

            return button;
        }
    }
}
