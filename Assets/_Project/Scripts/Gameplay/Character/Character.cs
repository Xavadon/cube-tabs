using MinecraftModels.Scripts;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character
{
    public class Character : MonoBehaviour
    {
        [field: SerializeField]
        public SkinChanger SkinChanger { get; private set; }
    }
}
