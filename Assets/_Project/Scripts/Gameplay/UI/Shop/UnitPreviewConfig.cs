using UnityEngine;

namespace _Project.Scripts.Gameplay.UI.Shop
{
    [CreateAssetMenu(menuName = "Config/UnitPreviewConfig")]
    public class UnitPreviewConfig : ScriptableObject
    {
        [field: SerializeField]
        public Vector3 RoomOrigin { get; private set; } = new(1000f, -1000f, 1000f);

        [field: SerializeField]
        public float RoomSpacing { get; private set; } = 10f;

        [field: SerializeField]
        public Vector3 ModelRotation { get; private set; } = Vector3.zero;

        [field: SerializeField]
        public Vector3 CameraOffset { get; private set; } = new(0f, 1f, -3f);

        [field: SerializeField]
        public Vector3 CameraLookOffset { get; private set; } = Vector3.up;

        [field: SerializeField]
        public float CameraFov { get; private set; } = 30f;

        [field: SerializeField]
        public float CameraNear { get; private set; } = 0.1f;

        [field: SerializeField]
        public float CameraFar { get; private set; } = 20f;

        [field: SerializeField]
        public Color CameraBackgroundColor { get; private set; } = Color.clear;

        [field: SerializeField]
        public int TextureSize { get; private set; } = 256;

        [field: SerializeField]
        public Vector3 LightOffset { get; private set; } = new(-2f, 3f, -2f);

        [field: SerializeField]
        public float LightIntensity { get; private set; } = 2f;

        [field: SerializeField]
        public float LightRange { get; private set; } = 15f;

        [field: SerializeField]
        public float LightSpotAngle { get; private set; } = 60f;
    }
}
