using _Project.Scripts.Gameplay.Character.Components.Health;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.Character.Components.UI
{
    public class HealthBarElement : MonoBehaviour
    {
        [SerializeField]
        private Image _fillImage;

        [SerializeField]
        private Vector3 _worldOffset = new(0f, 2.2f, 0f);

        [SerializeField]
        [Range(0f, 1f)]
        private float _lowHealthBrightness = 0.55f;

        private RectTransform _rectTransform;
        private Transform _target;
        private HealthComponent _health;
        private Camera _camera;
        private HealthBarPool _pool;
        private Color _teamColor;

        public void Bind(Transform target, HealthComponent health, Camera camera, HealthBarPool pool, Color teamColor)
        {
            _target = target;
            _health = health;
            _camera = camera;
            _pool = pool;
            _teamColor = teamColor;

            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            _health.OnHealthChanged += UpdateBar;
            UpdateBar(health.CurrentHealth, health.MaxHealth);
            gameObject.SetActive(true);
        }

        public void Release()
        {
            if (_health != null)
                _health.OnHealthChanged -= UpdateBar;

            _target = null;
            _health = null;
            gameObject.SetActive(false);
            _pool.Return(this);
        }

        private void UpdateBar(float current, float max)
        {
            float ratio = 0f;
            if (max > 0f)
            {
                ratio = current / max;
            }

            Color lowHealthColor = _teamColor * _lowHealthBrightness;
            lowHealthColor.a = _teamColor.a;

            _fillImage.fillAmount = ratio;
            _fillImage.color = Color.Lerp(lowHealthColor, _teamColor, ratio);
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;

            Vector3 screenPos = _camera.WorldToScreenPoint(_target.position + _worldOffset);

            if (screenPos.z < 0f)
            {
                _fillImage.enabled = false;
                return;
            }

            _fillImage.enabled = true;
            _rectTransform.position = screenPos;
        }
    }
}
