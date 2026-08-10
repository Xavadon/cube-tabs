using _Project.Scripts.Architecture;
using _Project.Scripts.Architecture.Services.Scene;
using Cysharp.Threading.Tasks;
using UnityEngine;
using CharacterEntity = _Project.Scripts.Gameplay.Character.Character;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    public class ScenePortal : Portal
    {
        [SerializeField] private string _targetScene;
        [SerializeField] private string _targetEntry = "default";

        protected override void Use(CharacterEntity player)
        {
            if (string.IsNullOrEmpty(_targetScene))
                return;

            Project.Get<ISceneService>().LoadArpgScene(_targetScene, _targetEntry).Forget();
        }
    }
}
