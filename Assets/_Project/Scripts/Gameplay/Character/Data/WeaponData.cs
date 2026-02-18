using _Project.Scripts.Gameplay.Character.Components.Health;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data
{
    [CreateAssetMenu(menuName = "Config/WeaponData")]
    public class WeaponData : ScriptableObject
    {
        [field: SerializeField]
        public float Damage { get; private set; } = 20f;

        [field: SerializeField]
        public DamageType DamageType { get; private set; } = DamageType.Physical;
    }
}