namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Media;

    using DataGridExtensions;

    using JetBrains.Annotations;

    using tomenglertde.ProjectConfigurationManager.Model;

    using Throttle;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Interactivity;

    public sealed class FodyProjectColumnsManagerBehavior : FrameworkElementBehavior<DataGrid>
    {
        private Solution _solution;

        protected override void OnAssociatedObjectLoaded()
        {
            base.OnAssociatedObjectLoaded();

            if (_solution == null)
            {
                var solution = AssociatedObject?.GetExportProvider()?.GetExportedValue<Solution>();

                _solution = solution;

                if (solution == null)
                    return;

                solution.Changed += Solution_Changed;
            }

            UpdateColumns();
        }

        private void Solution_Changed([NotNull] object sender, [NotNull] EventArgs e)
        {
            UpdateColumns();
        }

        [Throttled(typeof(DispatcherThrottle))]
        private void UpdateColumns()
        {
            var solution = _solution;
            if (solution == null)
                return;

            var dataGrid = AssociatedObject;
            if (dataGrid == null)
                return;

            var weavers = new HashSet<string>(FodyWeaver.EnumerateWeavers(solution).Select(item => item.WeaverName));

            var columns = dataGrid.Columns.Skip(dataGrid.FrozenColumnCount).ToArray();

            var toRemove = columns.Where(col => !weavers.Contains(GetWeaver(col))).ToArray();
            var existing = new HashSet<string>(columns.Except(toRemove).Select(GetWeaver));
            var toAdd = weavers.Except(existing);

            foreach (var column in toRemove)
            {
                dataGrid.Columns.Remove(column);
            }

            foreach (var weaver in toAdd)
            {
                dataGrid.Columns.Add(CreateColumn(weaver));
            }
        }

        [NotNull]
        private DataGridColumn CreateColumn([NotNull] string weaver)
        {
            var configurationBindingPath = "Configuration[" + weaver + "]";
            var configurationBinding = new Binding(configurationBindingPath);

            var contentStyle = new Style(typeof(ContentControl))
            {
                Setters = { new Setter { Property = ContentControl.ContentProperty, Value = configurationBinding } },
                Triggers =
                {
                    new DataTrigger { Binding = configurationBinding, Value = -1, Setters = { new Setter { Property = ContentControl.ContentProperty, Value = null }}},
                    new DataTrigger { Binding = configurationBinding, Value = 0, Setters = { new Setter { Property = ContentControl.ContentProperty, Value = "S" }}},
                }
            };

            // ReSharper disable AssignNullToNotNullAttribute
            var visualTree = new FrameworkElementFactory(typeof(Border));
            visualTree.SetBinding(Border.BackgroundProperty, new Binding(configurationBindingPath) { Converter = new IndexToBrushConverter() });

            var content = new FrameworkElementFactory(typeof(ContentControl));
            content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            content.SetValue(FrameworkElement.StyleProperty, contentStyle);
            content.SetResourceReference(TextElement.ForegroundProperty, SystemColors.WindowTextBrushKey);
            visualTree.AppendChild(content);
            // ReSharper restore AssignNullToNotNullAttribute

            var column = new DataGridTemplateColumn
            {
                Header = new TextBlock
                {
                    Text = weaver,
                    LayoutTransform = new RotateTransform(-90),
                },
                CanUserResize = false,
                CellTemplate = new DataTemplate(typeof(int))
                {
                    VisualTree = visualTree
                }
                // Binding = new Binding("Configuration[" + weaver + "]")
            };

            column.SetIsFilterVisible(false);

            return column;
        }

        private class IndexToBrushConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                try
                {
                    return new SolidColorBrush(BackgroundColors.GetColor(System.Convert.ToInt32(value)));
                }
                catch
                {
                    return null;
                }
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        [CanBeNull]
        private string GetWeaver([CanBeNull] DataGridColumn column)
        {
            return (column?.Header as TextBlock)?.Text;
        }
    }
}
