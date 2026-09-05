using System;
using System.IO;
using RollAndEscape.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEscape.EditorTools
{
    /// <summary>Shared runtime-UI construction helpers for the milestone scene generators -
    /// factored out once a third generator (Milestone 7) needed the same
    /// Text/Button/Toggle boilerplate as milestones 4 and 5.</summary>
    internal static class UIBuilderHelpers
    {
        // Bundled Nunito weights (SIL Open Font License) - the app-wide typeface per the
        // approved "Buze" mockup, replacing Unity's default LegacyRuntime.ttf everywhere.
        private static Font _nunitoBold;
        private static Font _nunitoBlack;
        public static Font NunitoBold => _nunitoBold != null ? _nunitoBold : (_nunitoBold = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Nunito-Bold.ttf"));
        public static Font NunitoBlack => _nunitoBlack != null ? _nunitoBlack : (_nunitoBlack = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Nunito-Black.ttf"));

        /// <summary>Self-healing pixel-texture generator shared by every screen that needs a
        /// gradient/rounded-shape sprite (no shader or hand-authored art needed) - always
        /// re-writes the pixels (cheap, deterministic) rather than load-if-exists, so a tuning
        /// change reaches the asset even after it's been generated once.</summary>
        /// <param name="border">Optional 9-slice border in texture pixels (left, bottom, right,
        /// top). Required for any rounded-corner sprite that will be stretched onto a
        /// NON-square RectTransform (a card, a wide button, a badge) - without it, Image.Type
        /// Simple stretches the whole square texture non-uniformly, squashing round corners
        /// into flat, barely-visible curves (this was a real bug: cards/badges rendered with
        /// almost-square corners instead of the intended rounding). Pass null for sprites only
        /// ever used on a square rect (a circle icon, a small dot).</param>
        public static Sprite GenerateSprite(string path, int width, int height, Func<int, int, Color32> pixelAt, Vector4? border = null)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = pixelAt(x, y);
            texture.SetPixels32(pixels);
            texture.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaIsTransparency = true;
                if (border.HasValue) importer.spriteBorder = border.Value;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Multi-stop color lerp, stops given as (0-1 position, color) pairs in
        /// ascending order - mirrors CSS's <c>linear-gradient()</c>/<c>radial-gradient()</c>
        /// stop list so mockup gradients translate directly.</summary>
        public static Color32 LerpStops(float t, params (float pos, Color32 color)[] stops)
        {
            t = Mathf.Clamp01(t);
            for (int i = 0; i < stops.Length - 1; i++)
            {
                if (t <= stops[i + 1].pos)
                {
                    float span = stops[i + 1].pos - stops[i].pos;
                    float localT = span > 0f ? (t - stops[i].pos) / span : 0f;
                    return Color32.Lerp(stops[i].color, stops[i + 1].color, Mathf.Clamp01(localT));
                }
            }
            return stops[stops.Length - 1].color;
        }

        /// <summary>0-1 gradient position for a pixel under a CSS-style linear-gradient angle
        /// (0deg = to top, 90deg = to right, 180deg = to bottom, clockwise) - texture space is
        /// Y-up (pixel (0,0) is bottom-left), the opposite of CSS's Y-down, so this flips Y.</summary>
        public static float LinearGradientT(int x, int y, int width, int height, float angleDegrees)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)); // texture-space (Y-up) direction of travel
            var center = new Vector2((width - 1) / 2f, (height - 1) / 2f);
            var point = new Vector2(x, y) - center;

            // Project every corner to find the gradient line's extent, so t=0/1 land exactly on
            // the rect's edges regardless of angle or aspect ratio.
            float half = Mathf.Abs(dir.x) * width / 2f + Mathf.Abs(dir.y) * height / 2f;
            float projected = Vector2.Dot(point, dir);
            return half > 0f ? (projected + half) / (2f * half) : 0f;
        }

        /// <summary>A full-stretch child that shrinks itself to Screen.safeArea at runtime -
        /// parent any edge-anchored interactive element (a top-corner button, a bottom HUD)
        /// under this instead of directly under the Canvas, or it can end up rendered behind
        /// a real device's notch/status bar, invisible and unclickable even though it looks
        /// fine in the Editor's flat simulator preview.</summary>
        public static RectTransform CreateSafeArea(Transform parent)
        {
            var go = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>Re-anchors an already-built element to sit a fixed distance below the TOP
        /// EDGE OF ITS PARENT (rather than a fixed distance from parent center) - safe
        /// regardless of how tall the parent (typically a SafeArea) actually ends up being,
        /// unlike a raw center-based Y offset which assumes a specific parent height and can
        /// push the element past a *smaller* safe area's own edge.</summary>
        /// <param name="xAnchor">0 = left edge, 0.5 = centered horizontally, 1 = right edge.</param>
        /// <param name="insetFromTop">Distance below the parent's top edge, in the parent's own local units.</param>
        public static void AnchorToTop(RectTransform rect, float xAnchor, float insetFromTop)
        {
            rect.anchorMin = new Vector2(xAnchor, 1f);
            rect.anchorMax = new Vector2(xAnchor, 1f);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -insetFromTop);
        }

        public static Text CreateText(string name, Transform parent, string content, Vector2 anchoredPosition, int fontSize, Font font = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(500, 80);

            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = font != null ? font : NunitoBold;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            return text;
        }

        /// <summary>Rounded-rect sprite via the standard signed-distance test (clamp to the
        /// inner rect, check distance to the corner radius) - shared by any card/chip/button
        /// that needs soft corners without a 9-sliced art asset. 9-slice bordered by
        /// cornerRadius on every side, so the caller MUST set the resulting Image's
        /// <c>type = Image.Type.Sliced</c> for corners to render correctly on a non-square
        /// rect (Simple/stretch distorts them - see GenerateSprite's border param doc).</summary>
        public static Sprite GenerateRoundedRectSprite(string path, int size, float cornerRadius, Color32 fillColor)
        {
            var transparent = new Color32(0, 0, 0, 0);
            var border = new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius);
            return GenerateSprite(path, size, size, (x, y) =>
            {
                float px = x + 0.5f, py = y + 0.5f;
                float cx = Mathf.Clamp(px, cornerRadius, size - cornerRadius);
                float cy = Mathf.Clamp(py, cornerRadius, size - cornerRadius);
                float dist = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                return dist <= cornerRadius ? fillColor : transparent;
            }, border);
        }

        /// <summary>Rounded-rect OUTLINE only (transparent fill) - the mockup's secondary
        /// "border only, transparent background" button style (e.g. Level Complete's "Back to
        /// map"/"Play again" button). Same 9-slice border requirement as
        /// GenerateRoundedRectSprite above - set the Image's type to Sliced.</summary>
        public static Sprite GenerateRoundedRectOutlineSprite(string path, int size, float cornerRadius, float strokeWidth, Color32 strokeColor)
        {
            var transparent = new Color32(0, 0, 0, 0);
            var border = new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius);
            return GenerateSprite(path, size, size, (x, y) =>
            {
                float px = x + 0.5f, py = y + 0.5f;
                float cx = Mathf.Clamp(px, cornerRadius, size - cornerRadius);
                float cy = Mathf.Clamp(py, cornerRadius, size - cornerRadius);
                float dist = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                bool insideOuter = dist <= cornerRadius;
                bool insideInner = dist <= cornerRadius - strokeWidth
                    && px >= strokeWidth && px <= size - strokeWidth
                    && py >= strokeWidth && py <= size - strokeWidth;
                return insideOuter && !insideInner ? strokeColor : transparent;
            }, border);
        }

        /// <summary>Solid-fill circle sprite - level nodes, star dots, the checkmark badge, all
        /// of which are perfect FLAT circles in the mockup.</summary>
        public static Sprite GenerateCircleSprite(string path, int size, Color32 fillColor)
        {
            var transparent = new Color32(0, 0, 0, 0);
            float radius = size / 2f;
            var center = new Vector2(radius, radius);
            return GenerateSprite(path, size, size, (x, y) =>
                Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) <= radius ? fillColor : transparent);
        }

        /// <summary>Glossy sphere sprite matching the mockup's exact recipe for every
        /// "ball-style" icon (the splash/maze ball, the header points chip's coin) -
        /// <c>radial-gradient(circle at 35% 30%, highlight, base)</c>: a circular clip with the
        /// gradient's own origin offset toward one corner for a highlight, fading to the base
        /// color at the farthest point in the box (CSS's default "farthest-corner" sizing).
        /// <paramref name="highlightOffsetNormalized"/> is in CSS convention - X from the left,
        /// Y from the TOP (flipped internally to texture space, which is Y-up).</summary>
        public static Sprite GenerateSphereSprite(string path, int size, Color32 highlightColor, Color32 baseColor, Vector2 highlightOffsetNormalized)
        {
            var transparent = new Color32(0, 0, 0, 0);
            float radius = size / 2f;
            var center = new Vector2(radius, radius);
            var highlightPoint = new Vector2(size * highlightOffsetNormalized.x, size * (1f - highlightOffsetNormalized.y));

            float maxDist = 0f;
            foreach (var corner in new[] { Vector2.zero, new Vector2(size, 0), new Vector2(0, size), new Vector2(size, size) })
                maxDist = Mathf.Max(maxDist, Vector2.Distance(highlightPoint, corner));

            return GenerateSprite(path, size, size, (x, y) =>
            {
                var point = new Vector2(x + 0.5f, y + 0.5f);
                if (Vector2.Distance(point, center) > radius) return transparent;
                float t = maxDist > 0f ? Vector2.Distance(point, highlightPoint) / maxDist : 0f;
                return LerpStops(Mathf.Clamp01(t), (0f, highlightColor), (1f, baseColor));
            });
        }

        public static Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);

            var label_ = CreateText(name + "_Label", go.transform, label, Vector2.zero, 32);
            label_.color = Color.black;

            return go.GetComponent<Button>();
        }

        public static Toggle CreateToggle(string name, Transform parent, string label, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(500, 70);

            var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(go.transform, false);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(0f, 0.5f);
            bgRect.pivot = new Vector2(0f, 0.5f);
            bgRect.anchoredPosition = new Vector2(0f, 0f);
            bgRect.sizeDelta = new Vector2(60, 60);
            var bgImage = bgGO.GetComponent<Image>();
            bgImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            bgImage.type = Image.Type.Sliced;
            bgImage.color = Color.white;

            var checkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkGO.transform.SetParent(bgGO.transform, false);
            var checkRect = checkGO.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.5f, 0.5f);
            checkRect.anchorMax = new Vector2(0.5f, 0.5f);
            checkRect.sizeDelta = new Vector2(40, 40);
            var checkImage = checkGO.GetComponent<Image>();
            checkImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
            checkImage.color = new Color(0.2f, 0.6f, 0.3f);

            var labelText = CreateText(name + "_Label", go.transform, label, new Vector2(280, 0), 32);
            labelText.alignment = TextAnchor.MiddleLeft;

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = true;

            return toggle;
        }

        public static void StretchToFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
