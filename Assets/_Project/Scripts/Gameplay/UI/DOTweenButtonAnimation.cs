using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI
{
    public enum ButtonAnimationType
    {
        None,
        Punch,
        Shake,
        Pop
    }

    [RequireComponent(typeof(Button))]
    public class DOTweenButtonAnimation : MonoBehaviour
    {
        [SerializeField]
        private ButtonAnimationType _animationType = ButtonAnimationType.Punch;

        [SerializeField]
        private float _duration = 0.3f;

        [SerializeField]
        private float _strength = 0.2f;

        [SerializeField]
        private Ease _ease = Ease.OutBack;

        private Button _button;
        private Tween _tween;
        private Vector3 _originalScale;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _originalScale = transform.localScale;
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(PlayAnimation);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(PlayAnimation);
            _tween?.Kill();
            transform.localScale = _originalScale;
        }

        private void PlayAnimation()
        {
            _tween?.Kill();
            transform.localScale = _originalScale;

            switch (_animationType)
            {
                case ButtonAnimationType.Punch:
                    _tween = transform
                        .DOPunchScale(Vector3.one * _strength, _duration)
                        .SetEase(_ease);
                    break;

                case ButtonAnimationType.Shake:
                    _tween = transform
                        .DOShakeScale(_duration, _strength)
                        .SetEase(_ease);
                    break;

                case ButtonAnimationType.Pop:
                    _tween = transform
                        .DOScale(_originalScale * (1f + _strength), _duration * 0.5f)
                        .SetEase(_ease)
                        .OnComplete(() =>
                        {
                            _tween = transform
                                .DOScale(_originalScale, _duration * 0.5f)
                                .SetEase(Ease.OutBounce);
                        });
                    break;
            }
        }
    }
}
