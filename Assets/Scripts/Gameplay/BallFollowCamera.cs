using UnityEngine;

namespace RollAndEscape.Gameplay
{
    /// <summary>
    /// Simple smoothed follow camera: stays at a fixed tilted offset behind/above
    /// <see cref="target"/> and eases toward it each frame. A plain script for now -
    /// milestone 9 swaps this for a Cinemachine virtual camera for extra smoothing/collision
    /// handling, per the project's milestone order, but the offset/tilt values here are what
    /// that Cinemachine rig will be tuned to match, so the framing doesn't jump when it does.
    /// </summary>
    public class BallFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 6f, -5f);
        [SerializeField] private float positionSmoothTime = 0.15f;
        [SerializeField] private float lookTilt = 52f;

        private Vector3 _velocity;

        public void SetTarget(Transform newTarget) => target = newTarget;

        private void LateUpdate()
        {
            if (target == null) return;

            var desiredPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, positionSmoothTime);
            transform.rotation = Quaternion.Euler(lookTilt, 0f, 0f);
        }
    }
}
