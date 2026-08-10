using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Gameplay.Inventory;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    public interface IArpgStartupService : IService
    {
        ArpgConfig Config { get; }
        void GrantStartingContent();
    }

    // стартовый контент выдаётся один раз за запуск, а не на каждой загрузке сцены
    public class ArpgStartupService : IArpgStartupService
    {
        private readonly IPlayerProgressService _progress;
        private readonly IEquipmentService _equipment;

        private bool _granted;

        public ArpgConfig Config { get; private set; }

        public ArpgStartupService(IPlayerProgressService progress, IEquipmentService equipment)
        {
            _progress = progress;
            _equipment = equipment;
        }

        public UniTask Initialize()
        {
            Config = Resources.Load<ArpgConfig>(ArpgConfig.ResourcePath);

            if (Config == null)
                Debug.LogError($"[ArpgStartupService] Нет конфига в Resources/{ArpgConfig.ResourcePath}");

            return UniTask.CompletedTask;
        }

        public void GrantStartingContent()
        {
            if (_granted || Config == null)
                return;

            _granted = true;

            // добираем до порога, а не прибавляем: иначе голда копилась бы за каждый запуск
            if (_progress.Gold < Config.StartingGold)
                _progress.AddGold(Config.StartingGold - _progress.Gold);

            if (Config.StartingItems != null)
            {
                for (int i = 0; i < Config.StartingItems.Length && i < _equipment.SlotCount; i++)
                {
                    if (Config.StartingItems[i] != null)
                        _equipment.Equip(Config.StartingItems[i], i);
                }
            }

            if (Config.StartingBackpack == null)
                return;

            foreach (ItemData item in Config.StartingBackpack)
                _equipment.AddToBackpack(item);
        }
    }
}
