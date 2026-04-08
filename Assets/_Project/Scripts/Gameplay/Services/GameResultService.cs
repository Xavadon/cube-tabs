using System;
using System.Collections.Generic;
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

    public struct ClaimedMilestoneData
    {
        public int KillsRequired;
        public RewardEntry[] Rewards;
        public int BonusArmySlots;
    }

    public struct GameResultData
    {
        public GameResult Result;
        public int EnemiesKilled;
        public int GoldEarned;
        public ClaimedMilestoneData[] ClaimedMilestones;
    }

    public interface IGameResultService : IService
    {
        void StartBattle(LevelConfig levelConfig);
        void Surrender();
        event Action<GameResultData> OnGameFinished;
        event Action<int, int> OnWaveStarted;
    }

    public class GameResultService : IGameResultService
    {
        private readonly ICharacterRegistry _characterRegistry;
        private readonly IPlayerProgressService _playerProgressService;
        private readonly IGameSessionService _gameSessionService;
        private readonly ICharacterSpawner _characterSpawner;

        private int _enemiesKilled;
        private int _goldBefore;
        private bool _battleActive;
        private LevelConfig _levelConfig;
        private WaveData[] _waves;
        private int _currentWaveIndex;

        public event Action<GameResultData> OnGameFinished;
        public event Action<int, int> OnWaveStarted;

        public GameResultService(
            ICharacterRegistry characterRegistry,
            IPlayerProgressService playerProgressService,
            IGameSessionService gameSessionService,
            ICharacterSpawner characterSpawner)
        {
            _characterRegistry = characterRegistry;
            _playerProgressService = playerProgressService;
            _gameSessionService = gameSessionService;
            _characterSpawner = characterSpawner;
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
            _waves = levelConfig.Waves;
            _currentWaveIndex = 0;
            _enemiesKilled = 0;
            _goldBefore = _playerProgressService.Gold;
            _battleActive = true;

            _characterRegistry.StartBattle();
            SpawnCurrentWave();
            Debug.Log("[GameResultService] Battle started");
        }

        public void Surrender()
        {
            if (!_battleActive)
                return;

            Debug.Log("[GameResultService] Player surrendered");
            FinishBattle(GameResult.Defeat);
        }

        private void SpawnCurrentWave()
        {
            _characterSpawner.SpawnEnemyWave(_waves[_currentWaveIndex].Entries);
            OnWaveStarted?.Invoke(_currentWaveIndex, _waves.Length);
            Debug.Log($"[GameResultService] Wave {_currentWaveIndex + 1}/{_waves.Length} started");
        }

        private void HandleCharacterDied(Character.Character character)
        {
            if (!_battleActive)
                return;

            if (character.CharacterType == CharacterType.Enemy)
            {
                _enemiesKilled++;

                if (_characterRegistry.GetEnemies().Count == 0)
                {
                    _currentWaveIndex++;

                    if (_currentWaveIndex >= _waves.Length)
                        FinishBattle(GameResult.Victory);
                    else
                        SpawnCurrentWave();
                }
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
            _characterRegistry.StopBattle();

            int goldEarned = _playerProgressService.Gold - _goldBefore;
            ClaimedMilestoneData[] claimedMilestones = null;

            if (_levelConfig != null)
            {
                int levelIndex = _levelConfig.LevelIndex;
                _playerProgressService.AddLevelKills(levelIndex, _enemiesKilled);

                claimedMilestones = ClaimReachedMilestones(levelIndex);

                if (AreAllMilestonesClaimed(levelIndex))
                    _playerProgressService.MarkLevelCompleted(levelIndex);
            }

            var data = new GameResultData
            {
                Result = result,
                EnemiesKilled = _enemiesKilled,
                GoldEarned = goldEarned,
                ClaimedMilestones = claimedMilestones
            };

            Debug.Log($"[GameResultService] Battle finished: {result}, Kills: {_enemiesKilled}, Gold: {goldEarned}");
            OnGameFinished?.Invoke(data);
        }

        private ClaimedMilestoneData[] ClaimReachedMilestones(int levelIndex)
        {
            var milestones = _levelConfig.Milestones;
            if (milestones == null || milestones.Length == 0)
                return null;

            int totalKills = _playerProgressService.GetLevelKills(levelIndex);
            List<ClaimedMilestoneData> claimed = null;

            for (int i = 0; i < milestones.Length; i++)
            {
                if (totalKills < milestones[i].KillsRequired)
                    continue;

                if (_playerProgressService.IsMilestoneClaimed(levelIndex, i))
                    continue;

                _playerProgressService.ClaimMilestone(levelIndex, i);
                GrantMilestoneRewards(milestones[i]);

                claimed ??= new List<ClaimedMilestoneData>();
                claimed.Add(new ClaimedMilestoneData
                {
                    KillsRequired = milestones[i].KillsRequired,
                    Rewards = milestones[i].Rewards,
                    BonusArmySlots = milestones[i].BonusArmySlots
                });

                Debug.Log($"[GameResultService] Milestone claimed: {milestones[i].KillsRequired} kills");
            }

            return claimed?.ToArray();
        }

        private void GrantMilestoneRewards(KillMilestone milestone)
        {
            if (milestone.Rewards != null)
            {
                foreach (var entry in milestone.Rewards)
                {
                    for (int i = 0; i < entry.Count; i++)
                        _playerProgressService.GrantUnit(entry.CharacterData);
                }
            }

            if (milestone.BonusArmySlots > 0)
                _playerProgressService.GrantArmySlots(milestone.BonusArmySlots);
        }

        private bool AreAllMilestonesClaimed(int levelIndex)
        {
            var milestones = _levelConfig.Milestones;
            if (milestones == null || milestones.Length == 0)
                return true;

            for (int i = 0; i < milestones.Length; i++)
            {
                if (!_playerProgressService.IsMilestoneClaimed(levelIndex, i))
                    return false;
            }

            return true;
        }
    }
}
