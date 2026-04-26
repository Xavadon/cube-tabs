namespace _Project.Scripts.Gameplay.UI
{
    public static class PriceFormat
    {
        public const string YanIcon = " <sprite=\"yan\" name=\"yan\">";

        public static string WithIcon(string price) => price + YanIcon;
        public static string WithIcon(int price) => price + YanIcon;
    }
}
