using System;
using UnityEngine;

namespace _Project.Scripts.Gameplay.UI.Shop
{
    public interface IShopScreenView
    {
        event Action RemoveAdsClicked;
        event Action ViewEnabled;

        void ClearCards();
        void AddUnitCard(string name, RenderTexture portrait, string price, bool canAfford, Action onBuy);
        void AddItemCard(string name, Sprite icon, string price, Action onBuy);
        void SetRemoveAdsVisible(bool visible);
        void SetRemoveAdsInteractable(bool interactable);
    }
}
