using DG.Tweening;
using UnityEngine;

namespace _Project.Scripts.Gameplay.UI
{
    public enum UIAnimationType
    {
        None,
        RotateContinuous,
        PulsateScale,
        FloatUpDown
    }

    public class DOTweenAnimationComponent : MonoBehaviour
    {
        [SerializeField]
        private UIAnimationType _animationType = UIAnimationType.None;

        [SerializeField]
        private float _duration = 10f;

        [SerializeField]
        private float _strength = 360f;

        [SerializeField]
        private Ease _ease = Ease.Linear;

        private Tween _tween;

        private void Awake()
        {
            StartAnimation();
        }

        private void OnDestroy()
        {
            _tween?.Kill();
        }

        private void StartAnimation()
        {
            _tween?.Kill();

            switch (_animationType)
            {
                case UIAnimationType.RotateContinuous:
                    _tween = transform
                        .DORotate(new Vector3(0, 0, -_strength), _duration, RotateMode.FastBeyond360)
                        .SetLoops(-1, LoopType.Restart)
                        .SetEase(_ease);
                    break;

                case UIAnimationType.PulsateScale:
                    var originalScale = transform.localScale;
                    _tween = transform
                        .DOScale(originalScale * (1f + _strength * 0.01f), _duration)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetEase(_ease);
                    break;

                case UIAnimationType.FloatUpDown:
                    _tween = transform
                        .DOLocalMoveY(transform.localPosition.y + _strength, _duration)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetEase(_ease);
                    break;
            }
        }
    }
}
