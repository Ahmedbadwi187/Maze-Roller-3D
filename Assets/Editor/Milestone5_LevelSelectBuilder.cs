using System.IO;
using RollAndEscape.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEscape.EditorTools
{
    /// <summary>
    /// Milestone 5 deliverable: the Level Select "snake path" map (Assets/Scenes/LevelSelect.unity)
    /// reading unlock/star state from LevelProgressService, and registers both LevelSelect and
    /// Game in Build Settings so SceneManager.LoadScene("Game") (called from
    /// LevelSelectUI.OnLevelSelected) resolves at runtime.
    ///
    /// Redesigned per the approved "Buze" mockup (Claude Design project d6305a3a,
    /// Buze.dc.html) - was a plain grid of square white tiles; now a winding vertical path of
    /// circular nodes (Candy-Crush-style), a purple/pink/gold palette, and Nunito type.
    ///
    /// Run via the Unity menu: Roll & Escape -> Milestone 5 - Build Level Select Scene.
    /// </summary>
    public static class Milestone5_LevelSelectBuilder
    {
        private const string LevelSelectScenePath = "Assets/Scenes/LevelSelect.unity";
        private const string GameScenePath = "Assets/Scenes/Game.unity";

        private const string HomeBackgroundPath = "Assets/Art/HomeBackground.png";
        private const string NodeDoneSpritePath = "Assets/Art/NodeDone.png";
        private const string NodeCurrentSpritePath = "Assets/Art/NodeCurrent.png";
        private const string NodeLockedSpritePath = "Assets/Art/NodeLocked.png";
        private const string CurrentRingSpritePath = "Assets/Art/CurrentRing.png";
        private const string CardSpritePath = "Assets/Art/ContinueCard.png";

        private const float NodeDiameter = 92f;

        [MenuItem("Roll & Escape/Milestone 5 - Build Level Select Scene")]
        public static void Build()
        {
            // Always rebuild Game.unity fresh (not just "if missing") - milestones 6-9 keep
            // adding to it, and a stale Game.unity left over from an earlier partial run was
            // exactly the cause of a real bug (a later step expecting GameObjects this scene
            // no longer had). Matches how milestones 2-4 already always rebuild unconditionally.
            Milestone4_WinConditionBuilder.Build();

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

            // Soft pink-purple-to-cream vertical gradient, per the mockup's Home screen.
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvasGO.transform, false);
            StretchToFill(background.GetComponent<RectTransform>());
            var backgroundImage = background.GetComponent<Image>();
            backgroundImage.sprite = UIBuilderHelpers.GenerateSprite(HomeBackgroundPath, 270, 480, (x, y) =>
            {
                float t = UIBuilderHelpers.LinearGradientT(x, y, 270, 480, 180f);
                return UIBuilderHelpers.LerpStops(t,
                    (0f, RollAndEscapePalette.HomeBgTop),
                    (0.3f, RollAndEscapePalette.HomeBgMid),
                    (0.55f, RollAndEscapePalette.HomeBgBottom));
            });
            backgroundImage.color = Color.white;
            backgroundImage.preserveAspect = false;

            var topSafeArea = UIBuilderHelpers.CreateSafeArea(canvasGO.transform);

            var eyebrow = UIBuilderHelpers.CreateText("Eyebrow", topSafeArea, "ROLL & ESCAPE", Vector2.zero, 22, UIBuilderHelpers.NunitoBlack);
            eyebrow.color = RollAndEscapePalette.EyebrowText;
            eyebrow.alignment = TextAnchor.MiddleLeft;
            var eyebrowRect = eyebrow.GetComponent<RectTransform>();
            eyebrowRect.pivot = new Vector2(0f, 0.5f); // left-pivot so anchoredPosition.x is a left-edge inset, not a center offset - a center-pivot box here would hang half its width off the left edge of the screen
            UIBuilderHelpers.AnchorToTop(eyebrowRect, 0f, 58f);
            eyebrowRect.anchoredPosition = new Vector2(40f, eyebrowRect.anchoredPosition.y);

            // "Player One" under the eyebrow, per the mockup - no player-profile system exists
            // (single-device save, no accounts), so this is a static label rather than backed
            // by real data, same spirit as the splash screen's static "100 MAZES TO SOLVE".
            var playerName = UIBuilderHelpers.CreateText("PlayerName", topSafeArea, "Player One", Vector2.zero, 40, UIBuilderHelpers.NunitoBlack);
            playerName.color = RollAndEscapePalette.PlayerNameText;
            playerName.alignment = TextAnchor.MiddleLeft;
            var playerNameRect = playerName.GetComponent<RectTransform>();
            playerNameRect.pivot = new Vector2(0f, 0.5f);
            UIBuilderHelpers.AnchorToTop(playerNameRect, 0f, 108f);
            playerNameRect.anchoredPosition = new Vector2(40f, playerNameRect.anchoredPosition.y);

            // Small circular gear-style icon button (was a wide "Settings" text pill) - per the
            // design update, matching the small round icon-button language used elsewhere
            // (back-chevron buttons). Drawn as a ring + center dot rather than an emoji glyph -
            // same reasoning as the Level Complete checkmark: the bundled font may not carry a
            // gear character, so a text glyph risks a missing-tofu box.
            var settingsButton = UIBuilderHelpers.CreateButton("SettingsButton", topSafeArea, "", Vector2.zero, new Vector2(72, 72));
            UIBuilderHelpers.AnchorToTop(settingsButton.GetComponent<RectTransform>(), 1f, 82f); // vertically centered against the two-line eyebrow+"Player One" header block
            settingsButton.GetComponent<RectTransform>().anchoredPosition += new Vector2(-56f, 0f);
            var settingsButtonImage = settingsButton.GetComponent<Image>();
            settingsButtonImage.sprite = UIBuilderHelpers.GenerateCircleSprite("Assets/Art/SettingsButtonBg.png", 128, RollAndEscapePalette.White);
            settingsButtonImage.color = Color.white;
            var gearGO = new GameObject("GearIcon", typeof(RectTransform), typeof(Image));
            gearGO.transform.SetParent(settingsButton.transform, false);
            var gearRect = gearGO.GetComponent<RectTransform>();
            gearRect.anchorMin = gearRect.anchorMax = new Vector2(0.5f, 0.5f);
            gearRect.sizeDelta = new Vector2(34, 34);
            // A real toothed-gear silhouette (angular modulation: the outer radius alternates
            // between a "tooth" and "valley" radius every 1/16 turn, with a hollow center hole)
            // - the earlier plain ring+dot didn't read as a gear at all.
            gearGO.GetComponent<Image>().sprite = UIBuilderHelpers.GenerateSprite("Assets/Art/GearIcon.png", 64, 64, (x, y) =>
            {
                const int teeth = 8;
                const float rValley = 19f, rTooth = 27f, rHole = 9f;
                var center = new Vector2(32f, 32f);
                var point = new Vector2(x + 0.5f, y + 0.5f) - center;
                float dist = point.magnitude;

                float angle = Mathf.Atan2(point.y, point.x);
                float sector = (angle + Mathf.PI) / (2f * Mathf.PI) * teeth;
                float frac = sector - Mathf.Floor(sector);
                float rOuter = frac < 0.5f ? rTooth : rValley;

                bool filled = dist <= rOuter && dist >= rHole;
                var transparent = new Color32(0, 0, 0, 0);
                return filled ? (Color32)RollAndEscapePalette.BackButtonText : transparent;
            });

            var settingsLoader = settingsButton.gameObject.AddComponent<LoadSceneOnClick>();
            var settingsLoaderSo = new SerializedObject(settingsLoader);
            settingsLoaderSo.FindProperty("sceneName").stringValue = "Settings";
            settingsLoaderSo.ApplyModifiedPropertiesWithoutUndo();

            // Total-stars/points chip, left of the settings button - the running total across
            // all levels, so it visibly grows as the player completes more mazes (the "develop
            // point achieve when move from level to another" ask).
            // Clickable - opens the Level Summary screen (per-level stars/best time list), the
            // "level achievement clickable in home page" ask.
            var starsChipGO = new GameObject("StarsChip", typeof(RectTransform), typeof(Image), typeof(Button));
            starsChipGO.transform.SetParent(topSafeArea, false);
            var starsChipRect = starsChipGO.GetComponent<RectTransform>();
            starsChipRect.sizeDelta = new Vector2(150, 64);
            UIBuilderHelpers.AnchorToTop(starsChipRect, 1f, 82f); // vertically centered against the two-line eyebrow+"Player One" header block
            starsChipRect.anchoredPosition += new Vector2(-190f, 0f);
            starsChipGO.GetComponent<Image>().sprite = UIBuilderHelpers.GenerateRoundedRectSprite("Assets/Art/StarsChipBg.png", 128, 64f, RollAndEscapePalette.White);
            starsChipGO.GetComponent<Image>().type = Image.Type.Sliced; // 150x64 pill shape - Simple/stretch would squash the rounded caps flat
            var starsChipLoader = starsChipGO.AddComponent<LoadSceneOnClick>();
            var starsChipLoaderSo = new SerializedObject(starsChipLoader);
            starsChipLoaderSo.FindProperty("sceneName").stringValue = "LevelSummary";
            starsChipLoaderSo.ApplyModifiedPropertiesWithoutUndo();

            var starsChipDot = new GameObject("StarDot", typeof(RectTransform), typeof(Image));
            starsChipDot.transform.SetParent(starsChipGO.transform, false);
            var starsChipDotRect = starsChipDot.GetComponent<RectTransform>();
            starsChipDotRect.anchorMin = starsChipDotRect.anchorMax = new Vector2(0f, 0.5f);
            starsChipDotRect.pivot = new Vector2(0f, 0.5f);
            starsChipDotRect.anchoredPosition = new Vector2(16, 0);
            starsChipDotRect.sizeDelta = new Vector2(28, 28);
            // Glossy gradient sphere matching the mockup's exact recipe (radial-gradient(circle
            // at 35% 30%, highlight, base)) - same treatment as the splash/maze ball, not a
            // flat tinted circle (an earlier flat-circle fix over-corrected: the built-in
            // Knob.psd's issue was its own muddy shading, not the idea of shading itself).
            starsChipDot.GetComponent<Image>().sprite = UIBuilderHelpers.GenerateSphereSprite(
                "Assets/Art/StarChipDot.png", 64, RollAndEscapePalette.BallHighlight, RollAndEscapePalette.BallBase, new Vector2(0.35f, 0.30f));
            starsChipDot.GetComponent<Image>().color = Color.white;

            var totalStarsText = UIBuilderHelpers.CreateText("StarsCount", starsChipGO.transform, "0", new Vector2(0, 0), 26, UIBuilderHelpers.NunitoBlack);
            totalStarsText.color = RollAndEscapePalette.StarChipText;
            totalStarsText.alignment = TextAnchor.MiddleLeft;
            totalStarsText.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
            totalStarsText.GetComponent<RectTransform>().anchoredPosition = new Vector2(54, 0);
            totalStarsText.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 64);

            var (continueCardGO, continueLevelText, continueSubtitleText, continueButton) = CreateContinueCard(topSafeArea);

            var sectionRow = new GameObject("SectionRow", typeof(RectTransform));
            sectionRow.transform.SetParent(topSafeArea, false);
            var sectionRect = sectionRow.GetComponent<RectTransform>();
            sectionRect.sizeDelta = new Vector2(1000, 60);
            UIBuilderHelpers.AnchorToTop(sectionRect, 0.5f, 510f); // extra breathing room below the continue card, per feedback that it felt cramped

            var sectionTitle = UIBuilderHelpers.CreateText("SectionTitle", sectionRow.transform, "Level map", new Vector2(-260, 0), 32, UIBuilderHelpers.NunitoBlack);
            sectionTitle.color = RollAndEscapePalette.SectionTitle;
            sectionTitle.alignment = TextAnchor.MiddleLeft;

            var completedCountText = UIBuilderHelpers.CreateText("CompletedCount", sectionRow.transform, "0 / 100 cleared", new Vector2(260, 0), 24);
            completedCountText.color = RollAndEscapePalette.SectionCount;
            completedCountText.alignment = TextAnchor.MiddleRight;

            var (scrollRect, content, viewport) = CreateSnakePathScroll(canvasGO.transform);
            var buttonTemplate = CreateNodeTemplate(content);

            var levelSelectUI = canvasGO.AddComponent<LevelSelectUI>();
            var so = new SerializedObject(levelSelectUI);
            so.FindProperty("content").objectReferenceValue = content;
            so.FindProperty("buttonTemplate").objectReferenceValue = buttonTemplate;
            so.FindProperty("gameSceneName").stringValue = "Game";
            so.FindProperty("continueLevelText").objectReferenceValue = continueLevelText;
            so.FindProperty("continueSubtitleText").objectReferenceValue = continueSubtitleText;
            so.FindProperty("continueButton").objectReferenceValue = continueButton;
            so.FindProperty("completedCountText").objectReferenceValue = completedCountText;
            so.FindProperty("totalStarsText").objectReferenceValue = totalStarsText;
            so.FindProperty("scrollRect").objectReferenceValue = scrollRect;
            so.FindProperty("viewport").objectReferenceValue = viewport;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, LevelSelectScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(LevelSelectScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };

            Debug.Log($"Milestone 5: built {LevelSelectScenePath} with {RollAndEscape.Levels.LevelRepository.TotalLevels} " +
                      "level nodes on the snake-path map, registered LevelSelect + Game in Build Settings.");
        }

        private static (GameObject card, Text levelText, Text subtitleText, Button continueButton) CreateContinueCard(Transform parent)
        {
            var cardGO = new GameObject("ContinueCard", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            cardGO.transform.SetParent(parent, false);
            var cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(1000, 260);
            UIBuilderHelpers.AnchorToTop(cardRect, 0.5f, 290f); // clears the two-line eyebrow+"Player One" header above it
            var cardImage = cardGO.GetComponent<Image>();
            cardImage.sprite = UIBuilderHelpers.GenerateRoundedRectSprite(CardSpritePath, 256, 44f, RollAndEscapePalette.White);
            cardImage.type = Image.Type.Sliced; // this card is 1000x260 - Simple/stretch would squash the round corners flat

            // Decorative soft-green circle peeking from the top-right corner, per the mockup -
            // RectMask2D on the card clips it to the card's bounds (a rectangular clip, not
            // perfectly following the rounded corners, but close enough at this scale).
            var blobGO = new GameObject("DecorativeBlob", typeof(RectTransform), typeof(Image));
            blobGO.transform.SetParent(cardGO.transform, false);
            var blobRect = blobGO.GetComponent<RectTransform>();
            blobRect.anchorMin = blobRect.anchorMax = new Vector2(1f, 1f);
            blobRect.anchoredPosition = new Vector2(30, 30);
            blobRect.sizeDelta = new Vector2(240, 240);
            blobGO.GetComponent<Image>().sprite = UIBuilderHelpers.GenerateCircleSprite("Assets/Art/ContinueCardBlob.png", 128, new Color32(0xCC, 0xE3, 0xCE, 0x59));

            // Card is 1000 wide (half-width 500); left-pivot every left-aligned label here and
            // set anchoredPosition.x to a left-edge inset directly - a center-pivot box (the
            // CreateText default) would need its offset to account for half its own width too,
            // which is exactly the bug that made the eyebrow header text run off-screen above.
            const float leftInset = -440f;

            var eyebrow = UIBuilderHelpers.CreateText("LevelEyebrow", cardGO.transform, "LEVEL 1", new Vector2(leftInset, 85), 22, UIBuilderHelpers.NunitoBlack);
            eyebrow.color = RollAndEscapePalette.CardAccentPurple;
            eyebrow.alignment = TextAnchor.MiddleLeft;
            eyebrow.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);

            var title = UIBuilderHelpers.CreateText("Title", cardGO.transform, "Roll to the exit", new Vector2(leftInset, 35), 42, UIBuilderHelpers.NunitoBlack);
            title.color = RollAndEscapePalette.CardTitleText;
            title.alignment = TextAnchor.MiddleLeft;
            title.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);

            var subtitle = UIBuilderHelpers.CreateText("Subtitle", cardGO.transform, "", new Vector2(leftInset, -15), 24);
            subtitle.color = RollAndEscapePalette.CardSubtext;
            subtitle.alignment = TextAnchor.MiddleLeft;
            subtitle.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);

            var continueButton = UIBuilderHelpers.CreateButton("ContinueButton", cardGO.transform, "Continue solving", new Vector2(0, -80), new Vector2(880, 90));
            var buttonImage = continueButton.GetComponent<Image>();
            buttonImage.sprite = null;
            buttonImage.color = RollAndEscapePalette.ContinueButtonBg;
            var buttonLabel = continueButton.GetComponentInChildren<Text>();
            buttonLabel.color = RollAndEscapePalette.White;
            buttonLabel.font = UIBuilderHelpers.NunitoBlack;

            return (cardGO, eyebrow, subtitle, continueButton);
        }

        private static (ScrollRect scrollRect, RectTransform content, RectTransform viewport) CreateSnakePathScroll(Transform parent)
        {
            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            var viewportRect = viewportGO.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0.02f, 0.02f);
            viewportRect.anchorMax = new Vector2(0.98f, 0.70f); // below the header + continue card + section row (nudged down to match the extra card/section spacing)
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f); // Mask needs a raycast-able graphic
            viewportGO.GetComponent<Mask>().showMaskGraphic = false;

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            // Height is set at runtime by LevelSelectUI once it knows the real level count - no
            // layout group here at all, since each node's left/center/right alignment varies per
            // level and neither GridLayoutGroup nor VerticalLayoutGroup can express a zig-zag.

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

            return (scrollRect, contentRect, viewportRect);
        }

        private static LevelSelectButton CreateNodeTemplate(Transform parent)
        {
            var go = new GameObject("LevelNodeTemplate", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(NodeDiameter, NodeDiameter);

            var doneSprite = UIBuilderHelpers.GenerateSprite(NodeDoneSpritePath, 128, 128, (x, y) =>
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(64f, 64f));
                if (dist > 64f) return new Color32(0, 0, 0, 0);
                float t = UIBuilderHelpers.LinearGradientT(x, y, 128, 128, 160f);
                return UIBuilderHelpers.LerpStops(t, (0f, RollAndEscapePalette.NodeDoneTop), (1f, RollAndEscapePalette.NodeDoneBottom));
            });
            var currentSprite = UIBuilderHelpers.GenerateCircleSprite(NodeCurrentSpritePath, 128, RollAndEscapePalette.White);
            var lockedSprite = UIBuilderHelpers.GenerateCircleSprite(NodeLockedSpritePath, 128, RollAndEscapePalette.NodeLockedBg);

            var background = go.GetComponent<Image>();
            background.sprite = doneSprite;

            var numberText = UIBuilderHelpers.CreateText("Number", go.transform, "1", Vector2.zero, 32, UIBuilderHelpers.NunitoBlack);

            // Pulsing ring, shown only for the current level - a donut sprite slightly larger
            // than the node itself, sitting behind it.
            var ringGO = new GameObject("CurrentRing", typeof(RectTransform), typeof(Image));
            ringGO.transform.SetParent(go.transform, false);
            var ringRect = ringGO.GetComponent<RectTransform>();
            ringRect.anchorMin = ringRect.anchorMax = new Vector2(0.5f, 0.5f);
            ringRect.sizeDelta = new Vector2(NodeDiameter + 24f, NodeDiameter + 24f);
            ringGO.transform.SetSiblingIndex(0); // behind the background/number
            var ringImage = ringGO.GetComponent<Image>();
            ringImage.sprite = UIBuilderHelpers.GenerateSprite(CurrentRingSpritePath, 152, 152, (x, y) =>
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(76f, 76f));
                bool inRing = dist <= 76f && dist >= 68f;
                return inRing ? (Color32)RollAndEscapePalette.CurrentRing : new Color32(0, 0, 0, 0);
            });
            ringGO.AddComponent<PulsingRing>();
            ringGO.SetActive(false);

            // Simplified padlock: a small body rect + a thin shackle bar above it, both using
            // the built-in sliced "Background" sprite for soft corners without a custom asset.
            var lockGO = new GameObject("LockIcon", typeof(RectTransform));
            lockGO.transform.SetParent(go.transform, false);
            var lockRect = lockGO.GetComponent<RectTransform>();
            lockRect.anchorMin = lockRect.anchorMax = new Vector2(0.5f, 0.5f);
            lockRect.sizeDelta = new Vector2(NodeDiameter, NodeDiameter);

            var lockBody = CreateSlicedRect("Body", lockGO.transform, new Vector2(0, -6), new Vector2(26, 18), RollAndEscapePalette.LockIcon);
            var lockShackle = CreateSlicedRect("Shackle", lockGO.transform, new Vector2(0, 8), new Vector2(16, 14), RollAndEscapePalette.LockIcon);
            lockShackle.GetComponent<Image>().type = Image.Type.Sliced;
            lockGO.SetActive(false);

            const int starCount = 5; // 5-star rating scale, per StarCalculator
            var starDots = new Image[starCount];
            for (int i = 0; i < starCount; i++)
            {
                var dotGO = new GameObject($"Star{i}", typeof(RectTransform), typeof(Image));
                dotGO.transform.SetParent(go.transform, false);
                var dotRect = dotGO.GetComponent<RectTransform>();
                dotRect.anchorMin = dotRect.anchorMax = new Vector2(0.5f, 0.5f);
                dotRect.sizeDelta = new Vector2(13, 13);
                dotRect.anchoredPosition = new Vector2((i - (starCount - 1) / 2f) * 13f, -(NodeDiameter / 2f + 12f));
                // A crisp white generated circle, not the built-in Knob.psd - the runtime
                // script tints this per-star (gold/dim), and Knob's soft/blurred edge falloff
                // reads as a washed-out smudge once tinted rather than a vivid flat dot.
                var dotImage = dotGO.GetComponent<Image>();
                dotImage.sprite = UIBuilderHelpers.GenerateCircleSprite("Assets/Art/StarDotBase.png", 64, RollAndEscapePalette.White);
                dotImage.color = RollAndEscapePalette.StarGoldHi;
                dotGO.SetActive(false);
                starDots[i] = dotImage;
            }

            var button = go.AddComponent<LevelSelectButton>();
            var so = new SerializedObject(button);
            so.FindProperty("button").objectReferenceValue = go.GetComponent<Button>();
            so.FindProperty("background").objectReferenceValue = background;
            so.FindProperty("levelNumberText").objectReferenceValue = numberText;
            so.FindProperty("currentRing").objectReferenceValue = ringGO;
            so.FindProperty("lockIcon").objectReferenceValue = lockGO;
            var starDotsProp = so.FindProperty("starDots");
            starDotsProp.arraySize = starDots.Length;
            for (int i = 0; i < starDots.Length; i++) starDotsProp.GetArrayElementAtIndex(i).objectReferenceValue = starDots[i];
            so.FindProperty("doneSprite").objectReferenceValue = doneSprite;
            so.FindProperty("currentSprite").objectReferenceValue = currentSprite;
            so.FindProperty("lockedSprite").objectReferenceValue = lockedSprite;
            so.ApplyModifiedPropertiesWithoutUndo();

            return button;
        }

        private static GameObject CreateSlicedRect(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            image.type = Image.Type.Sliced;
            image.color = color;
            return go;
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
