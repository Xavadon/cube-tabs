using GamePush;
using UnityEngine;

namespace _Project.Scripts.Architecture.Services
{
    public static class PlatformSignals
    {
        private static bool _gameReadySent;
        private static bool _gameplayActive;

        public static void GameReady()
        {
            if (_gameReadySent)
                return;

            _gameReadySent = true;

#if !UNITY_EDITOR && UNITY_WEBGL
            GP_Game.GameReady();
#endif
            Debug.Log("[PlatformSignals] GameReady");
        }

        public static void GameplayStart()
        {
            if (_gameplayActive)
                return;

            _gameplayActive = true;

#if !UNITY_EDITOR && UNITY_WEBGL
            GP_Game.GameplayStart();
#endif
            Debug.Log("[PlatformSignals] GameplayStart");
        }

        public static void GameplayStop()
        {
            if (!_gameplayActive)
                return;

            _gameplayActive = false;

#if !UNITY_EDITOR && UNITY_WEBGL
            GP_Game.GameplayStop();
#endif
            Debug.Log("[PlatformSignals] GameplayStop");
        }
    }
}
