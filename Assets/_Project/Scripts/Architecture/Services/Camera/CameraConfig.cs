using UnityEngine;

namespace _Project.Scripts.Architecture.Services.Camera
{
    [CreateAssetMenu(menuName = "Config/CameraConfig")]
    public class CameraConfig : ScriptableObject
    {
        [field: Header("Auto Camera: Orientation")]
        [field: SerializeField]
        [field: Range(20f, 89f)]
        public float Pitch { get; private set; } = 48f;

        [field: SerializeField]
        public float LookHeight { get; private set; } = 1f;

        [field: SerializeField]
        public float MinYawSeparation { get; private set; } = 8f;

        [field: SerializeField]
        public float YawDeadZone { get; private set; } = 35f;

        [field: Header("Auto Camera: Framing")]
        [field: SerializeField]
        public float EngageDistance { get; private set; } = 12f;

        [field: SerializeField]
        public float LeadDistance { get; private set; } = 10f;

        [field: SerializeField]
        public float ClusterRadius { get; private set; } = 18f;

        [field: SerializeField]
        public float FramePadding { get; private set; } = 4f;

        [field: SerializeField]
        public float MinDistance { get; private set; } = 16f;

        [field: SerializeField]
        public float MaxDistance { get; private set; } = 60f;

        [field: Header("Auto Camera: Compact Screen")]
        [field: SerializeField]
        public float CompactScreenHeightDp { get; private set; } = 540f;

        [field: SerializeField]
        [field: Range(0.2f, 1f)]
        public float CompactDistanceScale { get; private set; } = 0.55f;

        [field: Header("Touch Gestures")]
        [field: SerializeField]
        public float MinUserZoom { get; private set; } = 0.5f;

        [field: SerializeField]
        public float MaxUserZoom { get; private set; } = 1.5f;

        [field: SerializeField]
        public float UserZoomSmoothTime { get; private set; } = 0.15f;

        [field: SerializeField]
        public float MaxPanOffset { get; private set; } = 20f;

        [field: SerializeField]
        public float PanReturnDelay { get; private set; } = 2.5f;

        [field: SerializeField]
        public float PanReturnSmoothTime { get; private set; } = 0.8f;

        [field: Header("Auto Camera: Smoothing")]
        [field: SerializeField]
        public float CenterSmoothTime { get; private set; } = 0.6f;

        [field: SerializeField]
        public float ZoomOutSmoothTime { get; private set; } = 0.5f;

        [field: SerializeField]
        public float ZoomInSmoothTime { get; private set; } = 1.8f;

        [field: SerializeField]
        public float YawSmoothTime { get; private set; } = 1.5f;

        [field: Header("Shake")]
        [field: SerializeField]
        public float MaxShakeOffset { get; private set; } = 0.6f;

        [field: SerializeField]
        public float ShakeFrequency { get; private set; } = 22f;

        [field: SerializeField]
        public float TraumaDecay { get; private set; } = 1.4f;

        [field: SerializeField]
        public float ShakeFalloffDistance { get; private set; } = 45f;
    }
}
