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

        public static Text CreateText(string name, Transform parent, string content, Vector2 anchoredPosition, int fontSize)
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            return text;
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
