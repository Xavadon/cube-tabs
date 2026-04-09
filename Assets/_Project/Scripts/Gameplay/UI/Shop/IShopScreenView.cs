using System;
using UnityEngine;

namespace _Project.Scripts.Gameplay.UI.Shop
{
    public interface IShopScreenView
    {
        event Action<int> CardClicked;
        event Action BuyClicked;
        event Action ViewEnabled;

        void ClearCards();
        void AddHeroCard(string name, RenderTexture portrait);
        void AddItemCard(string name, Sprite icon);
        void SetCardSelected(int index, bool selected);
        void ShowUnitPreview(PreviewHandle handle, string name, string description, float hp, float damage, float speed);
        void ShowItemPreview(Sprite icon, string name, string description);
        void HidePreview();
        void SetBuyVisible(bool visible);
        void SetBuyInteractable(bool interactable);
        void SetBuyLabel(string text);
        void SetPriceLabel(string text);
    }
}
