using System;
using RollAndEscape.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEscape.UI
{
    /// <summary>One node on the Level Select "snake path" map - a circular tile showing the
    /// level number, styled per state (locked/current/done), with a small star-dot row for
    /// completed levels. Populated by <see cref="LevelSelectUI"/>, one instance per level.
    /// Redesigned per the approved "Buze" mockup (Claude Design project d6305a3a,
    /// Buze.dc.html) - was a plain white square with a number and an asterisk-text star
    /// count.</summary>
    public class LevelSelectButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Text levelNumberText;
        [SerializeField] private GameObject currentRing;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private Image[] starDots;

        [Header("State sprites (assigned once by the builder)")]
        [SerializeField] private Sprite doneSprite;
        [SerializeField] private Sprite currentSprite;
        [SerializeField] private Sprite lockedSprite;

        private static readonly Color32 DoneNumberColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        private static readonly Color32 LockedNumberColor = new Color32(0x00, 0x00, 0x00, 0x00); // hidden - lock icon shows instead

        public void Configure(LevelDefinition level, bool unlocked, bool completed, bool isCurrent, int stars, Action<LevelDefinition> onSelected)
        {
            if (levelNumberText != null)
            {
                levelNumberText.text = (level.LevelIndex + 1).ToString();
            }

            bool locked = !unlocked;

            if (background != null)
            {
                background.sprite = locked ? lockedSprite : (isCurrent ? currentSprite : doneSprite);
            }
            if (levelNumberText != null)
            {
                levelNumberText.color = locked ? LockedNumberColor
                    : isCurrent ? RollAndEscapePalette.CurrentNumber
                    : DoneNumberColor;
            }
            if (currentRing != null) currentRing.SetActive(isCurrent);
            if (lockIcon != null) lockIcon.SetActive(locked);

            if (starDots != null)
            {
                for (int i = 0; i < starDots.Length; i++)
                {
                    if (starDots[i] == null) continue;
                    bool show = completed && !locked;
                    starDots[i].gameObject.SetActive(show);
                    if (show) starDots[i].color = i < stars ? RollAndEscapePalette.StarGoldHi : RollAndEscapePalette.StarDim;
                }
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
