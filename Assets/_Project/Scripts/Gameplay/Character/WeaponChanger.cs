using _Project.Scripts.Gameplay.Character.Data;
using NaughtyAttributes;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character
{
    public class WeaponChanger : MonoBehaviour
    {
        [SerializeField]
        private Transform[] _weaponSockets;

        private readonly GameObject[] _currentWeapons = new GameObject[2];

        public void SetWeapon(WeaponData weaponData, int slotIndex = 0)
        {
            if (slotIndex < 0 || slotIndex >= _weaponSockets.Length)
                return;

            if (_currentWeapons[slotIndex] != null)
                Destroy(_currentWeapons[slotIndex]); // TODO: пул когда будет Pool<T>

            if (weaponData?.Prefab == null)
                return;

            var weapon = Instantiate(weaponData.Prefab, _weaponSockets[slotIndex]);
            weapon.transform.localPosition = weaponData.LocalPosition;
            weapon.transform.localRotation = Quaternion.Euler(weaponData.LocalRotation);
            weapon.transform.localScale = weaponData.LocalScale;
            _currentWeapons[slotIndex] = weapon;
        }
        
#if UNITY_EDITOR

        [SerializeField]
        private WeaponData[] _editorWeapons;

        [Button]
        public void SpawnEditorWeapons()
        {
            for (int i = 0; i < _editorWeapons.Length; i++)
            {
                SetWeapon(_editorWeapons[i], i);
            }
        }
#endif
    }
}
