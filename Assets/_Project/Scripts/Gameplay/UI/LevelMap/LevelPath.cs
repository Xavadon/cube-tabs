using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.UI.LevelMap
{
    [UxmlElement]
    public partial class LevelPath : VisualElement
    {
        private const float DefaultWidth = 4f;
        private const float DefaultDash = 12f;
        private const float DefaultGap = 8f;

        private static readonly CustomStyleProperty<Color> ColorProperty = new("--path-color");
        private static readonly CustomStyleProperty<float> WidthProperty = new("--path-width");
        private static readonly CustomStyleProperty<float> DashProperty = new("--path-dash");
        private static readonly CustomStyleProperty<float> GapProperty = new("--path-gap");

        private IReadOnlyList<VisualElement> _points = Array.Empty<VisualElement>();
        private Color _color = Color.red;
        private float _width = DefaultWidth;
        private float _dash = DefaultDash;
        private float _gap = DefaultGap;

        public LevelPath()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        public void SetPoints(IReadOnlyList<VisualElement> points)
        {
            _points = points;
            MarkDirtyRepaint();
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            ICustomStyle style = evt.customStyle;

            if (style.TryGetValue(ColorProperty, out Color color))
            {
                _color = color;
            }

            if (style.TryGetValue(WidthProperty, out float width))
            {
                _width = width;
            }

            if (style.TryGetValue(DashProperty, out float dash))
            {
                _dash = dash;
            }

            if (style.TryGetValue(GapProperty, out float gap))
            {
                _gap = gap;
            }

            MarkDirtyRepaint();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            Painter2D painter = context.painter2D;
            painter.strokeColor = _color;
            painter.lineWidth = _width;
            painter.lineCap = LineCap.Butt;
            painter.BeginPath();

            bool hasPrevious = false;
            Vector2 previous = Vector2.zero;

            for (int i = 0; i < _points.Count; i++)
            {
                VisualElement point = _points[i];

                if (!point.enabledSelf || point.resolvedStyle.display == DisplayStyle.None)
                {
                    continue;
                }

                Vector2 center = this.WorldToLocal(point.worldBound.center);

                if (hasPrevious)
                {
                    AddDashes(painter, previous, center);
                }

                previous = center;
                hasPrevious = true;
            }

            painter.Stroke();
        }

        private void AddDashes(Painter2D painter, Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            float step = _dash + _gap;

            if (_dash <= 0f || step <= 0f)
            {
                return;
            }

            Vector2 direction = delta / length;

            for (float offset = 0f; offset < length; offset += step)
            {
                float end = Mathf.Min(offset + _dash, length);
                painter.MoveTo(from + direction * offset);
                painter.LineTo(from + direction * end);
            }
        }
    }
}
