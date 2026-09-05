using RollAndEscape.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEscape.UI
{
    /// <summary>
    /// Always-on-screen "Level N" indicator during gameplay, per the spec's Game-scene HUD.
    /// Reads LevelSessionContext.CurrentLevelIndex (set when a level is picked from Level
    /// Select) - shows "Level -" when there isn't one (e.g. opening Game.unity directly
    /// in-editor during development).
    /// </summary>
    public class LevelHudUI : MonoBehaviour
    {
        [SerializeField] private Text levelText;

        private void Start()
        {
            if (levelText == null) return;

            levelText.text = LevelSessionContext.CurrentLevelIndex >= 0
                ? $"Level {LevelSessionContext.CurrentLevelIndex + 1}"
                : "Level -";
        }
    }
}
