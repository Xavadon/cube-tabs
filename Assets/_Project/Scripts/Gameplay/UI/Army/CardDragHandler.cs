using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.UI.Army
{
    public class CardDragHandler
    {
        private const float MouseDragThreshold = 12f;
        private const float TouchDragThreshold = 24f;
        private const string GhostClass = "unit-card--ghost";
        private const string DraggingClass = "unit-card--dragging";
        private const string DropTargetClass = "army-section--drop-target";

        private readonly VisualElement _layer;
        private readonly VisualElement _armyZone;
        private readonly VisualElement _reserveZone;

        private VisualElement _card;
        private VisualElement _ghost;
        private VisualElement _targetZone;
        private int _pointerId;
        private Vector2 _pressPosition;
        private Vector2 _grabOffset;
        private bool _isTouch;

        public event Action<VisualElement> Clicked;
        public event Action<VisualElement, bool> Dropped;

        public CardDragHandler(VisualElement layer, VisualElement armyZone, VisualElement reserveZone)
        {
            _layer = layer;
            _armyZone = armyZone;
            _reserveZone = reserveZone;
            _layer.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
        }

        public void Register(VisualElement card)
        {
            card.RegisterCallback<PointerDownEvent>(OnPointerDown);
            card.RegisterCallback<PointerUpEvent>(OnPointerUp);
            card.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        public void Cancel()
        {
            if (_card == null)
            {
                return;
            }

            var card = _card;
            _card = null;

            if (_ghost != null)
            {
                _ghost.RemoveFromHierarchy();
                _ghost = null;
            }

            if (_targetZone != null)
            {
                _targetZone.RemoveFromClassList(DropTargetClass);
                _targetZone = null;
            }

            card.RemoveFromClassList(DraggingClass);

            if (card.HasPointerCapture(_pointerId))
            {
                card.ReleasePointer(_pointerId);
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (_card != null || evt.button != 0)
            {
                return;
            }

            _card = (VisualElement)evt.currentTarget;
            _pointerId = evt.pointerId;
            _pressPosition = evt.position;
            _isTouch = evt.pointerType != UnityEngine.UIElements.PointerType.mouse;
            _card.CapturePointer(_pointerId);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!IsTracking(evt.pointerId))
            {
                return;
            }

            if (_ghost == null)
            {
                Vector2 position = evt.position;
                Vector2 delta = position - _pressPosition;

                if (delta.magnitude < GetDragThreshold())
                {
                    evt.StopPropagation();
                    return;
                }

                if (_isTouch && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    Cancel();
                    return;
                }

                BeginDrag();
            }

            MoveGhost(evt.position);
            evt.StopPropagation();
        }

        private float GetDragThreshold()
        {
            if (_isTouch)
            {
                return TouchDragThreshold;
            }

            return MouseDragThreshold;
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!IsTracking(evt.pointerId))
            {
                return;
            }

            var card = _card;
            var targetZone = _targetZone;
            bool dragged = _ghost != null;

            Cancel();

            if (!dragged)
            {
                Clicked?.Invoke(card);
                return;
            }

            if (targetZone.worldBound.Contains(evt.position))
            {
                Dropped?.Invoke(card, targetZone == _armyZone);
            }
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            Cancel();
        }

        private bool IsTracking(int pointerId)
        {
            return _card != null && pointerId == _pointerId;
        }

        private void BeginDrag()
        {
            _grabOffset = _card.WorldToLocal(_pressPosition);
            _ghost = CloneVisual(_card);
            _ghost.AddToClassList(GhostClass);
            _layer.Add(_ghost);

            _card.AddToClassList(DraggingClass);

            if (_armyZone.Contains(_card))
            {
                _targetZone = _reserveZone;
            }
            else
            {
                _targetZone = _armyZone;
            }

            _targetZone.AddToClassList(DropTargetClass);
        }

        private void MoveGhost(Vector2 pointerPosition)
        {
            Vector2 position = _layer.WorldToLocal(pointerPosition) - _grabOffset;
            _ghost.style.left = position.x;
            _ghost.style.top = position.y;
        }

        private static VisualElement CloneVisual(VisualElement source)
        {
            VisualElement clone;

            if (source is Label label)
            {
                clone = new Label(label.text);
            }
            else
            {
                clone = new VisualElement();
            }

            foreach (string className in source.GetClasses())
            {
                clone.AddToClassList(className);
            }

            clone.style.backgroundImage = source.style.backgroundImage;
            clone.pickingMode = PickingMode.Ignore;

            for (int i = 0; i < source.childCount; i++)
            {
                clone.Add(CloneVisual(source[i]));
            }

            return clone;
        }
    }
}
