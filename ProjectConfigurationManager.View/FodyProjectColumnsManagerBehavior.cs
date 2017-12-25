namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
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
        private IValueConverter _indexToBrushConverter;

        protected override void OnAttached()
        {
            base.OnAttached();

            var dataGrid = AssociatedObject;
            Contract.Assume(dataGrid != null);

            dataGrid.KeyDown += DataGrid_KeyDown;
            dataGrid.TextInput += DataGrid_TextInput;

            _indexToBrushConverter = dataGrid.GetExportProvider().GetExportedValue<IndexToBrushConverter>();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            var dataGrid = AssociatedObject;
            Contract.Assume(dataGrid != null);

            dataGrid.KeyDown -= DataGrid_KeyDown;
            dataGrid.TextInput -= DataGrid_TextInput;

            var solution = _solution;

            if (solution == null)
                return;

            solution.Changed += Solution_Changed;
            solution.FileChanged += Solution_FileChanged;

        }

        protected override void OnAssociatedObjectLoaded()
        {
            base.OnAssociatedObjectLoaded();

            if (_solution == null)
            {
                var solution = AssociatedObject?.GetExportProvider().GetExportedValue<Solution>();

                _solution = solution;

                if (solution == null)
                    return;

                solution.Changed += Solution_Changed;
                solution.FileChanged += Solution_FileChanged;
            }

            UpdateColumns();
        }

        private void Solution_Changed([NotNull] object sender, [NotNull] EventArgs e)
        {
            UpdateColumns();
        }

        private void Solution_FileChanged([NotNull] object sender, [NotNull] FileSystemEventArgs e)
        {
            var changedFile = Path.GetFileName(e.Name);

            if (changedFile?.StartsWith(FodyWeaver.ConfigurationFileName, StringComparison.OrdinalIgnoreCase) != true)
                return;

            UpdateColumns();
        }

        private void DataGrid_KeyDown([NotNull] object sender, [NotNull] KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.OemMinus:
                case Key.Subtract:
                    SetWeaverConfiguration((DataGrid)sender, 0);
                    break;
            }
        }

        private void DataGrid_TextInput([NotNull] object sender, [NotNull] TextCompositionEventArgs e)
        {
            if (!int.TryParse(e.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var number))
                return;

            SetWeaverConfiguration((DataGrid)sender, number);
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private void SetWeaverConfiguration([NotNull] DataGrid dataGrid, int number)
        {
            foreach (var cell in dataGrid.SelectedCells)
            {
                var row = (FodyConfigurationMapping)cell.Item;
                var weaver = GetWeaver(cell.Column);
                if (weaver == null)
                    continue;

                row.Configuration[weaver] = number;
            }
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
                Contract.Assume(weaver != null);
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
            visualTree.SetBinding(Border.BackgroundProperty, new Binding(configurationBindingPath) { Converter = _indexToBrushConverter });

            var content = new FrameworkElementFactory(typeof(ContentControl));
            content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            content.SetValue(FrameworkElement.StyleProperty, contentStyle);
            content.SetResourceReference(TextElement.ForegroundProperty, SystemColors.WindowTextBrushKey);
            visualTree.AppendChild(content);
            // ReSharper restore AssignNullToNotNullAttribute

            var header = new TextBlock
            {
                Text = weaver,
                LayoutTransform = new RotateTransform(-90),
                Margin = new Thickness(2)
            };

            TextOptions.SetTextRenderingMode(header, TextRenderingMode.Grayscale);

            var column = new DataGridTemplateColumn
            {
                Header = header,
                CanUserResize = false,
                SortMemberPath = configurationBindingPath,
                CellTemplate = new DataTemplate(typeof(int))
                {
                    VisualTree = visualTree
                }
            };

            column.SetIsFilterVisible(false);

            return column;
        }

        [CanBeNull]
        private string GetWeaver([CanBeNull] DataGridColumn column)
        {
            return (column?.Header as TextBlock)?.Text;
        }
    }
}
