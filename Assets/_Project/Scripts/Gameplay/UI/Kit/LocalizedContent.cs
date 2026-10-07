using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Services.Scene;

namespace _Project.Scripts.Gameplay.UI.Kit
{
    public static class LocalizedContent
    {
        public static string GetName(this ILocalizationService localization, CharacterData data)
        {
            return GetText(localization, data.NameKey, data.Name);
        }

        public static string GetUnitName(this ILocalizationService localization, CharacterData data, int tierIndex)
        {
            string name = localization.GetName(data);

            if (data.MaxTier > 0)
            {
                return localization.Get(LocalizationKeys.Evolution.Tier, name, tierIndex + 1);
            }

            return name;
        }

        public static string GetName(this ILocalizationService localization, LevelConfig data)
        {
            return GetText(localization, data.NameKey, data.LevelName);
        }

        public static string GetName(this ILocalizationService localization, ShopItemData data)
        {
            return GetText(localization, data.NameKey, data.Name);
        }

        private static string GetText(ILocalizationService localization, string key, string fallback)
        {
            if (string.IsNullOrEmpty(key))
            {
                return fallback;
            }

            return localization.Get(key);
        }
    }
}
