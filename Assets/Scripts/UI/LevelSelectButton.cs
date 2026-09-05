using System;
using RollAndEscape.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEscape.UI
{
    /// <summary>One tile in the Level Select grid - level number, star count, locked/unlocked
    /// visual state. Populated by <see cref="LevelSelectUI"/>, one instance per level.</summary>
    public class LevelSelectButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Text levelNumberText;
        [SerializeField] private Text starsText;

        // Bright, varied palette for unlocked cards - a deterministic pick per level index (so
        // the same level always shows the same color) rather than every unlocked tile being
        // plain white, per feedback that the game needed more color appeal for younger players.
        private static readonly Color[] CardPalette =
        {
            new Color32(0xFF, 0x6B, 0x6B, 0xFF), // coral red
            new Color32(0xFF, 0xD9, 0x3D, 0xFF), // sunny yellow
            new Color32(0x4E, 0xCD, 0xC4, 0xFF), // turquoise
            new Color32(0x95, 0xE1, 0xD3, 0xFF), // mint
            new Color32(0xA8, 0xE6, 0xCF, 0xFF), // light green
            new Color32(0xFF, 0xAF, 0xCC, 0xFF), // pink
            new Color32(0xB2, 0x8D, 0xFF, 0xFF), // purple
            new Color32(0xFF, 0xB4, 0x7B, 0xFF), // orange
        };

        private static readonly Color LockedColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

        public void Configure(LevelDefinition level, bool unlocked, int stars, Action<LevelDefinition> onSelected)
        {
            if (levelNumberText != null)
            {
                levelNumberText.text = (level.LevelIndex + 1).ToString();
            }
            else
            {
                Debug.LogWarning($"LevelSelectButton for level {level.LevelIndex + 1}: levelNumberText is not wired up - numbers won't show.");
            }
            if (starsText != null) starsText.text = stars > 0 ? new string('*', stars) : string.Empty;
            if (background != null)
            {
                background.color = unlocked ? CardPalette[level.LevelIndex % CardPalette.Length] : LockedColor;
            }

            if (button != null)
            {
                button.interactable = unlocked;
                button.onClick.RemoveAllListeners();
                if (unlocked) button.onClick.AddListener(() => onSelected(level));
            }
        }
    }
}
