namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;

    using TomsToolbox.Core;

    [Export]
    public class CellToBackgroundBrushConverter : IValueConverter, IMultiValueConverter
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

        /// <summary>
        /// Converts source values to a value for the binding target. The data binding engine calls this method when it propagates the values from source bindings to the binding target.
        /// </summary>
        /// <returns>
        /// A converted value.If the method returns null, the valid null value is used.A return value of <see cref="T:System.Windows.DependencyProperty"/>.<see cref="F:System.Windows.DependencyProperty.UnsetValue"/> indicates that the converter did not produce a value, and that the binding will use the <see cref="P:System.Windows.Data.BindingBase.FallbackValue"/> if it is available, or else will use the default value.A return value of <see cref="T:System.Windows.Data.Binding"/>.<see cref="F:System.Windows.Data.Binding.DoNothing"/> indicates that the binding does not transfer the value or use the <see cref="P:System.Windows.Data.BindingBase.FallbackValue"/> or the default value.
        /// </returns>
        /// <param name="values">The array of values that the source bindings in the <see cref="T:System.Windows.Data.MultiBinding"/> produces. The value <see cref="F:System.Windows.DependencyProperty.UnsetValue"/> indicates that the source binding has no value to provide for conversion.</param><param name="targetType">The type of the binding target property.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(values.OfType<DataGridCell>().FirstOrDefault(), targetType, parameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts a binding target value to the source binding values.
        /// </summary>
        /// <returns>
        /// An array of values that have been converted from the target value back to the source values.
        /// </returns>
        /// <param name="value">The value that the binding target produces.</param><param name="targetTypes">The array of types to convert to. The array length indicates the number and types of values that are suggested for the method to return.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        private Brush GetNextBrush(string text)
        {
            return _brushes[_mappingCache.Count % _brushes.Length];
        }
    }
}
