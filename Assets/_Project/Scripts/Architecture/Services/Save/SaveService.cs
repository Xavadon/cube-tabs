using Cysharp.Threading.Tasks;
using GamePush;
using UnityEngine;

namespace _Project.Scripts.Architecture.Services.Save
{
    /// <summary>
    /// Гибрид: локально PlayerPrefs (тот же браузер, работает всегда) +
    /// облако платформы через GamePush Player (кросс-девайс, требование Yandex п.1.13.3).
    /// Облако включается когда в дашборде GamePush заведено поле игрока с ключом "save" (тип String).
    /// Без поля GamePush пишет в консоль "Field save not exists" и дропает Set — тогда работает только локаль.
    /// </summary>
    public class SaveService : ISaveService
    {
        private const string CloudKey = "save";
        private const string LocalKey = "player_save";
        private const int AutoSyncIntervalSeconds = 10;
        private const float ManualSyncCooldown = 5f;

        private float _lastSyncTime = -100f;

        public async UniTask Initialize()
        {
#if !UNITY_EDITOR
            if (!GP_Init.isReady)
            {
                Debug.Log("[SaveService] Waiting for GP_Init.OnReady...");
                var tcs = new UniTaskCompletionSource();
                void OnReady()
                {
                    GP_Init.OnReady -= OnReady;
                    tcs.TrySetResult();
                }
                GP_Init.OnReady += OnReady;
                await tcs.Task;
            }

            // GamePush тянет данные игрока из облака при инициализации.
            GP_Player.EnableAutoSync(AutoSyncIntervalSeconds, SyncStorageType.cloud);
#else
            await UniTask.CompletedTask;
#endif
            Debug.Log($"[SaveService] Initialized. Cloud available: {CloudHasSave()}");
        }

        public void Save(SaveData data)
        {
            string json = JsonUtility.ToJson(data);

            // Локально — всегда, чтобы держалось в том же браузере независимо от дашборда.
            PlayerPrefs.SetString(LocalKey, json);
            PlayerPrefs.Save();

            // Облако — GamePush сам дропнет если поля "save" нет в дашборде (см. консоль).
            GP_Player.Set(CloudKey, json);
            SyncThrottled();

            Debug.Log($"[SaveService] Saved: {json}");
        }

        public SaveData Load()
        {
            // Приоритет облаку — оно кросс-девайс. Иначе локаль.
            string json = CloudHasSave()
                ? GP_Player.GetString(CloudKey)
                : PlayerPrefs.GetString(LocalKey, string.Empty);

            if (string.IsNullOrEmpty(json))
                return new SaveData();

            SaveData data;

            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (System.Exception)
            {
                Debug.LogWarning($"[SaveService] Save unparseable, starting fresh: {json}");
                return new SaveData();
            }

            if (data == null)
            {
                Debug.LogWarning("[SaveService] Save unparseable, starting fresh");
                return new SaveData();
            }

            Debug.Log($"[SaveService] Loaded: {json}");
            return data;
        }

        public bool HasSave()
        {
            return CloudHasSave() || PlayerPrefs.HasKey(LocalKey);
        }

        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(LocalKey);
            PlayerPrefs.Save();

            GP_Player.Set(CloudKey, string.Empty);
            GP_Player.Sync(SyncStorageType.cloud);

            Debug.Log("[SaveService] Save deleted (local + cloud)");
        }

        /// <summary>
        /// Есть ли валидный сейв в облаке. GP_Player.Has возвращает bool и безопасен при
        /// отсутствии поля (GetString при undefined роняет jslib — поэтому только после Has).
        /// </summary>
        private bool CloudHasSave()
        {
            if (!GP_Player.Has(CloudKey))
                return false;

            return !string.IsNullOrEmpty(GP_Player.GetString(CloudKey));
        }

        /// <summary>
        /// Пушит в облако не чаще ManualSyncCooldown — лимит платформы setData 100/5мин.
        /// </summary>
        private void SyncThrottled()
        {
            float now = Time.realtimeSinceStartup;
            if (now - _lastSyncTime < ManualSyncCooldown)
                return;

            _lastSyncTime = now;
            GP_Player.Sync(SyncStorageType.cloud);
        }
    }
}
