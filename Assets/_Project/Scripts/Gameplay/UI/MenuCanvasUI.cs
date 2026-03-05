using _Project.Scripts.Gameplay.UI.Army;
using _Project.Scripts.Gameplay.UI.LevelMap;
using _Project.Scripts.Gameplay.UI.Shop;
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
            SetActiveScreen(_shopScreen);
        }

        private void ShowArmy()
        {
            SetActiveScreen(_armyScreen);
        }

        private void ShowLevelMap()
        {
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
