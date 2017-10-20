namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Media;

    using DataGridExtensions;

    using JetBrains.Annotations;

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Core;

    internal static class BuildConfigurationColumnsManager
    {
        [NotNull]
        private static readonly DependencyProperty _solutionConfigurationProperty =
            DependencyProperty.RegisterAttached("SolutionConfiguration", typeof(SolutionConfiguration), typeof(BuildConfigurationColumnsManager), new FrameworkPropertyMetadata(null));


        [CanBeNull, ItemNotNull]
        [AttachedPropertyBrowsableForType(typeof(DataGrid))]
        public static ICollection GetConfigurations([NotNull] DependencyObject obj)
        {
            Contract.Requires(obj != null);
            return (ICollection)obj.GetValue(ConfigurationsProperty);
        }
        public static void SetConfigurations([NotNull] DependencyObject obj, [CanBeNull, ItemNotNull] ICollection value)
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
        [NotNull]
        public static readonly DependencyProperty ConfigurationsProperty =
            DependencyProperty.RegisterAttached("Configurations", typeof(ICollection), typeof(BuildConfigurationColumnsManager), new FrameworkPropertyMetadata(null, Configurations_Changed));

        private static void Configurations_Changed([NotNull] DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Register((DataGrid)d, (ICollection)e.NewValue);
        }

        private static void Register([NotNull] DataGrid dataGrid, [CanBeNull, ItemNotNull] ICollection configurations)
        {
            Contract.Requires(dataGrid != null);

            if ((configurations == null) || DesignerProperties.GetIsInDesignMode(dataGrid))
                return;

            dataGrid.Columns.AddRange(configurations.OfType<SolutionConfiguration>().Select(CreateColumn));

            // ReSharper disable once AssignNullToNotNullAttribute
            ((INotifyCollectionChanged)configurations).CollectionChanged += (sender, e) => SolutionConfigurations_CollectionChanged(dataGrid, e);
        }

        [ContractVerification(false)]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        private static void SolutionConfigurations_CollectionChanged([NotNull] DataGrid dataGrid, [NotNull] NotifyCollectionChangedEventArgs e)
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

        [NotNull]
        private static DataGridColumn CreateColumn([NotNull] SolutionConfiguration solutionConfiguration)
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
            // ReSharper disable AssignNullToNotNullAttribute
            visualTree.SetValue(ToggleButton.IsThreeStateProperty, true);
            visualTree.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            visualTree.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            visualTree.SetBinding(ToggleButton.IsCheckedProperty, binding);
            // ReSharper restore AssignNullToNotNullAttribute

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
