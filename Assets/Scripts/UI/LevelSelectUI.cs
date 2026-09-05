using RollAndEscape.Core;
using RollAndEscape.Gameplay;
using RollAndEscape.Levels;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RollAndEscape.UI
{
    /// <summary>
    /// Populates the Level Select "snake path" map at runtime from LevelRepository +
    /// LevelProgressService rather than the editor generator baking every button into the scene
    /// file - unlock state/stars can change between visits (a level completed just now), so
    /// this has to be live data read at Start anyway, not something worth pre-baking. Also
    /// drives the header's total-star chip and the "Continue solving" card, and auto-scrolls
    /// the map to the player's current level on open so they don't have to hunt for it.
    /// </summary>
    public class LevelSelectUI : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private LevelSelectButton buttonTemplate;
        [SerializeField] private string gameSceneName = "Game";

        [Header("Header + continue card (optional - null-checked)")]
        [SerializeField] private Text totalStarsText;
        [SerializeField] private Text continueLevelText;
        [SerializeField] private Text continueSubtitleText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Text completedCountText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewport;

        // Snake-path layout constants - each node sits directly under Content (no per-row
        // wrapper needed) at a manually computed position, since left/center/right alignment
        // varies per level and neither GridLayoutGroup nor VerticalLayoutGroup can express that.
        private const float RowHeight = 110f;
        private const float NodeSideInset = 76f; // distance from the row's edge to an off-center node's center

        private void Start()
        {
            // Banner lives on menu/level-select only, never in-game, per the monetization spec.
            if (GameServices.AdsGate.ShouldShowBanner()) GameServices.Ads.ShowBanner();

            var progress = GameServices.LevelProgress;
            buttonTemplate.gameObject.SetActive(false);

            int totalStars = 0;
            int completedCount = 0;
            int currentLevelIndex = -1; // first unlocked-but-not-completed level - where the player left off
            LevelSelectButton currentButtonInstance = null;

            foreach (var level in LevelRepository.GetAllLevels())
            {
                bool unlocked = progress.IsUnlocked(level.LevelIndex);
                bool completed = progress.IsCompleted(level.LevelIndex);
                int stars = progress.GetStars(level.LevelIndex);
                totalStars += stars;
                if (completed) completedCount++;
                if (unlocked && !completed && currentLevelIndex < 0) currentLevelIndex = level.LevelIndex;

                bool isCurrent = level.LevelIndex == currentLevelIndex;

                var button = Instantiate(buttonTemplate, content);
                button.gameObject.SetActive(true);
                button.Configure(level, unlocked, completed, isCurrent, stars, OnLevelSelected);
                PositionNode(button.GetComponent<RectTransform>(), level.LevelIndex);
                if (isCurrent) currentButtonInstance = button;
            }

            if (content is RectTransform contentRect)
            {
                contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, LevelRepository.TotalLevels * RowHeight);
            }

            // Every level's already completed (finished all 100) - "current" falls back to the
            // last level so the header/continue card still show something sensible.
            if (currentLevelIndex < 0) currentLevelIndex = LevelRepository.TotalLevels - 1;

            if (totalStarsText != null) totalStarsText.text = totalStars.ToString();
            if (completedCountText != null) completedCountText.text = $"{completedCount} / {LevelRepository.TotalLevels} cleared";
            if (continueLevelText != null) continueLevelText.text = $"Level {currentLevelIndex + 1}";
            if (continueSubtitleText != null)
            {
                int remaining = LevelRepository.TotalLevels - completedCount;
                continueSubtitleText.text = remaining > 0 ? $"{remaining} mazes left to clear" : "All mazes cleared!";
            }
            if (continueButton != null)
            {
                // Explicitly forced true (defensive) - a fresh save (nothing played yet) must
                // still be able to start level 1 from here, never appear disabled.
                continueButton.interactable = true;
                int levelToStart = currentLevelIndex;
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(() => OnLevelSelected(LevelRepository.GetLevel(levelToStart)));

                var continueLabel = continueButton.GetComponentInChildren<Text>();
                if (continueLabel != null) continueLabel.text = completedCount > 0 ? "Continue solving" : "Start solving";
            }

            if (currentButtonInstance != null) ScrollToButton(currentButtonInstance.GetComponent<RectTransform>());
        }

        /// <summary>Scrolls the map so the current-level node lands roughly a third of the way
        /// down the viewport (not flush at the very top edge) - avoids opening on a scroll
        /// position that hides the node behind the header on some aspect ratios.</summary>
        private void ScrollToButton(RectTransform target)
        {
            if (scrollRect == null || scrollRect.content == null || target == null) return;

            Canvas.ForceUpdateCanvases();
            float contentHeight = scrollRect.content.rect.height;
            float viewportHeight = viewport != null ? viewport.rect.height : scrollRect.viewport.rect.height;
            if (contentHeight <= viewportHeight) return;

            // Target's Y position within content, measured from content's top edge.
            float targetTopOffset = scrollRect.content.rect.height / 2f - target.anchoredPosition.y;
            float desiredCenterOffset = targetTopOffset - viewportHeight * 0.33f;
            float maxScroll = contentHeight - viewportHeight;
            float normalized = 1f - Mathf.Clamp01(desiredCenterOffset / maxScroll);
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalized);
        }

        /// <summary>Places one node in the winding snake path: level index % 3 picks
        /// center/left/right alignment (matching the mockup's <c>idx===0/1/2</c> logic), each
        /// row stacked top-to-bottom by <see cref="RowHeight"/>.</summary>
        private static void PositionNode(RectTransform rect, int levelIndex)
        {
            int column = levelIndex % 3;
            float xAnchor = column == 0 ? 0.5f : column == 1 ? 0f : 1f;
            float xOffset = column == 0 ? 0f : column == 1 ? NodeSideInset : -NodeSideInset;

            rect.anchorMin = rect.anchorMax = new Vector2(xAnchor, 1f);
            rect.pivot = new Vector2(xAnchor, 0.5f);
            rect.anchoredPosition = new Vector2(xOffset, -(levelIndex * RowHeight + RowHeight / 2f));
        }

        private void OnLevelSelected(LevelDefinition level)
        {
            LevelSessionContext.SelectLevel(level.LevelIndex, level.Width, level.Height, level.Seed);
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
