using _Project.Scripts.Architecture.Services;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    public interface ISpawnPointProvider : IService
    {
        Vector3 GetPlayerSpawnPosition();
        Quaternion GetPlayerSpawnRotation();
    }
}
