using RollAndEscape.Core;
using RollAndEscape.Levels;
using UnityEngine;

namespace RollAndEscape.UI
{
    /// <summary>
    /// Populates the Level Summary screen - one row per level showing its real per-level star
    /// rating and best completion time, read live from LevelProgressService (never a fabricated
    /// placeholder). A plain top-to-bottom list (unlike Level Select's winding snake path),
    /// since every row shares the same layout here.
    /// </summary>
    public class LevelSummaryUI : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private LevelSummaryRow rowTemplate;

        private const float RowHeight = 96f;

        private void Start()
        {
            var progress = GameServices.LevelProgress;
            rowTemplate.gameObject.SetActive(false);

            foreach (var level in LevelRepository.GetAllLevels())
            {
                bool unlocked = progress.IsUnlocked(level.LevelIndex);
                bool completed = progress.IsCompleted(level.LevelIndex);
                int stars = progress.GetStars(level.LevelIndex);
                float bestTime = progress.GetBestTimeSeconds(level.LevelIndex);

                var row = Instantiate(rowTemplate, content);
                row.gameObject.SetActive(true);
                row.Configure(level.LevelIndex, unlocked, completed, stars, bestTime);

                var rect = row.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -(level.LevelIndex * RowHeight));
            }

            content.sizeDelta = new Vector2(content.sizeDelta.x, LevelRepository.TotalLevels * RowHeight);
        }
    }
}
