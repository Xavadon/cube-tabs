using UnityEngine;

namespace _Project.Scripts.Gameplay.Character
{
    public class WeaponChanger : MonoBehaviour
    {
        //TODO: research
        [SerializeField]
        private GameObject[] _weapons;

        public void SetWeapon(int index)
        {
            foreach (var weapon in _weapons)
            {
                weapon.gameObject.SetActive(false);
            }
            
            _weapons[index].SetActive(true);
        }
    }
}