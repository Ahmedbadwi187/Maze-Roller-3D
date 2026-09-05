using System;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEscape.UI
{
    /// <summary>
    /// Level Complete overlay: completion time, Replay and Next Level buttons. Plain
    /// UnityEngine.UI.Text for now rather than TextMeshPro, to avoid an extra package
    /// dependency this early - swapping the Text components for TMP ones is a pure visual
    /// upgrade planned for the milestone 9 polish pass, not a rework of this class.
    ///
    /// Star display is intentionally a bare optional int (0 = hidden) rather than wired to
    /// real star thresholds - milestone 6 (StarCalculator) decides what a "star" is; this
    /// overlay just needs to already be able to show whatever number it's given.
    /// </summary>
    public class LevelCompleteUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text timeText;
        [SerializeField] private Text starsText;
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

        public void Show(float elapsedSeconds, int stars = 0)
        {
            if (root != null) root.SetActive(true);
            if (timeText != null) timeText.text = FormatTime(elapsedSeconds);
            if (starsText != null) starsText.text = stars > 0 ? new string('*', stars) : string.Empty;
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        private static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:00}:{secs:00}";
        }
    }
}
