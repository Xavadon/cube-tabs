using System;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Gameplay.Character;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.UI.Arpg;
using UnityEngine.UIElements;

namespace _Project.Scripts.Dev
{
    public class HudView : UIDocumentView
    {
        private Label _goldValue;
        private Label _expValue;
        private VisualElement _hpFill;

        private IPlayerProgressService _progress;
        private ILocalizationService _localization;
        private Character _player;

        public event Action OnCharacterClicked;
        public event Action OnMerchantClicked;

        public void Bind(IPlayerProgressService progress, ILocalizationService localization, Character player)
        {
            _progress = progress;
            _localization = localization;
            _player = player;

            _progress.OnGoldChanged += RefreshGold;
            _progress.OnExpChanged += RefreshExp;

            StartWiring();
        }

        protected override void Wire()
        {
            _goldValue = Root.Q<Label>("gold-value");
            _expValue = Root.Q<Label>("exp-value");
            _hpFill = Root.Q<VisualElement>("hp-fill");

            var characterButton = Root.Q<Button>("character-button");
            characterButton.text = _localization.Get(LocalizationKeys.Arpg.HudCharacter);
            characterButton.clicked += () => OnCharacterClicked?.Invoke();

            var merchantButton = Root.Q<Button>("shop-button");
            merchantButton.text = _localization.Get(LocalizationKeys.Arpg.HudMerchant);
            merchantButton.clicked += () => OnMerchantClicked?.Invoke();

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

        protected override void Update()
        {
            base.Update();

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
