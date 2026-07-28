using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.Merchant.UI
{
    public class RecipeLinks : VisualElement
    {
        private static readonly Color LineColor = new(0.25f, 0.25f, 0.25f);
        private const float LineWidth = 4f;

        private readonly List<(VisualElement From, VisualElement To)> _links = new();
        private readonly HashSet<VisualElement> _watched = new();

        public RecipeLinks()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;

            generateVisualContent += Draw;
        }

        // концы линий известны по worldBound, который наполняется только после раскладки,
        // поэтому слой перерисовывается по геометрии самих иконок, а не один раз при построении
        public void Set(IEnumerable<(VisualElement From, VisualElement To)> links)
        {
            foreach (VisualElement element in _watched)
                element.UnregisterCallback<GeometryChangedEvent>(HandleEndpointGeometryChanged);

            _watched.Clear();
            _links.Clear();
            _links.AddRange(links);

            foreach ((VisualElement from, VisualElement to) in _links)
            {
                Watch(from);
                Watch(to);
            }

            MarkDirtyRepaint();
        }

        private void Watch(VisualElement element)
        {
            if (element == null || !_watched.Add(element))
                return;

            element.RegisterCallback<GeometryChangedEvent>(HandleEndpointGeometryChanged);
        }

        private void HandleEndpointGeometryChanged(GeometryChangedEvent evt)
        {
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            if (_links.Count == 0)
                return;

            Painter2D painter = context.painter2D;
            painter.lineWidth = LineWidth;
            painter.strokeColor = LineColor;

            foreach ((VisualElement from, VisualElement to) in _links)
            {
                if (from?.panel == null || to?.panel == null)
                    continue;

                painter.BeginPath();
                painter.MoveTo(this.WorldToLocal(from.worldBound.center));
                painter.LineTo(this.WorldToLocal(to.worldBound.center));
                painter.Stroke();
            }
        }
    }
}
