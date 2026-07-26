using _Project.Scripts.Gameplay.Character;
using _Project.Scripts.Gameplay.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Project.Scripts.Dev
{
    [RequireComponent(typeof(UIDocument))]
    public class HudView : MonoBehaviour
    {
        private Label _goldValue;
        private Label _expValue;
        private VisualElement _hpFill;

        private IPlayerProgressService _progress;
        private Character _player;

        public void Bind(IPlayerProgressService progress, Character player)
        {
            _progress = progress;
            _player = player;

            var root = GetComponent<UIDocument>().rootVisualElement;
            _goldValue = root.Q<Label>("gold-value");
            _expValue = root.Q<Label>("exp-value");
            _hpFill = root.Q<VisualElement>("hp-fill");

            _progress.OnGoldChanged += RefreshGold;
            _progress.OnExpChanged += RefreshExp;

            RefreshGold();
            RefreshExp();
        }

        private void OnDisable()
        {
            if (_progress == null)
                return;

            _progress.OnGoldChanged -= RefreshGold;
            _progress.OnExpChanged -= RefreshExp;
        }

        private void Update()
        {
            if (_player != null && _hpFill != null)
                _hpFill.style.width = Length.Percent(_player.HealthRatio * 100f);
        }

        private void RefreshGold()
        {
            if (_goldValue != null)
                _goldValue.text = _progress.Gold.ToString();
        }

        private void RefreshExp()
        {
            if (_expValue != null)
                _expValue.text = _progress.Exp.ToString();
        }
    }
}
