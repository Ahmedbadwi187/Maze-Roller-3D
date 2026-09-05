using UnityEngine;

namespace RollAndEscape.Gameplay
{
    /// <summary>
    /// A source of 2D movement input (X = left/right, Y = forward/back on the maze's floor
    /// plane), magnitude roughly in [-1,1] per axis. Implemented by <see cref="TiltInputHandler"/>
    /// and <see cref="JoystickInputHandler"/> so <see cref="BallController"/> doesn't care which
    /// one is currently active - that choice lives in <see cref="PlayerInputRouter"/>.
    /// </summary>
    public interface IMovementInputProvider
    {
        Vector2 GetMovementInput();
    }
}
