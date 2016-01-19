namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;

    using DataGridExtensions;

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Core;

    public static class ProperitesColumnsMananger
    {
        internal static readonly DependencyProperty ProjectConfigurationProperty =
            DependencyProperty.RegisterAttached("ProjectProperty", typeof(ProjectPropertyName), typeof(ProperitesColumnsMananger), new FrameworkPropertyMetadata(null));


        [AttachedPropertyBrowsableForType(typeof(DataGrid))]
        public static ICollection GetProperites(DependencyObject obj)
        {
            return (ICollection)obj.GetValue(ProperitesProperty);
        }
        public static void SetProperites(DependencyObject obj, ICollection value)
        {
            obj.SetValue(ProperitesProperty, value);
        }
        /// <summary>
        /// Identifies the <see cref="P:tomenglertde.ProjectConfigurationManager.View.ProperitesColumnsMananger.Properites"/> attached property
        /// </summary>
        /// <AttachedPropertyComments>
        /// <summary>
        /// </summary>
        /// </AttachedPropertyComments>
        public static readonly DependencyProperty ProperitesProperty =
            DependencyProperty.RegisterAttached("Properites", typeof(ICollection), typeof(ProperitesColumnsMananger), new FrameworkPropertyMetadata(null, Properties_Changed));

        private static void Properties_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dataGrid = (DataGrid)d;
            Register(dataGrid, (ICollection)e.NewValue);
        }

        private static void Register(DataGrid dataGrid, ICollection propertieGroups)
        {
            Contract.Requires(dataGrid != null);

            if (propertieGroups == null)
                return;

            dataGrid.Columns.AddRange(propertieGroups.OfType<CollectionViewGroup>().SelectMany(group => group.Items.Cast<ProjectPropertyName>()).Select(CreateColumn));

            ((INotifyCollectionChanged)propertieGroups).CollectionChanged += (sender, e) => ProjectProperties_CollectionChanged(dataGrid, e);
        }

        private static void ProjectProperties_CollectionChanged(DataGrid dataGrid, NotifyCollectionChangedEventArgs e)
        {
            Contract.Requires(dataGrid != null);
            Contract.Requires(e != null);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var newColumns = e.NewItems?
                        .OfType<CollectionViewGroup>()
                        .SelectMany(group => group.Items.Cast<ProjectPropertyName>());

                    if (newColumns != null)
                        dataGrid.Columns.AddRange(newColumns.Select(CreateColumn));
                    break;

                case NotifyCollectionChangedAction.Remove:
                    var oldColumns = e.OldItems?
                        .OfType<CollectionViewGroup>()
                        .SelectMany(group => group.Items.Cast<ProjectPropertyName>())
                        .ToArray();

                    var columnsToRemove = dataGrid.Columns
                        .Where(col => (oldColumns?.Contains(col.GetValue(ProjectConfigurationProperty))).GetValueOrDefault())
                        .ToArray();

                    foreach (var column in columnsToRemove)
                    {
                        column.Visibility = Visibility.Collapsed;
                        dataGrid.Columns.Remove(column);
                    }

                    break;
            }
        }

        private static DataGridColumn CreateColumn(ProjectPropertyName projectPropertyName)
        {
            var column = new DataGridTextColumn
            {
                Header = projectPropertyName.DisplayName,
                Binding = new Binding(@"PropertyValue[" + projectPropertyName.Name + @"]")
                {
                    Mode = BindingMode.TwoWay
                }
            };

            column.EnableMultilineEditing();

            column.SetValue(ProjectConfigurationProperty, projectPropertyName);

            return column;
        }
    }
}
