using UnityEngine;

namespace RollAndEscape.UI
{
    /// <summary>
    /// The app-wide color palette from the approved "Buze" design (Claude Design project
    /// d6305a3a-9a91-4aae-9592-12f30fe13cee, Buze.dc.html) - converted from the mockup's OKLCH
    /// values to sRGB hex via the standard CSS Color 4 OKLab conversion, since Unity has no
    /// native OKLCH support. Runtime-accessible (not editor-only) because it's read both by the
    /// scene-generator editor scripts AND by runtime UI scripts (LevelSelectButton,
    /// LevelCompleteUI) that recolor elements per state. Deliberately excludes the mockup's
    /// Play-screen maze/wall/ball colors (green board, brown walls, orange ball) - per explicit
    /// instruction, the actual gameplay maze/ball keep their existing colors untouched; only
    /// chrome around it (HUD, buttons) picks up this palette.
    /// </summary>
    public static class RollAndEscapePalette
    {
        // Splash
        public static readonly Color32 SplashBgTop = new Color32(0x98, 0x55, 0x93, 0xFF);
        public static readonly Color32 SplashBgBottom = new Color32(0x5E, 0x47, 0x84, 0xFF);
        public static readonly Color32 IconSquareGreen = new Color32(0x5E, 0x96, 0x60, 0xFF);
        public static readonly Color32 BallHighlight = new Color32(0xFD, 0xDC, 0x5B, 0xFF);
        public static readonly Color32 BallBase = new Color32(0xD7, 0x71, 0x00, 0xFF);

        // Home / Level Select
        public static readonly Color32 HomeBgTop = new Color32(0xB4, 0x91, 0xB0, 0xFF);
        public static readonly Color32 HomeBgMid = new Color32(0xC1, 0xB8, 0xD4, 0xFF);
        public static readonly Color32 HomeBgBottom = new Color32(0xF4, 0xEE, 0xE0, 0xFF);
        public static readonly Color32 EyebrowText = new Color32(0x2F, 0x2C, 0x37, 0xFF);
        public static readonly Color32 PlayerNameText = new Color32(0x1C, 0x19, 0x23, 0xFF);
        public static readonly Color32 StarChipText = new Color32(0x30, 0x27, 0x1F, 0xFF);
        public static readonly Color32 CardAccentPurple = new Color32(0x83, 0x4D, 0x7E, 0xFF);
        public static readonly Color32 CardTitleText = new Color32(0x21, 0x1D, 0x27, 0xFF);
        public static readonly Color32 CardSubtext = new Color32(0x57, 0x53, 0x5F, 0xFF);
        public static readonly Color32 ContinueButtonBg = new Color32(0x98, 0x55, 0x93, 0xFF);
        public static readonly Color32 SectionTitle = new Color32(0x21, 0x1D, 0x27, 0xFF);
        public static readonly Color32 SectionCount = new Color32(0x57, 0x53, 0x5F, 0xFF);

        // Level map nodes
        public static readonly Color32 NodeDoneTop = new Color32(0x72, 0xB8, 0x75, 0xFF);
        public static readonly Color32 NodeDoneBottom = new Color32(0x47, 0x8D, 0x4B, 0xFF);
        public static readonly Color32 NodeLockedBg = new Color32(0xD9, 0xD5, 0xE3, 0xFF);
        public static readonly Color32 LockIcon = new Color32(0x73, 0x6F, 0x7C, 0xFF);
        public static readonly Color32 CurrentRing = new Color32(0x83, 0x4D, 0x7E, 0xFF);
        public static readonly Color32 CurrentNumber = new Color32(0x83, 0x4D, 0x7E, 0xFF);
        public static readonly Color32 White = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

        // Game HUD chrome (NOT the maze/wall/ball themselves - see class doc comment)
        public static readonly Color32 BackButtonText = new Color32(0x2F, 0x2C, 0x37, 0xFF);
        public static readonly Color32 LevelBadgeText = new Color32(0x57, 0x53, 0x5F, 0xFF);
        public static readonly Color32 LevelEyebrow = new Color32(0x74, 0x3F, 0x6F, 0xFF);
        public static readonly Color32 LevelTitle = new Color32(0x21, 0x1D, 0x27, 0xFF);

        // Level Complete
        public static readonly Color32 CompleteBgTop = new Color32(0xB4, 0x91, 0xB0, 0xFF);
        public static readonly Color32 CompleteBgBottom = new Color32(0xD1, 0xC8, 0xE5, 0xFF);
        public static readonly Color32 CheckmarkColor = new Color32(0x83, 0x4D, 0x7E, 0xFF);
        public static readonly Color32 StarGoldHi = new Color32(0xFF, 0xCF, 0x53, 0xFF);
        public static readonly Color32 StarGoldLo = new Color32(0xDB, 0x6D, 0x00, 0xFF);
        public static readonly Color32 StarDim = new Color32(0xD3, 0xCD, 0xBF, 0xFF);
        public static readonly Color32 NextMazeButtonText = new Color32(0x7D, 0x47, 0x78, 0xFF);
    }
}
