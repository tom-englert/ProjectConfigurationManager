namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.Windows.Media;

    using JetBrains.Annotations;

    public static class BackgroundColors
    {
        [NotNull]
        private static readonly Color[] _colors =
        {
            Colors.Aquamarine,
            Colors.Aqua,
            Colors.BlanchedAlmond,
            Colors.LightBlue,
            Colors.LightGreen,
            Colors.LightPink,
            Colors.LightSalmon,
            Colors.LightSeaGreen,
            Colors.Thistle,
            Colors.Turquoise,
            Colors.Gold
        };

        public static Color GetColor(int index) => index < 0 ? Colors.Transparent : _colors[index % _colors.Length];
    }
}
