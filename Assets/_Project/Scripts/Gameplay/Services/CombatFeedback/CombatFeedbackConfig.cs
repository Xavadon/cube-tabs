using System;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services.CombatFeedback
{
    [CreateAssetMenu(menuName = "Config/CombatFeedbackConfig")]
    public class CombatFeedbackConfig : ScriptableObject
    {
        [field: Header("Damage Numbers")]
        [field: SerializeField]
        public FloatingTextStyle EnemyDamageStyle { get; private set; } =
            new("{0:0}", new Color(1f, 0.96f, 0.85f), 30f, 0.7f, 55f, new Vector3(0f, 2.4f, 0f));

        [field: SerializeField]
        public FloatingTextStyle AllyDamageStyle { get; private set; } =
            new("{0:0}", new Color(1f, 0.45f, 0.4f), 24f, 0.55f, 40f, new Vector3(0f, 2.4f, 0f));

        [field: SerializeField]
        public bool ShowAllyDamage { get; private set; }

        [field: SerializeField]
        public float MinDamageToShow { get; private set; } = 1f;

        [field: SerializeField]
        [field: Range(0f, 1f)]
        public float BigHitHealthRatio { get; private set; } = 0.25f;

        [field: SerializeField]
        public float BigHitScale { get; private set; } = 1.45f;

        [field: SerializeField]
        public float HorizontalJitterPixels { get; private set; } = 28f;

        [field: Header("Gold")]
        [field: SerializeField]
        public FloatingTextStyle GoldStyle { get; private set; } =
            new("+{0:0}", new Color(1f, 0.82f, 0.2f), 34f, 1f, 90f, new Vector3(0f, 1.6f, 0f));

        [field: Header("Camera Shake")]
        [field: SerializeField]
        public float DeathShakeMinMaxHealth { get; private set; } = 700f;

        [field: SerializeField]
        [field: Range(0f, 1f)]
        public float DeathShakeTrauma { get; private set; } = 0.45f;

        [field: SerializeField]
        public float AbilityShakeCooldown { get; private set; } = 0.35f;
    }

    [Serializable]
    public class FloatingTextStyle
    {
        [field: SerializeField]
        public string Format { get; private set; } = "{0:0}";

        [field: SerializeField]
        public Color Color { get; private set; } = Color.white;

        [field: SerializeField]
        public float FontSize { get; private set; } = 30f;

        [field: SerializeField]
        public float Lifetime { get; private set; } = 0.7f;

        [field: SerializeField]
        public float RisePixels { get; private set; } = 55f;

        [field: SerializeField]
        public Vector3 WorldOffset { get; private set; } = new(0f, 2.4f, 0f);

        public FloatingTextStyle()
        {
        }

        public FloatingTextStyle(string format, Color color, float fontSize, float lifetime, float risePixels, Vector3 worldOffset)
        {
            Format = format;
            Color = color;
            FontSize = fontSize;
            Lifetime = lifetime;
            RisePixels = risePixels;
            WorldOffset = worldOffset;
        }
    }
}
