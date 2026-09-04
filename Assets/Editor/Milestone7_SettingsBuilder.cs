using System.IO;
using MazeRoller3D.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MazeRoller3D.EditorTools
{
    /// <summary>
    /// Milestone 7 deliverable: a Settings scene (Assets/Scenes/Settings.unity) with sound,
    /// music, and control-scheme toggles plus stubbed Restore Purchases / Remove Ads buttons
    /// (real IAP is milestone 8), all wired to SettingsUI. Registers Settings in Build
    /// Settings alongside LevelSelect and Game.
    ///
    /// Run via the Unity menu: Maze Roller 3D -> Milestone 7 - Build Settings Scene.
    /// </summary>
    public static class Milestone7_SettingsBuilder
    {
        private const string SettingsScenePath = "Assets/Scenes/Settings.unity";
        private const string LevelSelectScenePath = "Assets/Scenes/LevelSelect.unity";
        private const string GameScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Maze Roller 3D/Milestone 7 - Build Settings Scene")]
        public static void Build()
        {
            if (!File.Exists(LevelSelectScenePath))
            {
                Milestone5_LevelSelectBuilder.Build();
            }

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
            background.GetComponent<Image>().color = new Color(0.97f, 0.97f, 0.98f, 1f);

            UIBuilderHelpers.CreateText("Title", canvasGO.transform, "Settings", new Vector2(0, 700), 64);

            var soundToggle = UIBuilderHelpers.CreateToggle("SoundToggle", canvasGO.transform, "Sound", new Vector2(0, 400));
            var musicToggle = UIBuilderHelpers.CreateToggle("MusicToggle", canvasGO.transform, "Music", new Vector2(0, 300));
            var controlSchemeToggle = UIBuilderHelpers.CreateToggle("ControlSchemeToggle", canvasGO.transform, "Joystick (off = Tilt)", new Vector2(0, 200));

            var restoreButton = UIBuilderHelpers.CreateButton("RestorePurchasesButton", canvasGO.transform, "Restore Purchases", new Vector2(0, 50), new Vector2(400, 80));
            var removeAdsButton = UIBuilderHelpers.CreateButton("RemoveAdsButton", canvasGO.transform, "Remove Ads", new Vector2(0, -50), new Vector2(400, 80));
            var backButton = UIBuilderHelpers.CreateButton("BackButton", canvasGO.transform, "Back", new Vector2(0, -700), new Vector2(300, 90));
            backButton.onClick.AddListener(() => SceneManager.LoadScene("LevelSelect"));

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
    }
}
