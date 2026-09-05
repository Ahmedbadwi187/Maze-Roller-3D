using System;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEscape.UI
{
    /// <summary>
    /// Level Complete overlay: completion time, star row, Replay and Next Level buttons.
    /// Redesigned per the approved "Buze" mockup (Claude Design project d6305a3a,
    /// Buze.dc.html) - was a dark scrim with plain white text; now a purple gradient panel
    /// with a checkmark badge and 3 individual star-dot images (gold/dim) instead of an
    /// asterisk-text string. The mockup shows "Solved in N steps" - adapted to elapsed TIME
    /// here since this game tracks continuous rolling time, not discrete grid steps (the
    /// mockup's simplified simulation isn't how the real physics-based game works).
    /// </summary>
    public class LevelCompleteUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text headingText;
        [SerializeField] private Text timeText;
        [SerializeField] private Image[] starDots;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button nextLevelButton;

        public event Action ReplayRequested;
        public event Action NextLevelRequested;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
            if (replayButton != null) replayButton.onClick.AddListener(() => ReplayRequested?.Invoke());
            if (nextLevelButton != null) nextLevelButton.onClick.AddListener(() => NextLevelRequested?.Invoke());
        }

        /// <param name="levelNumber">1-based level number for the heading (e.g. "Maze 5
        /// solved!"); -1 (default) when unknown, which falls back to a generic heading - see
        /// LevelFlowController for why this can be unknown (testing Game.unity directly).</param>
        public void Show(float elapsedSeconds, int stars = 0, int levelNumber = -1)
        {
            if (root != null) root.SetActive(true);
            if (headingText != null) headingText.text = levelNumber > 0 ? $"Maze {levelNumber} solved!" : "Maze solved!";
            if (timeText != null) timeText.text = FormatTime(elapsedSeconds);

            if (starDots != null)
            {
                for (int i = 0; i < starDots.Length; i++)
                {
                    if (starDots[i] == null) continue;
                    starDots[i].color = i < stars ? RollAndEscapePalette.StarGoldHi : RollAndEscapePalette.StarDim;
                }
            }
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        private static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"Solved in {minutes:00}:{secs:00}";
        }
    }
}
