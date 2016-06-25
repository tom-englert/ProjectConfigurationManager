namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;

    using DataGridExtensions;

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Core;
    using TomsToolbox.ObservableCollections;

    public static class PropertiesColumnsManager
    {
        private static readonly DependencyProperty _columnsProperty =
            DependencyProperty.RegisterAttached("_columns", typeof(IObservableCollection<object>), typeof(PropertiesColumnsManager), new FrameworkPropertyMetadata(null));


        internal static readonly DependencyProperty ProjectConfigurationProperty =
            DependencyProperty.RegisterAttached("ProjectProperty", typeof(ProjectPropertyName), typeof(PropertiesColumnsManager), new FrameworkPropertyMetadata(null));



        public static string GetPropertyName(DependencyObject obj)
        {
            Contract.Requires(obj != null);
            return (string)obj.GetValue(PropertyNameProperty);
        }
        public static void SetPropertyName(DependencyObject obj, string value)
        {
            Contract.Requires(obj != null);
            obj.SetValue(PropertyNameProperty, value);
        }
        public static readonly DependencyProperty PropertyNameProperty =
            DependencyProperty.RegisterAttached("PropertyName", typeof(string), typeof(PropertiesColumnsManager), new FrameworkPropertyMetadata(null));



        [AttachedPropertyBrowsableForType(typeof(DataGrid))]
        public static ICollection GetProperties(DependencyObject obj)
        {
            Contract.Requires(obj != null);
            return (ICollection)obj.GetValue(PropertiesProperty);
        }
        public static void SetProperties(DependencyObject obj, ICollection value)
        {
            Contract.Requires(obj != null);
            obj.SetValue(PropertiesProperty, value);
        }
        public static readonly DependencyProperty PropertiesProperty =
            DependencyProperty.RegisterAttached("Properties", typeof(ICollection), typeof(PropertiesColumnsManager), new FrameworkPropertyMetadata(null, Properties_Changed));


        private static void Properties_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dataGrid = (DataGrid)d;
            Register(dataGrid, e.NewValue as IList<object>);
        }

        private static void Register(DataGrid dataGrid, IList<object> propertieGroups)
        {
            Contract.Requires(dataGrid != null);

            if ((propertieGroups == null) || DesignerProperties.GetIsInDesignMode(dataGrid))
                return;

            if (DesignerProperties.GetIsInDesignMode(dataGrid))
                return;

            var columns = propertieGroups.ObservableSelectMany(group => ((CollectionViewGroup)group).Items);
            dataGrid.SetValue(_columnsProperty, columns); // need to hold a reference...

            dataGrid.Columns.AddRange(columns.OfType<ProjectPropertyName>().Select(CreateColumn));

            columns.CollectionChanged += (sender, e) => ProjectProperties_CollectionChanged(dataGrid, e);
        }

        private static void ProjectProperties_CollectionChanged(DataGrid dataGrid, NotifyCollectionChangedEventArgs e)
        {
            Contract.Requires(dataGrid != null);
            Contract.Requires(e != null);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var newColumns = e.NewItems?
                        .OfType<ProjectPropertyName>();

                    if (newColumns != null)
                        dataGrid.Columns.AddRange(newColumns.Select(CreateColumn));
                    break;

                case NotifyCollectionChangedAction.Remove:
                    var oldColumns = e.OldItems?
                        .OfType<ProjectPropertyName>()
                        .ToArray();

                    var columnsToRemove = dataGrid.Columns
                        .Where(col => (oldColumns?.Contains(col.GetValue(ProjectConfigurationProperty))).GetValueOrDefault())
                        .ToArray();

                    foreach (var column in columnsToRemove)
                    {
                        Contract.Assume(column != null);
                        // Hide first as a hint that this column is no longer valid => e.g. OnCopyingCellClipboardContent crashes when called for removed columns.
                        column.Visibility = Visibility.Collapsed;
                        dataGrid.Columns.Remove(column);
                    }

                    break;
            }
        }

        private static DataGridColumn CreateColumn(ProjectPropertyName projectPropertyName)
        {
            Contract.Requires(projectPropertyName != null);

            var column = new DataGridTextColumn
            {
                Header = new TextBlock { Text = projectPropertyName.DisplayName },
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                Binding = new Binding(@"PropertyValue[" + projectPropertyName.Name + @"]")
                {
                    Mode = BindingMode.TwoWay
                }
            };

            column.EnableMultilineEditing();

            column.SetValue(ProjectConfigurationProperty, projectPropertyName);
            column.SetValue(PropertyNameProperty, projectPropertyName.Name);

            return column;
        }
    }
}
