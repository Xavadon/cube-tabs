using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data
{
    [CreateAssetMenu(menuName = "Config/WeaponData")]
    public class WeaponData : ScriptableObject
    {
        [field: SerializeField]
        public GameObject Prefab { get; private set; }

        [field: SerializeField]
        public Vector3 LocalPosition { get; private set; }

        [field: SerializeField]
        public Vector3 LocalRotation { get; private set; }

        [field: SerializeField]
        public Vector3 LocalScale { get; private set; } = Vector3.one;
    }
}
