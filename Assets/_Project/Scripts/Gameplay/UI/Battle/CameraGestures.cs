using System;
using _Project.Scripts.Architecture.Services.Camera;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.UI.Battle
{
    public class CameraGestures : IDisposable
    {
        private const int NoPointer = -1;
        private const float TapMaxTravel = 24f;
        private const float DoubleTapInterval = 0.3f;
        private const float MinPinchDistance = 1f;

        private readonly VisualElement _surface;
        private readonly ICameraService _cameraService;

        private int _firstId = NoPointer;
        private Vector2 _firstPosition;
        private int _secondId = NoPointer;
        private Vector2 _secondPosition;
        private float _tapTravel;
        private float _lastTapTime = float.NegativeInfinity;

        public CameraGestures(VisualElement surface, ICameraService cameraService)
        {
            _surface = surface;
            _cameraService = cameraService;

            _surface.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _surface.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _surface.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _surface.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            _surface.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        public void Dispose()
        {
            _surface.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            _surface.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _surface.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            _surface.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            _surface.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }

            if (_firstId == NoPointer)
            {
                _firstId = evt.pointerId;
                _firstPosition = evt.position;
                _tapTravel = 0f;
            }
            else if (_secondId == NoPointer)
            {
                _secondId = evt.pointerId;
                _secondPosition = evt.position;
                _tapTravel = float.MaxValue;
            }
            else
            {
                return;
            }

            _surface.CapturePointer(evt.pointerId);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            Vector2 position = evt.position;

            if (evt.pointerId == _firstId)
            {
                if (_secondId == NoPointer)
                {
                    Vector2 delta = position - _firstPosition;
                    _tapTravel += delta.magnitude;
                    Pan(delta);
                }
                else
                {
                    MoveTwoFingers(position, _secondPosition);
                }

                _firstPosition = position;
            }
            else if (evt.pointerId == _secondId)
            {
                MoveTwoFingers(_firstPosition, position);
                _secondPosition = position;
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId == _firstId && _secondId == NoPointer && _tapTravel < TapMaxTravel)
            {
                RegisterTap();
            }

            Release(evt.pointerId);
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            Release(evt.pointerId);
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            Release(evt.pointerId);
        }

        private void MoveTwoFingers(Vector2 first, Vector2 second)
        {
            Vector2 previousCenter = (_firstPosition + _secondPosition) * 0.5f;
            Vector2 center = (first + second) * 0.5f;
            Pan(center - previousCenter);

            float previousDistance = Vector2.Distance(_firstPosition, _secondPosition);
            float distance = Vector2.Distance(first, second);
            if (previousDistance > MinPinchDistance && distance > MinPinchDistance)
            {
                _cameraService.Zoom(previousDistance / distance);
            }
        }

        private void Pan(Vector2 delta)
        {
            float height = _surface.layout.height;
            if (height <= 0f)
            {
                return;
            }

            _cameraService.Pan(delta / height);
        }

        private void RegisterTap()
        {
            float now = Time.unscaledTime;
            if (now - _lastTapTime <= DoubleTapInterval)
            {
                _cameraService.ResetUserView();
                _lastTapTime = float.NegativeInfinity;
                return;
            }

            _lastTapTime = now;
        }

        private void Release(int pointerId)
        {
            if (pointerId == _firstId)
            {
                _firstId = _secondId;
                _firstPosition = _secondPosition;
                _secondId = NoPointer;
            }
            else if (pointerId == _secondId)
            {
                _secondId = NoPointer;
            }
            else
            {
                return;
            }

            if (_surface.HasPointerCapture(pointerId))
            {
                _surface.ReleasePointer(pointerId);
            }
        }
    }
}
