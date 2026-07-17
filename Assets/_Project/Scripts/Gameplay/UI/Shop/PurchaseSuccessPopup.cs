using System;
using _Project.Scripts.Architecture.Services.Audio;
using _Project.Scripts.Architecture.Services.Localization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI.Shop
{
    /// <summary>
    /// Окно "покупка успешна / предмет получен". Показывается после успешной оплаты инаппа.
    /// Поддерживает Sprite (предметы) и RenderTexture (превью юнитов).
    /// </summary>
    public class PurchaseSuccessPopup : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField]
        private GameObject _root;

        [SerializeField]
        private RectTransform _panel;

        [Header("Content")]
        [SerializeField]
        private TextMeshProUGUI _titleText;

        [SerializeField]
        private Image _iconImage;

        [SerializeField]
        private RawImage _iconRaw;

        [SerializeField]
        private TextMeshProUGUI _rewardText;

        [Header("Button")]
        [SerializeField]
        private Button _okButton;

        [SerializeField]
        private TextMeshProUGUI _okButtonLabel;

        [Header("Animation")]
        [SerializeField]
        private float _popDuration = 0.25f;

        private IAudioService _audioService;
        private ILocalizationService _localization;
        private Action _onClosed;
        private Tween _popTween;

        public void Initialize(IAudioService audioService, ILocalizationService localization)
        {
            _audioService = audioService;
            _localization = localization;

            if (_okButton != null)
                _okButton.onClick.AddListener(OnOkClicked);

            ApplyStaticLabels();
            Hide();
        }

        private void OnDestroy()
        {
            if (_okButton != null)
                _okButton.onClick.RemoveListener(OnOkClicked);

            _popTween?.Kill();
        }

        /// <summary>Показать окно для предмета (иконка-спрайт).</summary>
        public void ShowItem(string itemName, Sprite icon, Action onClosed = null)
        {
            SetIconSprite(icon);
            ShowInternal(itemName, onClosed);
        }

        /// <summary>Показать окно для юнита (превью-RenderTexture).</summary>
        public void ShowUnit(string unitName, RenderTexture portrait, Action onClosed = null)
        {
            SetIconTexture(portrait);
            ShowInternal(unitName, onClosed);
        }

        private void ShowInternal(string displayName, Action onClosed)
        {
            _onClosed = onClosed;

            if (_titleText != null)
                _titleText.text = Loc(LocalizationKeys.Shop.PurchaseSuccessTitle, "Purchase successful!");

            if (_rewardText != null)
                _rewardText.text = LocFormat(LocalizationKeys.Shop.PurchaseSuccessReward, "{0} received", displayName);

            if (_root != null)
                _root.SetActive(true);

            transform.SetAsLastSibling();
            AnimateIn();
        }

        public void Hide()
        {
            _popTween?.Kill();

            if (_root != null)
                _root.SetActive(false);
        }

        private void OnOkClicked()
        {
            _audioService?.PlayUIClick();
            var callback = _onClosed;
            _onClosed = null;
            Hide();
            callback?.Invoke();
        }

        private void ApplyStaticLabels()
        {
            if (_okButtonLabel != null)
                _okButtonLabel.text = Loc(LocalizationKeys.Shop.PurchaseSuccessOk, "Great!");
        }

        private void SetIconSprite(Sprite icon)
        {
            if (_iconRaw != null)
                _iconRaw.gameObject.SetActive(false);

            if (_iconImage != null)
            {
                _iconImage.gameObject.SetActive(icon != null);
                if (icon != null)
                    _iconImage.sprite = icon;
            }
        }

        private void SetIconTexture(RenderTexture portrait)
        {
            if (_iconImage != null)
                _iconImage.gameObject.SetActive(false);

            if (_iconRaw != null)
            {
                _iconRaw.gameObject.SetActive(portrait != null);
                if (portrait != null)
                    _iconRaw.texture = portrait;
            }
        }

        private void AnimateIn()
        {
            if (_panel == null)
                return;

            _popTween?.Kill();
            _panel.localScale = Vector3.one * 0.8f;
            _popTween = _panel
                .DOScale(Vector3.one, _popDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        private string Loc(string key, string fallback)
        {
            if (_localization == null)
                return fallback;

            string value = _localization.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }

        private string LocFormat(string key, string fallback, params object[] args)
        {
            if (_localization == null)
                return string.Format(fallback, args);

            string value = _localization.Get(key);
            if (string.IsNullOrEmpty(value) || value == key)
                return string.Format(fallback, args);

            try
            {
                return string.Format(value, args);
            }
            catch (FormatException)
            {
                return string.Format(fallback, args);
            }
        }
    }
}
