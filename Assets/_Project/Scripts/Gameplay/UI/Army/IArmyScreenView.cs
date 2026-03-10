using System;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.UI.Shop;
using UnityEngine;

namespace _Project.Scripts.Gameplay.UI.Army
{
    public interface IArmyScreenView
    {
        event Action BuyClicked;
        event Action TransferClicked;
        event Action<int> CardClicked;
        event Action ViewEnabled;

        void SetActive(bool active);
        void ClearCards();
        void AddCard(string name, int count, RenderTexture portrait, bool isInArmy);
        void SetCardSelected(int index, bool selected);
        void SetTransferVisible(bool visible);
        void SetTransferLabel(string text);
        void SetTransferInteractable(bool interactable);
        void ShowFullBodyPreview(PreviewHandle handle);
        void HideFullBodyPreview();
        void ShowEvolution(int ownedIndex, CharacterData data);
        void HideEvolution();
    }
}
