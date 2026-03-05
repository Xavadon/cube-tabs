using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Architecture.Services.Save
{
    public class SaveService : ISaveService
    {
        private const string SaveKey = "player_save";

        public UniTask Initialize()
        {
            Debug.Log("[SaveService] Initialized");
            return UniTask.CompletedTask;
        }

        public void Save(SaveData data)
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
            Debug.Log($"[SaveService] Saved: {json}");
        }

        public SaveData Load()
        {
            if (!HasSave())
                return new SaveData();

            string json = PlayerPrefs.GetString(SaveKey);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log($"[SaveService] Loaded: {json}");
            return data;
        }

        public bool HasSave()
        {
            return PlayerPrefs.HasKey(SaveKey);
        }

        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            Debug.Log("[SaveService] Save deleted");
        }
    }
}
