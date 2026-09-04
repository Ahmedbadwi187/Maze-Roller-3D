using System;
using UnityEngine;

namespace MazeRoller3D.Gameplay
{
    /// <summary>
    /// Placed on a trigger collider at the maze's exit cell. Fires <see cref="LevelCompleted"/>
    /// once when the ball enters it - identified by the presence of a <see cref="BallController"/>
    /// up the hierarchy rather than a tag, so no separate tag setup is needed. Fire-once
    /// guarded so bouncing back out (or physics jitter) doesn't retrigger completion; call
    /// <see cref="ResetTrigger"/> when a level restarts to re-arm it.
    ///
    /// Deliberately has no knowledge of UI/overlays - callers (see
    /// MazeRoller3D.UI.LevelFlowController) decide what "level complete" means for the
    /// player. Keeps this assembly free of a dependency on the UI layer.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LevelExitTrigger : MonoBehaviour
    {
        public event Action LevelCompleted;

        private bool _triggered;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered) return;
            if (other.GetComponentInParent<BallController>() == null) return;

            _triggered = true;
            LevelCompleted?.Invoke();
        }

        public void ResetTrigger() => _triggered = false;
    }
}
