using UnityEngine;

namespace MazeRoller3D.Gameplay
{
    /// <summary>
    /// Positions a plain Camera at a fixed elevated, tilted 3/4 angle over a maze so the whole
    /// grid fits in view - used for the milestone-2 static preview (no ball/follow-target
    /// exists yet). Milestone 3 introduces a Cinemachine virtual camera that follows the ball
    /// using this same tilt/height ratio, so the framing established here should feel
    /// continuous once the follow-cam takes over.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MazeCameraFramer : MonoBehaviour
    {
        [Tooltip("Downward tilt in degrees. ~50-55 matches the reference app's 3/4 overview angle.")]
        [SerializeField] private float tiltDegrees = 52f;

        [Tooltip("Multiplier applied to the maze's largest dimension to compute camera distance.")]
        [SerializeField] private float distancePadding = 0.9f;

        [SerializeField] private float extraMargin = 3f;

        public void Frame(int mazeWidthCells, int mazeHeightCells, float cellSize, Vector3 mazeOrigin)
        {
            var center = mazeOrigin + new Vector3(
                (mazeWidthCells - 1) * cellSize / 2f,
                0f,
                (mazeHeightCells - 1) * cellSize / 2f);

            float largestSpan = Mathf.Max(mazeWidthCells, mazeHeightCells) * cellSize;
            float distance = largestSpan * distancePadding + extraMargin;

            var tilt = Quaternion.Euler(tiltDegrees, 0f, 0f);
            // Back off along the tilted rig's local -Z (i.e. "backward and up") from the center.
            var offset = tilt * new Vector3(0f, 0f, -distance);

            transform.position = center + offset;
            transform.rotation = tilt;
        }
    }
}
