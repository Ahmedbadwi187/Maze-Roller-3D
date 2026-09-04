using MazeRoller3D.Core;
using UnityEngine;

namespace MazeRoller3D.Gameplay
{
    /// <summary>
    /// Picks which <see cref="IMovementInputProvider"/> feeds <see cref="BallController"/>,
    /// based on <see cref="ActiveScheme"/> - read from the persisted Settings on Awake
    /// (milestone 7's Settings screen writes it), falling back to the inspector-set default
    /// (Tilt, the primary scheme per spec) the first time the game ever runs. Keeping this one
    /// small class as the single switch point means BallController never needs to know a
    /// joystick exists.
    /// </summary>
    public class PlayerInputRouter : MonoBehaviour
    {
        [SerializeField] private ControlScheme activeScheme = ControlScheme.Tilt;
        [SerializeField] private TiltInputHandler tiltInput;
        [SerializeField] private JoystickInputHandler joystickInput;

        public ControlScheme ActiveScheme
        {
            get => activeScheme;
            set => activeScheme = value;
        }

        private void Awake()
        {
            activeScheme = (ControlScheme)GameServices.Settings.ControlSchemeRaw;
        }

        public Vector2 GetMovementInput()
        {
            IMovementInputProvider provider = activeScheme == ControlScheme.Tilt
                ? tiltInput
                : joystickInput;

            return provider != null ? provider.GetMovementInput() : Vector2.zero;
        }
    }
}
