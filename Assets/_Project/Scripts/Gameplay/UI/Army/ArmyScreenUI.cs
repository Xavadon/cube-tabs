using System.Collections.Generic;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Services;
using TMPro;
using UnityEngine;

namespace _Project.Scripts.Gameplay.UI.Army
{
    public class ArmyScreenUI : MonoBehaviour
    {
        [SerializeField]
        private ArmyUnitCardUI _cardPrefab;

        [Header("Army")]
        [SerializeField]
        private Transform _armyContainer;

        [Header("Backlog")]
        [SerializeField]
        private Transform _backlogContainer;

        [SerializeField]
        private TextMeshProUGUI _slotCountLabel;

        private IPlayerProgressService _progress;

        public void Initialize(IPlayerProgressService progress)
        {
            _progress = progress;
            _progress.OnArmyChanged += Rebuild;
            _progress.OnOwnedChanged += Rebuild;

            Rebuild();
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (_progress != null)
                Rebuild();
        }

        private void OnDestroy()
        {
            if (_progress != null)
            {
                _progress.OnArmyChanged -= Rebuild;
                _progress.OnOwnedChanged -= Rebuild;
            }
        }

        private void Rebuild()
        {
            ClearContainer(_armyContainer);
            ClearContainer(_backlogContainer);

            var armyStacks = GroupUnits(_progress.ArmyUnits);
            var backlogStacks = GroupUnits(_progress.BacklogUnits);

            foreach (var (unit, count) in armyStacks)
            {
                var card = Instantiate(_cardPrefab, _armyContainer);
                var capturedUnit = unit;
                card.Init(unit.Name, count, () => _progress.RemoveFromArmy(capturedUnit));
            }

            foreach (var (unit, count) in backlogStacks)
            {
                var card = Instantiate(_cardPrefab, _backlogContainer);
                var capturedUnit = unit;
                card.Init(unit.Name, count, () => _progress.AddToArmy(capturedUnit));
            }

            _slotCountLabel.text = $"{_progress.ArmyUnits.Count}/{_progress.ArmySlots}";
        }

        private static List<(CharacterData unit, int count)> GroupUnits(List<CharacterData> units)
        {
            var grouped = new List<(CharacterData unit, int count)>();
            var counts = new Dictionary<int, int>();
            var first = new Dictionary<int, CharacterData>();

            foreach (var unit in units)
            {
                if (counts.ContainsKey(unit.Id))
                {
                    counts[unit.Id]++;
                }
                else
                {
                    counts[unit.Id] = 1;
                    first[unit.Id] = unit;
                }
            }

            foreach (var kvp in first)
                grouped.Add((kvp.Value, counts[kvp.Key]));

            return grouped;
        }

        private static void ClearContainer(Transform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);
        }
    }
}
