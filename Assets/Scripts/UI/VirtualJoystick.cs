using UnityEngine;
using UnityEngine.EventSystems;

namespace Test1.Combat.UI
{
    [DisallowMultipleComponent]
    public sealed class VirtualJoystick : MonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler
    {
        private const float MINIMUM_RADIUS = 0.001f;

        [SerializeField]
        private RectTransform background;

        [SerializeField]
        private RectTransform handle;

        [SerializeField]
        [Range(0.1f, 1f)]
        private float handleRange = 0.72f;

        public Vector2 Direction { get; private set; }

        private void Awake()
        {
            background = GetComponent<RectTransform>();
            if (handle == null)
            {
                throw new MissingReferenceException(
                    $"{nameof(VirtualJoystick)} on '{name}' requires a " +
                    "handle RectTransform.");
            }
        }

        private void OnDisable()
        {
            ResetHandle();
        }

        public void Configure(
            RectTransform targetBackground,
            RectTransform targetHandle,
            float range = 0.72f)
        {
            background = targetBackground;
            handle = targetHandle;
            handleRange = Mathf.Clamp(range, 0.1f, 1f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    background,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint) == false)
            {
                return;
            }

            float radius = Mathf.Min(
                               background.rect.width,
                               background.rect.height) *
                           0.5f *
                           handleRange;
            if (radius <= MINIMUM_RADIUS)
            {
                return;
            }

            Direction = Vector2.ClampMagnitude(
                localPoint / radius,
                1f);
            handle.anchoredPosition = Direction * radius;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ResetHandle();
        }

        private void ResetHandle()
        {
            Direction = Vector2.zero;
            handle.anchoredPosition = Vector2.zero;
        }
    }
}
