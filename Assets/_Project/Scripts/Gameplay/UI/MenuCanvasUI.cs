using _Project.Scripts.Architecture.Services.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI
{
    public class MenuCanvasUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject _shopScreen;

        [SerializeField]
        private GameObject _armyScreen;

        [SerializeField]
        private GameObject _levelMapScreen;

        [SerializeField]
        private Button _shopButton;

        [SerializeField]
        private Button _armyButton;

        [SerializeField]
        private Button _levelMapButton;

        private IAudioService _audioService;

        public void Initialize(IAudioService audioService)
        {
            _audioService = audioService;
        }

        private void OnEnable()
        {
            _shopButton.onClick.AddListener(ShowShop);
            _armyButton.onClick.AddListener(ShowArmy);
            _levelMapButton.onClick.AddListener(ShowLevelMap);
        }

        private void OnDisable()
        {
            _shopButton.onClick.RemoveListener(ShowShop);
            _armyButton.onClick.RemoveListener(ShowArmy);
            _levelMapButton.onClick.RemoveListener(ShowLevelMap);
        }

        private void ShowShop()
        {
            _audioService?.PlayUIClick();
            SetActiveScreen(_shopScreen);
        }

        private void ShowArmy()
        {
            _audioService?.PlayUIClick();
            SetActiveScreen(_armyScreen);
        }

        private void ShowLevelMap()
        {
            _audioService?.PlayUIClick();
            SetActiveScreen(_levelMapScreen);
        }

        private void SetActiveScreen(GameObject active)
        {
            _shopScreen.SetActive(_shopScreen == active);
            _armyScreen.SetActive(_armyScreen == active);
            _levelMapScreen.SetActive(_levelMapScreen == active);
        }
    }
}
