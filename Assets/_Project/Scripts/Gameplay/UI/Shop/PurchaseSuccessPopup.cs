using System;
using _Project.Scripts.Architecture.Services.Audio;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Gameplay.UI.Kit;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.UI.Shop
{
    public class PurchaseSuccessPopup : IDisposable
    {
        private const string PanelName = "purchase-panel";
        private const string IconName = "purchase-icon";
        private const string RewardName = "purchase-reward";
        private const string OkButtonName = "purchase-ok";
        private const string ShownClass = "popup--shown";

        private readonly VisualElement _root;
        private readonly VisualElement _panel;
        private readonly VisualElement _icon;
        private readonly Label _reward;
        private readonly Button _okButton;
        private readonly IAudioService _audioService;
        private readonly ILocalizationService _localization;

        public PurchaseSuccessPopup(VisualElement root, IAudioService audioService, ILocalizationService localization)
        {
            _root = root;
            _audioService = audioService;
            _localization = localization;

            _panel = root.Q(PanelName);
            _icon = root.Q(IconName);
            _reward = root.Q<Label>(RewardName);
            _okButton = root.Q<Button>(OkButtonName);

            _okButton.clicked += OnOkClicked;
            Hide();
        }

        public void Dispose()
        {
            _okButton.clicked -= OnOkClicked;
        }

        public void ShowItem(string itemName, Sprite icon)
        {
            _icon.SetImage(icon);
            Show(itemName);
        }

        public void ShowUnit(string unitName, RenderTexture portrait)
        {
            _icon.SetImage(portrait);
            Show(unitName);
        }

        private void Show(string displayName)
        {
            _reward.text = _localization.Get(LocalizationKeys.Shop.PurchaseSuccessReward, displayName);
            _panel.RemoveFromClassList(ShownClass);
            _root.SetDisplayed(true);
            _root.BringToFront();
            _panel.schedule.Execute(PlayPopIn);
        }

        private void PlayPopIn()
        {
            _panel.AddToClassList(ShownClass);
        }

        private void Hide()
        {
            _root.SetDisplayed(false);
        }

        private void OnOkClicked()
        {
            _audioService.PlayUIClick();
            Hide();
        }
    }
}
