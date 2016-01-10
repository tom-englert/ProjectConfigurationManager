namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Globalization;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;

    using TomsToolbox.Core;

    [Export]
    public class CellToBackgroundBrushConverter : IValueConverter
    {
        private readonly Dictionary<string, Brush> _mappingCache = new Dictionary<string, Brush>(StringComparer.OrdinalIgnoreCase);

        private static readonly Brush[] _brushes =
        {
            Brushes.Aquamarine,
            Brushes.Aqua,
            Brushes.BlanchedAlmond,
            Brushes.Gold,
            Brushes.LightBlue,
            Brushes.LightGreen,
            Brushes.LightPink,
            Brushes.LightSalmon,
            Brushes.LightSeaGreen,
            Brushes.Thistle,
            Brushes.Turquoise
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var cell = value as DataGridCell;
            if (cell == null)
                return Brushes.Transparent;

            var column = cell.Column;
            if (column.IsReadOnly)
                return Brushes.Transparent;

            var text = column.OnCopyingCellClipboardContent(cell.DataContext) as string;
            if (text == null)
                return Brushes.LightGray;

            if (string.IsNullOrEmpty(text))
                return Brushes.Transparent;

            return _mappingCache.ForceValue(text, GetNextBrush);
        }

        private Brush GetNextBrush(string text)
        {
            return _brushes[_mappingCache.Count % _brushes.Length];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
