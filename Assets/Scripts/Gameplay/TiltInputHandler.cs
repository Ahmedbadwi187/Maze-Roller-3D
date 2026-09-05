using UnityEngine;
using UnityEngine.InputSystem;

namespace RollAndEscape.Gameplay
{
    /// <summary>
    /// Reads the device accelerometer via the New Input System and maps it to a 2D movement
    /// vector. Tilting the device so its right edge dips down pushes the ball right, tilting
    /// the top edge down pushes it forward - i.e. the phone's tilt maps directly onto the
    /// maze's floor plane, matching how every other tilt-maze game controls.
    ///
    /// The accelerometer only reports real values on an actual device; in the Editor, use
    /// Window > Analysis > Input Debugger to feed it simulated values for testing.
    /// </summary>
    public class TiltInputHandler : MonoBehaviour, IMovementInputProvider
    {
        [Tooltip("Raw accelerometer reading needed before it counts as input, to ignore sensor noise while the device is held flat.")]
        [SerializeField] private float deadZone = 0.05f;

        [Tooltip("Multiplies the raw tilt reading before clamping to [-1,1] - higher = more sensitive to small tilts.")]
        [SerializeField] private float sensitivity = 2f;

        private void OnEnable()
        {
            if (Accelerometer.current != null) InputSystem.EnableDevice(Accelerometer.current);
        }

        private void OnDisable()
        {
            if (Accelerometer.current != null) InputSystem.DisableDevice(Accelerometer.current);
        }

        public Vector2 GetMovementInput()
        {
            if (Accelerometer.current == null) return Vector2.zero;

            // Device acceleration: x = tilt left/right, y = tilt up/down (portrait orientation).
            // Gravity reads ~-1 on the axis pointing into the ground when the device is flat,
            // so raw x/y already read ~0 when flat and ramp up as the device tilts - exactly
            // the signal we want, just scaled and dead-zoned.
            var raw = Accelerometer.current.acceleration.ReadValue();
            var input = new Vector2(raw.x, raw.y);

            if (input.magnitude < deadZone) return Vector2.zero;

            input *= sensitivity;
            return Vector2.ClampMagnitude(input, 1f);
        }
    }
}
