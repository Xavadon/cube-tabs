using System;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Gameplay.Character.Services;
using _Project.Scripts.Gameplay.Services.Scene;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services
{
    public enum GameResult
    {
        Victory,
        Defeat
    }

    public struct GameResultData
    {
        public GameResult Result;
        public int EnemiesKilled;
        public int GoldEarned;
    }

    public interface IGameResultService : IService
    {
        void StartBattle(LevelConfig levelConfig);
        event Action<GameResultData> OnGameFinished;
    }

    public class GameResultService : IGameResultService
    {
        private readonly ICharacterRegistry _characterRegistry;
        private readonly IPlayerProgressService _playerProgressService;
        private readonly IGameSessionService _gameSessionService;

        private int _enemiesKilled;
        private int _goldBefore;
        private bool _battleActive;
        private LevelConfig _levelConfig;

        public event Action<GameResultData> OnGameFinished;

        public GameResultService(
            ICharacterRegistry characterRegistry,
            IPlayerProgressService playerProgressService,
            IGameSessionService gameSessionService)
        {
            _characterRegistry = characterRegistry;
            _playerProgressService = playerProgressService;
            _gameSessionService = gameSessionService;
        }

        public UniTask Initialize()
        {
            _characterRegistry.OnCharacterDied += HandleCharacterDied;
            Debug.Log("[GameResultService] Initialized");
            return UniTask.CompletedTask;
        }

        public void StartBattle(LevelConfig levelConfig)
        {
            _levelConfig = levelConfig;
            _enemiesKilled = 0;
            _goldBefore = _playerProgressService.Gold;
            _battleActive = true;

            _characterRegistry.StartBattle();
            Debug.Log("[GameResultService] Battle started");
        }

        private void HandleCharacterDied(Character.Character character)
        {
            if (!_battleActive)
                return;

            if (character.CharacterType == CharacterType.Enemy)
            {
                _enemiesKilled++;

                if (_characterRegistry.GetEnemies().Count == 0)
                    FinishBattle(GameResult.Victory);
            }
            else if (character.CharacterType == CharacterType.Ally)
            {
                if (_characterRegistry.GetAllies().Count == 0)
                    FinishBattle(GameResult.Defeat);
            }
        }

        private void FinishBattle(GameResult result)
        {
            _battleActive = false;

            int goldEarned = _playerProgressService.Gold - _goldBefore;

            if (result == GameResult.Victory && _levelConfig != null)
            {
                _playerProgressService.AddLevelKills(_levelConfig.LevelIndex, _enemiesKilled);

                int totalKills = _playerProgressService.GetLevelKills(_levelConfig.LevelIndex);
                if (totalKills >= _levelConfig.KillsToComplete)
                    _playerProgressService.MarkLevelCompleted(_levelConfig.LevelIndex);
            }

            var data = new GameResultData
            {
                Result = result,
                EnemiesKilled = _enemiesKilled,
                GoldEarned = goldEarned
            };

            Debug.Log($"[GameResultService] Battle finished: {result}, Kills: {_enemiesKilled}, Gold: {goldEarned}");
            OnGameFinished?.Invoke(data);
        }
    }
}
