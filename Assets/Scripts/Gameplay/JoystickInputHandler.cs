using UnityEngine;
using UnityEngine.EventSystems;

namespace MazeRoller3D.Gameplay
{
    /// <summary>
    /// On-screen virtual joystick: drag anywhere inside <see cref="background"/> and
    /// <see cref="handle"/> follows the drag, clamped to <see cref="handleRange"/> pixels from
    /// center. Movement input is the handle's offset from center, normalized to [-1,1] per
    /// axis. Fallback/alternate control scheme to <see cref="TiltInputHandler"/>, selected via
    /// <see cref="PlayerInputRouter"/>.
    /// </summary>
    public class JoystickInputHandler : MonoBehaviour, IMovementInputProvider, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;
        [SerializeField] private float handleRange = 100f;

        private Vector2 _input;

        public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

        public void OnDrag(PointerEventData eventData)
        {
            if (background == null || handle == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, eventData.position, eventData.pressEventCamera, out var localPoint);

            var clamped = Vector2.ClampMagnitude(localPoint, handleRange);
            handle.anchoredPosition = clamped;
            _input = clamped / handleRange;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _input = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }

        public Vector2 GetMovementInput() => _input;
    }
}
