using System.Collections;
using RollAndEscape.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEscape.UI
{
    /// <summary>
    /// Always-on-screen "Level N" indicator plus a live m:ss timer badge during gameplay, per
    /// the spec's Game-scene HUD. Reads LevelSessionContext.CurrentLevelIndex (set when a level
    /// is picked from Level Select) - shows "Level -" when there isn't one (e.g. opening
    /// Game.unity directly in-editor during development). The timer starts the moment this HUD
    /// loads (i.e. the moment the level begins), ticks ~4x/second, and freezes the instant the
    /// ball reaches the exit - it doesn't own completion logic itself (that's
    /// LevelFlowController's job via the same LevelExitTrigger event), just displays elapsed
    /// time for the player to see while they're still playing.
    /// </summary>
    public class LevelHudUI : MonoBehaviour
    {
        private const float TickIntervalSeconds = 0.25f; // ~4x/second, per spec

        [SerializeField] private Text levelText;
        [SerializeField] private Text timerText;
        [SerializeField] private LevelExitTrigger exitTrigger;

        private float _startTime;
        private bool _running;

        private void Start()
        {
            if (levelText != null)
            {
                levelText.text = LevelSessionContext.CurrentLevelIndex >= 0
                    ? $"LEVEL {LevelSessionContext.CurrentLevelIndex + 1}"
                    : "LEVEL -";
            }

            _startTime = Time.time;
            _running = true;
            if (exitTrigger != null) exitTrigger.LevelCompleted += StopTimer;
            if (timerText != null) StartCoroutine(TickTimer());
        }

        private void OnDestroy()
        {
            if (exitTrigger != null) exitTrigger.LevelCompleted -= StopTimer;
        }

        private IEnumerator TickTimer()
        {
            while (_running)
            {
                UpdateTimerText();
                yield return new WaitForSeconds(TickIntervalSeconds);
            }
        }

        private void StopTimer()
        {
            _running = false;
            UpdateTimerText(); // one last update so the badge freezes on the exact stop time, not up to a tick-interval stale
        }

        private void UpdateTimerText()
        {
            if (timerText == null) return;
            float elapsed = Time.time - _startTime;
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);
            timerText.text = $"{minutes}:{seconds:00}";
        }
    }
}
