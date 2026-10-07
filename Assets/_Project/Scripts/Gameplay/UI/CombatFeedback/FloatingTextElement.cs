using _Project.Scripts.Gameplay.Services.CombatFeedback;
using TMPro;
using UnityEngine;

namespace _Project.Scripts.Gameplay.UI.CombatFeedback
{
    public class FloatingTextElement : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _text;

        [SerializeField]
        private AnimationCurve _scaleCurve = new(
            new Keyframe(0f, 0.4f),
            new Keyframe(0.12f, 1.25f),
            new Keyframe(0.3f, 1f),
            new Keyframe(1f, 0.9f));

        [SerializeField]
        private AnimationCurve _alphaCurve = new(
            new Keyframe(0f, 1f),
            new Keyframe(0.6f, 1f),
            new Keyframe(1f, 0f));

        [SerializeField]
        private AnimationCurve _riseCurve = new(
            new Keyframe(0f, 0f, 0f, 2.5f),
            new Keyframe(1f, 1f, 0f, 0f));

        private FloatingTextPool _pool;
        private RectTransform _rectTransform;
        private Camera _camera;
        private Vector3 _worldPosition;
        private float _horizontalOffset;
        private float _risePixels;
        private float _scale;
        private float _lifetime;
        private float _elapsed;

        public void Setup(FloatingTextPool pool)
        {
            _pool = pool;
            _rectTransform = (RectTransform)transform;
        }

        public void Play(Vector3 worldPosition, float value, FloatingTextStyle style, float scale, float horizontalOffset, Camera camera)
        {
            _camera = camera;
            _worldPosition = worldPosition + style.WorldOffset;
            _horizontalOffset = horizontalOffset;
            _risePixels = style.RisePixels;
            _scale = scale;
            _lifetime = style.Lifetime;
            _elapsed = 0f;

            _text.fontSize = style.FontSize;
            _text.color = style.Color;
            _text.SetText(style.Format, value);

            gameObject.SetActive(true);
            UpdateView(0f);
        }

        private void LateUpdate()
        {
            _elapsed += Time.deltaTime;

            float progress = _elapsed / _lifetime;
            if (progress >= 1f)
            {
                _pool.Return(this);
                return;
            }

            UpdateView(progress);
        }

        private void UpdateView(float progress)
        {
            Vector3 screenPosition = _camera.WorldToScreenPoint(_worldPosition);
            if (screenPosition.z < 0f)
            {
                _text.enabled = false;
                return;
            }

            _text.enabled = true;
            screenPosition.x += _horizontalOffset;
            screenPosition.y += _risePixels * _riseCurve.Evaluate(progress);
            screenPosition.z = 0f;

            _rectTransform.position = screenPosition;
            _rectTransform.localScale = Vector3.one * (_scale * _scaleCurve.Evaluate(progress));
            _text.alpha = _alphaCurve.Evaluate(progress);
        }
    }
}
