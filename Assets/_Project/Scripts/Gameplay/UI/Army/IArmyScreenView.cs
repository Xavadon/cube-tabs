using System;
using UnityEngine;

namespace _Project.Scripts.Gameplay.UI.Army
{
    public interface IArmyScreenView
    {
        event Action BuyClicked;
        event Action TransferClicked;
        event Action<int> CardClicked;
        event Action ViewEnabled;

        EvolutionPanelUI EvolutionPanel { get; }

        void SetActive(bool active);
        void ClearCards();
        void AddCard(string name, int count, RenderTexture portrait, bool isInArmy);
        void SetCardSelected(int index, bool selected);
        void SetSlotCount(string text);
        void SetGoldText(string text);
        void SetBuyInteractable(bool interactable);
        void SetBuyCost(string text);
        void SetTransferVisible(bool visible);
        void SetTransferLabel(string text);
        void SetTransferInteractable(bool interactable);
        void ShowFullBodyPreview(RenderTexture rt);
        void HideFullBodyPreview();
    }
}
