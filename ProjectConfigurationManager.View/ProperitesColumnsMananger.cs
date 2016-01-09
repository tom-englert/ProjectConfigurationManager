namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.Collections;
    using System.Collections.Specialized;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Core;

    public static class ProperitesColumnsMananger
    {
        private static readonly DependencyProperty _projectConfigurationProperty =
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

        private static void Register(DataGrid dataGrid, ICollection properties)
        {
            Contract.Requires(dataGrid != null);

            if (properties == null)
                return;

            dataGrid.Columns.AddRange(properties.OfType<ProjectPropertyName>().Select(CreateColumn));

            ((INotifyCollectionChanged)properties).CollectionChanged += (sender, e) => ProjectProperties_CollectionChanged(dataGrid, e);
        }

        private static void ProjectProperties_CollectionChanged(DataGrid dataGrid, NotifyCollectionChangedEventArgs e)
        {
            Contract.Requires(dataGrid != null);
            Contract.Requires(e != null);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    dataGrid.Columns.Add(CreateColumn((ProjectPropertyName)e.NewItems[0]));
                    break;

                case NotifyCollectionChangedAction.Remove:
                    dataGrid.Columns.RemoveRange(col => Equals(col.GetValue(_projectConfigurationProperty), e.OldItems[0]));
                    break;
            }
        }

        private static DataGridColumn CreateColumn(ProjectPropertyName projectPropertyName)
        {
            var column = new DataGridTextColumn()
            {
                Header = projectPropertyName.DisplayName,
                Binding = new Binding(@"PropertyValue[" + projectPropertyName.Name + @"]")
                {
                    Mode = BindingMode.TwoWay
                }
            };

            column.SetValue(_projectConfigurationProperty, projectPropertyName);

            return column;
        }
    }
}
