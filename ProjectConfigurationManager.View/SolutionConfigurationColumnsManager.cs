namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.Collections.Specialized;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Core;

    static class SolutionConfigurationColumnsManager
    {
        private static readonly DependencyProperty _solutionConfigurationProperty =
            DependencyProperty.RegisterAttached("_solutionConfiguration", typeof(SolutionConfiguration), typeof(SolutionConfigurationColumnsManager), new FrameworkPropertyMetadata(null));

        public static void Register(DataGrid dataGrid, Solution solution)
        {
            Contract.Requires(dataGrid != null);
            Contract.Requires(solution != null);

            dataGrid.Columns.AddRange(solution.SolutionConfigurations.Select(CreateColumn));

            solution.SolutionConfigurations.CollectionChanged += (sender, e) => SolutionConfigurations_CollectionChanged(dataGrid, e);
        }

        private static void SolutionConfigurations_CollectionChanged(DataGrid dataGrid, NotifyCollectionChangedEventArgs e)
        {
            Contract.Requires(dataGrid != null);
            Contract.Requires(e != null);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    dataGrid.Columns.Add(CreateColumn((SolutionConfiguration)e.NewItems[0]));
                    break;

                case NotifyCollectionChangedAction.Remove:
                    dataGrid.Columns.RemoveRange(col => Equals(col.GetValue(_solutionConfigurationProperty), e.OldItems[0]));
                    break;
            }
        }

        private static DataGridCheckBoxColumn CreateColumn(SolutionConfiguration solutionConfiguration)
        {
            var column = new DataGridCheckBoxColumn
            {
                IsThreeState = true,
                Header = new TextBlock
                {
                    Text = solutionConfiguration.UniqueName,
                    LayoutTransform = new RotateTransform(-90),
                },
                Binding = new Binding(@"ShouldBuild[" + solutionConfiguration.UniqueName + @"]")
                {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Mode = BindingMode.TwoWay
                }
            };

            column.SetValue(_solutionConfigurationProperty, solutionConfiguration);

            return column;
        }
    }
}
