namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;

    using JetBrains.Annotations;

    using TomsToolbox.Core;

    [Export]
    public sealed class CellToBackgroundBrushConverter : IValueConverter, IMultiValueConverter
    {
        [NotNull]
        private readonly ThemeManager _themeManager;
        [NotNull]
        private readonly Dictionary<string, Color> _mappingCache = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);

        public CellToBackgroundBrushConverter([NotNull] ThemeManager themeManager)
        {
            _themeManager = themeManager;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var cell = value as DataGridCell;

            var column = cell?.Column;
            if (column == null || column.IsReadOnly)
                return Brushes.Transparent;

            var dataContext = cell.DataContext;
            if (dataContext == null)
                return Brushes.Transparent;

            var text = GetColumnText(column, dataContext);
            if (text == null)
                return _themeManager.IsDarkTheme ? Brushes.DimGray : Brushes.LightGray;

            if (string.IsNullOrEmpty(text))
                return Brushes.Transparent;

            var color = _mappingCache.ForceValue(text, GetNextColor);

            return new SolidColorBrush(color);
        }

        [CanBeNull]
        private static string GetColumnText([NotNull] DataGridColumn column, [NotNull] object dataContext)
        {
            Contract.Requires(column != null);
            Contract.Requires(dataContext != null);

            try
            {
                if (column.Visibility != Visibility.Visible)
                    return null;

                return column.OnCopyingCellClipboardContent(dataContext) as string;
            }
            catch
            {
                return null;
            }
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
            return Convert(values?.OfType<DataGridCell>().FirstOrDefault(), targetType, parameter, culture);
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

        [ContractVerification(false)]
        private Color GetNextColor([CanBeNull] string text)
        {
            return BackgroundColors.GetColor(_mappingCache.Count, _themeManager.IsDarkTheme);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_mappingCache != null);
        }
    }
}
