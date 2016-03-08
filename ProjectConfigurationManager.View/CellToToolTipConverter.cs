using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using tomenglertde.ProjectConfigurationManager.Model;

namespace tomenglertde.ProjectConfigurationManager.View
{
    class CellToToolTipConverter : IValueConverter, IMultiValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var propertyText = "";
            var configText = "";

            var cell = value as DataGridCell;

            var projectConfiguration = cell.DataContext as ProjectConfiguration;
            if (projectConfiguration != null)
            {
                configText = string.Format("{0} {1} {2}", projectConfiguration.Project.Name, projectConfiguration.Configuration, projectConfiguration.Platform);
            }

            var column = cell?.Column;
            if (column != null)
            {
                var propertyName = (ProjectPropertyName)cell.Column.GetValue(ProperitesColumnsMananger.ProjectConfigurationProperty);
                if (propertyName != null)
                {
                    propertyText = propertyName.DisplayName;
                }
            }

            return configText + Environment.NewLine + propertyText;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(values?.OfType<DataGridCell>().FirstOrDefault(), targetType, parameter, culture);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
