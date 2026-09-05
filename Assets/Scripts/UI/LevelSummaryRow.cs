using UnityEngine;
using UnityEngine.UI;

namespace RollAndEscape.UI
{
    /// <summary>One row in the Level Summary screen's scrollable list - level number, the 5
    /// real per-level star dots (gold for earned, dim for not, all dim/hidden-detail for a
    /// level never played), and the best completion time or "Not played yet" for an unplayed
    /// level. One instance per level, populated by <see cref="LevelSummaryUI"/>.</summary>
    public class LevelSummaryRow : MonoBehaviour
    {
        [SerializeField] private Text levelNumberText;
        [SerializeField] private Image[] starDots;
        [SerializeField] private Text timeText;
        [SerializeField] private Image lockIcon;

        public void Configure(int levelIndex, bool unlocked, bool completed, int stars, float bestTimeSeconds)
        {
            if (levelNumberText != null) levelNumberText.text = $"Level {levelIndex + 1}";

            if (starDots != null)
            {
                for (int i = 0; i < starDots.Length; i++)
                {
                    if (starDots[i] == null) continue;
                    // Never-played levels show 0 stars (all dim) - only a real recorded
                    // completion lights any dot, never a fabricated/flat placeholder value.
                    starDots[i].color = (completed && i < stars) ? RollAndEscapePalette.StarGoldHi : RollAndEscapePalette.StarDim;
                }
            }

            if (timeText != null)
            {
                if (!unlocked) timeText.text = "Locked";
                else if (completed) timeText.text = FormatTime(bestTimeSeconds);
                else timeText.text = "Not played yet";
            }

            if (lockIcon != null) lockIcon.gameObject.SetActive(!unlocked);

            var group = GetComponent<CanvasGroup>();
            if (group != null) group.alpha = unlocked ? 1f : 0.5f;
        }

        private static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes}:{secs:00}";
        }
    }
}
