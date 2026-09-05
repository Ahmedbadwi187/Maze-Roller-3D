using UnityEngine;

namespace RollAndEscape.UI
{
    /// <summary>Gentle scale pulse for the ring around the current level's node on the Level
    /// Select map - a cheap always-on animation (only one node is ever "current" at a time, so
    /// this never runs more than one instance) approximating the mockup's CSS "wiggle" cue that
    /// draws the eye to where the player should tap next.</summary>
    public class PulsingRing : MonoBehaviour
    {
        [SerializeField] private float minScale = 1f;
        [SerializeField] private float maxScale = 1.12f;
        [SerializeField] private float periodSeconds = 1.6f;

        private RectTransform _rect;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        private void Update()
        {
            float t = (Mathf.Sin(Time.time * (2f * Mathf.PI / periodSeconds)) + 1f) * 0.5f;
            float scale = Mathf.Lerp(minScale, maxScale, t);
            _rect.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
