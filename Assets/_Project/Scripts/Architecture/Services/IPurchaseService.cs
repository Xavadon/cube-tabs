using System;
using _Project.Scripts.Gameplay.Character.Data;

namespace _Project.Scripts.Architecture.Services
{
    public interface IPurchaseService : IService
    {
        void Purchase(ShopItemData item, Action onSuccess, Action onFailure);
    }
}
