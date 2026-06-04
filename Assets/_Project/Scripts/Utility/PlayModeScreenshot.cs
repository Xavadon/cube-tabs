#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace _Project.Scripts.Utility
{
    public class PlayModeScreenshot : MonoBehaviour
    {
        private const string FolderName = "Recordings";
        private const KeyCode CaptureKey = KeyCode.P;

        private string _folderPath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoSpawn()
        {
            var go = new GameObject("[PlayModeScreenshot]");
            go.AddComponent<PlayModeScreenshot>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            _folderPath = Path.Combine(Application.dataPath, "..", FolderName);

            if (!Directory.Exists(_folderPath))
            {
                Directory.CreateDirectory(_folderPath);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(CaptureKey))
            {
                CaptureScreenshot();
            }
        }

        private void CaptureScreenshot()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename = $"screenshot_{timestamp}.png";
            string fullPath = Path.Combine(_folderPath, filename);

            ScreenCapture.CaptureScreenshot(fullPath);
            Debug.Log($"[Screenshot] Saved: {fullPath}");
        }
    }
}
#endif
