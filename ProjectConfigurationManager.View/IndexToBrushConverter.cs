namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.ComponentModel.Composition;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    using JetBrains.Annotations;

    [Export]
    public class IndexToBrushConverter : IValueConverter
    {
        [NotNull]
        private readonly ThemeManager _themeManager;

        [ImportingConstructor]
        public IndexToBrushConverter([NotNull] ThemeManager themeManager)
        {
            _themeManager = themeManager;
        }

        public object Convert(object value, [CanBeNull] Type targetType, object parameter, [CanBeNull] CultureInfo culture)
        {
            try
            {
                return new SolidColorBrush(BackgroundColors.GetColor(System.Convert.ToInt32(value, CultureInfo.InvariantCulture), _themeManager.IsDarkTheme));
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
