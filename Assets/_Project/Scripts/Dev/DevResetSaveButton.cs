using _Project.Scripts.Architecture;
using _Project.Scripts.Architecture.Services.Save;
using UnityEngine;

namespace _Project.Scripts.Dev
{
    public class DevResetSaveButton : MonoBehaviour
    {
        private const float ButtonWidth = 200f;
        private const float ButtonHeight = 50f;
        private const float Margin = 10f;

        private string _status;

        private void OnGUI()
        {
            var buttonRect = new Rect(Margin, Margin, ButtonWidth, ButtonHeight);

            if (GUI.Button(buttonRect, "RESET SAVE"))
                ResetSave();

            if (!string.IsNullOrEmpty(_status))
                GUI.Label(new Rect(Margin, Margin + ButtonHeight + 4f, 420f, 24f), _status);
        }

        private void ResetSave()
        {
            var saveService = Project.Get<ISaveService>();

            if (saveService == null)
            {
                _status = "SaveService not resolved";
                return;
            }

            saveService.DeleteSave();
            _status = "Save deleted. Reload the page (F5) to start clean.";
        }
    }
}
