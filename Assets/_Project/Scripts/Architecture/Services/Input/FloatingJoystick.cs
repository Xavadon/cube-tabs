using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace _Project.Scripts.Architecture.Services.Input
{
    public class FloatingJoystick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _stickBase;
        [SerializeField] private RectTransform _handle;
        [SerializeField] private CanvasGroup _visuals;
        [SerializeField] private float _radius = 150f;
        [SerializeField] private float _deadZone = 0.12f;
        [SerializeField] private bool _trailing = true;

        [InputControl(layout = "Vector2")]
        [SerializeField] private string _controlPath = "<Gamepad>/leftStick";

        protected override string controlPathInternal
        {
            get => _controlPath;
            set => _controlPath = value;
        }

        private RectTransform _zone;
        private UnityEngine.Camera _uiCamera;
        private Vector2 _origin;
        private int _pointerId = InvalidPointer;

        private const int InvalidPointer = -64;

        private void Awake()
        {
            _zone = (RectTransform)transform;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                _uiCamera = canvas.worldCamera;

            SetVisible(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId != InvalidPointer || !TryGetLocal(eventData, out Vector2 local))
                return;

            _pointerId = eventData.pointerId;
            _origin = local;

            _stickBase.anchoredPosition = _origin;
            _handle.anchoredPosition = _origin;

            SetVisible(true);
            SendValueToControl(Vector2.zero);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId || !TryGetLocal(eventData, out Vector2 local))
                return;

            Vector2 offset = local - _origin;

            if (offset.magnitude > _radius)
            {
                Vector2 clamped = offset.normalized * _radius;

                if (_trailing)
                {
                    _origin = local - clamped;
                    _stickBase.anchoredPosition = _origin;
                }

                offset = clamped;
            }

            _handle.anchoredPosition = _origin + offset;
            SendValueToControl(ApplyDeadZone(offset / _radius));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId)
                return;

            _pointerId = InvalidPointer;

            SetVisible(false);
            SendValueToControl(Vector2.zero);
        }

        private Vector2 ApplyDeadZone(Vector2 value)
        {
            float magnitude = value.magnitude;

            if (magnitude <= _deadZone)
                return Vector2.zero;

            float scaled = (magnitude - _deadZone) / (1f - _deadZone);
            return value.normalized * Mathf.Clamp01(scaled);
        }

        private bool TryGetLocal(PointerEventData eventData, out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _zone, eventData.position, _uiCamera, out local);
        }

        private void SetVisible(bool visible)
        {
            if (_visuals != null)
                _visuals.alpha = visible ? 1f : 0f;
        }
    }
}
