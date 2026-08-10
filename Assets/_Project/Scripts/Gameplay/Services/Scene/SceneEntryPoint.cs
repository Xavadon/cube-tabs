using UnityEngine;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    // куда поставить игрока после перехода: портал называет id, сцена его ищет
    public class SceneEntryPoint : MonoBehaviour
    {
        [SerializeField] private string _id = "default";

        public string Id => _id;
    }
}
