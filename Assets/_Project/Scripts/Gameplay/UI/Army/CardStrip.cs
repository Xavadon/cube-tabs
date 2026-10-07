using System;
using _Project.Scripts.Architecture.Services.Audio;
using _Project.Scripts.Gameplay.UI.Kit;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.UI.Army
{
    public class CardStrip : IDisposable
    {
        private const float ScrollDuration = 0.25f;
        private const float EdgeTolerance = 1f;

        private readonly ScrollView _scroll;
        private readonly Button _prevButton;
        private readonly Button _nextButton;
        private readonly IAudioService _audioService;

        private Tween _scrollTween;

        public CardStrip(ScrollView scroll, Button prevButton, Button nextButton, IAudioService audioService)
        {
            _scroll = scroll;
            _prevButton = prevButton;
            _nextButton = nextButton;
            _audioService = audioService;

            _prevButton.clicked += OnPrevClicked;
            _nextButton.clicked += OnNextClicked;
            _scroll.horizontalScroller.valueChanged += OnScrolled;
            _scroll.contentViewport.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            _scroll.contentContainer.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            RefreshArrows();
        }

        public void Dispose()
        {
            _scrollTween?.Kill();

            _prevButton.clicked -= OnPrevClicked;
            _nextButton.clicked -= OnNextClicked;
            _scroll.horizontalScroller.valueChanged -= OnScrolled;
            _scroll.contentViewport.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            _scroll.contentContainer.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnPrevClicked()
        {
            ScrollPages(-1);
        }

        private void OnNextClicked()
        {
            ScrollPages(1);
        }

        private void OnScrolled(float offset)
        {
            RefreshArrows();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            RefreshArrows();
        }

        private void ScrollPages(int direction)
        {
            _audioService.PlayUIClick();

            float stride = GetCardStride();

            if (stride <= 0f)
            {
                return;
            }

            int cardsPerPage = Mathf.Max(1, Mathf.FloorToInt(_scroll.contentViewport.layout.width / stride));
            float alignedOffset = Mathf.Round(GetOffset() / stride) * stride;
            float target = Mathf.Clamp(alignedOffset + direction * cardsPerPage * stride, 0f, GetMaxOffset());

            _scrollTween?.Kill();
            _scrollTween = DOTween.To(GetOffset, SetOffset, target, ScrollDuration).SetEase(Ease.OutCubic);
        }

        private void RefreshArrows()
        {
            float offset = GetOffset();
            float maxOffset = GetMaxOffset();
            bool scrollable = maxOffset > EdgeTolerance;

            _prevButton.SetVisible(scrollable);
            _nextButton.SetVisible(scrollable);
            _prevButton.SetEnabled(offset > EdgeTolerance);
            _nextButton.SetEnabled(offset < maxOffset - EdgeTolerance);
        }

        private float GetCardStride()
        {
            var content = _scroll.contentContainer;

            if (content.childCount == 0)
            {
                return 0f;
            }

            var card = content[0];
            return card.layout.width + card.resolvedStyle.marginLeft + card.resolvedStyle.marginRight;
        }

        private float GetMaxOffset()
        {
            var content = _scroll.contentContainer;

            if (content.childCount == 0)
            {
                return 0f;
            }

            float contentWidth = content[content.childCount - 1].layout.xMax;
            return Mathf.Max(0f, contentWidth - _scroll.contentViewport.layout.width);
        }

        private float GetOffset()
        {
            return _scroll.scrollOffset.x;
        }

        private void SetOffset(float offset)
        {
            _scroll.scrollOffset = new Vector2(offset, 0f);
        }
    }
}
