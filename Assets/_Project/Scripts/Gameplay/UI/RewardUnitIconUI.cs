using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI
{
    public class RewardUnitIconUI : MonoBehaviour
    {
        [SerializeField]
        private RawImage _previewImage;

        [SerializeField]
        private TextMeshProUGUI _countLabel;

        public void Setup(RenderTexture portrait, int count)
        {
            if (_previewImage != null && portrait != null)
                _previewImage.texture = portrait;

            if (_countLabel != null)
                _countLabel.text = count > 1 ? $"x{count}" : "";
        }
    }
}
