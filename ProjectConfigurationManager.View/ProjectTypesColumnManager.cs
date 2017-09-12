namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.Collections.Generic;
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
    using TomsToolbox.Desktop;

    public static class ProjectTypesColumnManager
    {
        public static bool GetIsAttached([NotNull] DependencyObject obj)
        {
            Contract.Requires(obj != null);
            return obj.GetValue<bool>(IsAttachedProperty);
        }
        public static void SetIsAttached([NotNull] DependencyObject obj, bool value)
        {
            Contract.Requires(obj != null);
            obj.SetValue(IsAttachedProperty, value);
        }
        [NotNull]
        public static readonly DependencyProperty IsAttachedProperty =
            DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(ProjectTypesColumnManager), new FrameworkPropertyMetadata(false, IsAttached_Changed));

        private static void IsAttached_Changed([NotNull] DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dataGrid = (DataGrid)d;

            ProjectTypeGuid.WellKnown
                .Where(item => item.Key != ProjectTypeGuid.Unspecified)
                .OrderBy(item => item.Value)
                .ForEach(item => dataGrid.Columns.Add(CreateColumn(item)));
        }

        [NotNull]
        private static DataGridColumn CreateColumn(KeyValuePair<string, string> item)
        {
            Contract.Ensures(Contract.Result<DataGridColumn>() != null);

            var path = @"IsProjectTypeGuidSelected[" + item.Key + @"]";
            var binding = new Binding(path)
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Mode = BindingMode.TwoWay
            };

            var visualTree = new FrameworkElementFactory(typeof(CheckBox));
            // ReSharper disable AssignNullToNotNullAttribute
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
                    Text = item.Value,
                    LayoutTransform = new RotateTransform(-90),
                },
                CellTemplate = new DataTemplate(typeof(ProjectConfiguration))
                {
                    VisualTree = visualTree
                }
            };

            column.SetIsFilterVisible(false);

            return column;
        }

    }
}
