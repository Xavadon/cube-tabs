using _Project.Scripts.Gameplay.Character.Data;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character
{
    public class WeaponChanger : MonoBehaviour
    {
        [SerializeField]
        private Transform _weaponSocket;

        private GameObject _currentWeapon;

        public void SetWeapon(WeaponData weaponData)
        {
            if (_currentWeapon != null)
                Destroy(_currentWeapon); // TODO: пул когда будет Pool<T>

            if (weaponData?.Prefab == null)
                return;

            _currentWeapon = Instantiate(weaponData.Prefab, _weaponSocket);
            _currentWeapon.transform.localPosition = weaponData.LocalPosition;
            _currentWeapon.transform.localRotation = Quaternion.Euler(weaponData.LocalRotation);
            _currentWeapon.transform.localScale = weaponData.LocalScale;
        }
    }
}
