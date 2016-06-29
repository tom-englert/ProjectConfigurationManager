namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Media;

    using DataGridExtensions;

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Core;

    static class BuildConfigurationColumnsManager
    {
        private static readonly DependencyProperty _solutionConfigurationProperty =
            DependencyProperty.RegisterAttached("SolutionConfiguration", typeof(SolutionConfiguration), typeof(BuildConfigurationColumnsManager), new FrameworkPropertyMetadata(null));


        [AttachedPropertyBrowsableForType(typeof(DataGrid))]
        public static ICollection GetConfigurations(DependencyObject obj)
        {
            Contract.Requires(obj != null);
            return (ICollection)obj.GetValue(ConfigurationsProperty);
        }
        public static void SetConfigurations(DependencyObject obj, ICollection value)
        {
            Contract.Requires(obj != null);
            obj.SetValue(ConfigurationsProperty, value);
        }
        /// <summary>
        /// Identifies the <see cref="P:tomenglertde.ProjectConfigurationManager.View.BuildConfigurationColumnsManager.Configurations"/> attached property
        /// </summary>
        /// <AttachedPropertyComments>
        /// <summary>
        /// </summary>
        /// </AttachedPropertyComments>
        public static readonly DependencyProperty ConfigurationsProperty =
            DependencyProperty.RegisterAttached("Configurations", typeof(ICollection), typeof(BuildConfigurationColumnsManager), new FrameworkPropertyMetadata(null, Configurations_Changed));

        private static void Configurations_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dataGrid = (DataGrid)d;
            Register(dataGrid, (ICollection)e.NewValue);
        }

        private static void Register(DataGrid dataGrid, ICollection configurations)
        {
            Contract.Requires(dataGrid != null);

            if ((configurations == null) || DesignerProperties.GetIsInDesignMode(dataGrid))
                return;

            dataGrid.Columns.AddRange(configurations.OfType<SolutionConfiguration>().Select(CreateColumn));

            ((INotifyCollectionChanged)configurations).CollectionChanged += (sender, e) => SolutionConfigurations_CollectionChanged(dataGrid, e);
        }

        [ContractVerification(false)]
        private static void SolutionConfigurations_CollectionChanged(DataGrid dataGrid, NotifyCollectionChangedEventArgs e)
        {
            Contract.Requires(dataGrid != null);
            Contract.Requires(e != null);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var solutionConfiguration = (SolutionConfiguration)e.NewItems[0];
                    dataGrid.Columns.Add(CreateColumn(solutionConfiguration));
                    break;

                case NotifyCollectionChangedAction.Remove:
                    dataGrid.Columns.RemoveRange(col => Equals(col.GetValue(_solutionConfigurationProperty), e.OldItems[0]));
                    break;
            }
        }

        private static DataGridColumn CreateColumn(SolutionConfiguration solutionConfiguration)
        {
            Contract.Requires(solutionConfiguration != null);
            Contract.Ensures(Contract.Result<DataGridColumn>() != null);

            var path = @"ShouldBuild[" + solutionConfiguration.UniqueName + @"]";
            var binding = new Binding(path)
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Mode = BindingMode.TwoWay
            };

            var visualTree = new FrameworkElementFactory(typeof(CheckBox));
            visualTree.SetValue(ToggleButton.IsThreeStateProperty, true);
            visualTree.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            visualTree.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            visualTree.SetBinding(ToggleButton.IsCheckedProperty, binding);

            var column = new DataGridTemplateColumn
            {
                IsReadOnly = true,
                SortMemberPath = path,
                CanUserResize = false,
                Header = new TextBlock
                {
                    Text = solutionConfiguration.UniqueName,
                    LayoutTransform = new RotateTransform(-90),
                },
                CellTemplate = new DataTemplate(typeof(ProjectConfiguration))
                {
                    VisualTree = visualTree
                }
            };

            column.SetIsFilterVisible(false);
            column.SetValue(_solutionConfigurationProperty, solutionConfiguration);

            return column;
        }
    }
}
