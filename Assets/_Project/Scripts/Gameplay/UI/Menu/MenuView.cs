using _Project.Scripts.Architecture.Services.Audio;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Gameplay.UI.Kit;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.UI.Menu
{
    [RequireComponent(typeof(UIDocument))]
    public class MenuView : MonoBehaviour
    {
        private const string NavName = "nav";
        private const string MapButtonName = "map-button";
        private const string ArmyButtonName = "army-button";
        private const string ShopButtonName = "shop-button";

        [SerializeField]
        private UIDocument _document;

        private IAudioService _audioService;
        private ILocalizationService _localization;
        private IMenuScreen _levelMap;
        private IMenuScreen _army;
        private IMenuScreen _shop;
        private Button _mapButton;
        private Button _armyButton;
        private Button _shopButton;

        public void Initialize(IAudioService audioService, ILocalizationService localization,
            IMenuScreen levelMap, IMenuScreen army, IMenuScreen shop)
        {
            _audioService = audioService;
            _localization = localization;
            _localization.OnLanguageChanged += UpdateLocalizedTexts;
            _levelMap = levelMap;
            _army = army;
            _shop = shop;

            var nav = _document.rootVisualElement.Q(NavName);
            UILocalization.Apply(nav, localization);

            _mapButton = nav.Q<Button>(MapButtonName);
            _armyButton = nav.Q<Button>(ArmyButtonName);
            _shopButton = nav.Q<Button>(ShopButtonName);

            _mapButton.clicked += ShowLevelMap;
            _armyButton.clicked += ShowArmy;
            _shopButton.clicked += ShowShop;
        }

        private void OnDestroy()
        {
            if (_localization != null)
            {
                _localization.OnLanguageChanged -= UpdateLocalizedTexts;
            }

            if (_mapButton == null)
            {
                return;
            }

            _mapButton.clicked -= ShowLevelMap;
            _armyButton.clicked -= ShowArmy;
            _shopButton.clicked -= ShowShop;
        }

        private void ShowLevelMap()
        {
            _audioService.PlayUIClick();
            SetActiveScreen(_levelMap);
        }

        private void UpdateLocalizedTexts()
        {
            UILocalization.Apply(_document.rootVisualElement.Q(NavName), _localization);
        }

        private void ShowArmy()
        {
            _audioService.PlayUIClick();
            SetActiveScreen(_army);
        }

        private void ShowShop()
        {
            _audioService.PlayUIClick();
            SetActiveScreen(_shop);
        }

        private void SetActiveScreen(IMenuScreen active)
        {
            ToggleScreen(_levelMap, active);
            ToggleScreen(_army, active);
            ToggleScreen(_shop, active);
        }

        private static void ToggleScreen(IMenuScreen screen, IMenuScreen active)
        {
            if (screen == active)
            {
                screen.Show();
            }
            else
            {
                screen.Hide();
            }
        }
    }
}
