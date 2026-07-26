using GamePush;
using UnityEngine;

namespace _Project.Scripts.Architecture.Services
{
    /// <summary>
    /// Сигналы платформы: GamePush -> Yandex LoadingAPI/GameplayAPI.
    /// GameReady = ysdk.features.LoadingAPI.ready() (требование Yandex п.1.19). Зовётся один раз,
    /// когда меню реально показано. Автовызов GamePush (ProjectData.GAMEREADY_AUTOCALL) намеренно
    /// выключен: он ждёт только сплэш Unity, т.е. срабатывает до инициализации сервисов и загрузки меню.
    /// </summary>
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
