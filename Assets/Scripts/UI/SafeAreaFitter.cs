using UnityEngine;

namespace RollAndEscape.UI
{
    /// <summary>
    /// Shrinks this RectTransform to Screen.safeArea so UI content never sits under a
    /// notch/status-bar/gesture-nav area, where it would be invisible or untouchable even
    /// though it looks fine in the Editor's flat simulator preview - added after real device
    /// testing showed a top-anchored button was unclickable. Parent any top/bottom-edge
    /// interactive elements under a child object with this component instead of directly
    /// under the Canvas.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastSafeArea;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            // Cheap to compare each frame; only actually recomputes on rotation/fold events.
            if (Screen.safeArea != _lastSafeArea) Apply();
        }

        private void Apply()
        {
            _lastSafeArea = Screen.safeArea;
            var area = _lastSafeArea;

            // Defensive: a degenerate safe area (zero width/height - seen transiently in some
            // Editor/Game-view contexts, e.g. before layout is fully initialized) must never
            // collapse this rect to zero size, or everything inside it becomes invisible and
            // unclickable - fall back to the full screen (equivalent to no inset at all)
            // rather than trusting a clearly-invalid value.
            if (area.width <= 0f || area.height <= 0f || Screen.width <= 0 || Screen.height <= 0)
            {
                _rect.anchorMin = Vector2.zero;
                _rect.anchorMax = Vector2.one;
                _rect.offsetMin = Vector2.zero;
                _rect.offsetMax = Vector2.zero;
                return;
            }

            var anchorMin = area.position;
            var anchorMax = area.position + area.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _rect.anchorMin = anchorMin;
            _rect.anchorMax = anchorMax;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
