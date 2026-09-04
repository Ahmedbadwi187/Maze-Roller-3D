using System;
using MazeRoller3D.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace MazeRoller3D.UI
{
    /// <summary>One tile in the Level Select grid - level number, star count, locked/unlocked
    /// visual state. Populated by <see cref="LevelSelectUI"/>, one instance per level.</summary>
    public class LevelSelectButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Text levelNumberText;
        [SerializeField] private Text starsText;

        private static readonly Color UnlockedColor = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color LockedColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

        public void Configure(LevelDefinition level, bool unlocked, int stars, Action<LevelDefinition> onSelected)
        {
            if (levelNumberText != null) levelNumberText.text = (level.LevelIndex + 1).ToString();
            if (starsText != null) starsText.text = stars > 0 ? new string('*', stars) : string.Empty;
            if (background != null) background.color = unlocked ? UnlockedColor : LockedColor;

            if (button != null)
            {
                button.interactable = unlocked;
                button.onClick.RemoveAllListeners();
                if (unlocked) button.onClick.AddListener(() => onSelected(level));
            }
        }
    }
}
