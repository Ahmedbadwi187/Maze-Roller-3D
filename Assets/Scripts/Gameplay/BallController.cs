using UnityEngine;

namespace MazeRoller3D.Gameplay
{
    /// <summary>
    /// Drives the ball's Rigidbody from whatever <see cref="PlayerInputRouter"/> reports each
    /// physics step - a force along the maze's floor plane (X/Z), scaled and clamped to a max
    /// speed so the ball stays controllable rather than accelerating forever. Realistic rolling
    /// itself comes from the Rigidbody + a low-friction/low-bounciness PhysicsMaterial on both
    /// the ball's collider and the floor/wall colliders (set on the prefabs, not here) - this
    /// script only supplies the driving force, physics does the rest.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BallController : MonoBehaviour
    {
        [SerializeField] private PlayerInputRouter inputRouter;
        [SerializeField] private float forceMultiplier = 12f;
        [SerializeField] private float maxSpeed = 6f;

        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (inputRouter == null) return;

            var input = inputRouter.GetMovementInput();
            var force = new Vector3(input.x, 0f, input.y) * forceMultiplier;
            _rigidbody.AddForce(force, ForceMode.Force);

            var flatVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
            if (flatVelocity.magnitude > maxSpeed)
            {
                var clamped = flatVelocity.normalized * maxSpeed;
                _rigidbody.linearVelocity = new Vector3(clamped.x, _rigidbody.linearVelocity.y, clamped.z);
            }
        }
    }
}
