namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;

    using JetBrains.Annotations;

    using tomenglertde.ProjectConfigurationManager.Model;

    using Throttle;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Interactivity;

    public class FodyWeaverColumnsManagerBehavior : FrameworkElementBehavior<DataGrid>
    {
        private Solution _solution;
        private IValueConverter _indexToBrushConverter;

        protected override void OnAttached()
        {
            base.OnAttached();

            var dataGrid = AssociatedObject;
            Contract.Assume(dataGrid != null);

            _indexToBrushConverter = dataGrid.GetExportProvider().GetExportedValue<IndexToBrushConverter>();
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

        [Throttled(typeof(DispatcherThrottle))]
        private void UpdateColumns()
        {
            var solution = _solution;
            if (solution == null)
                return;

            var dataGrid = AssociatedObject;
            if (dataGrid == null)
                return;

            var projectWeavers = FodyWeaver.EnumerateWeavers(solution)
                .Where(weaver => weaver.Project != null)
                .GroupBy(weaver => weaver.WeaverName)
                .ToArray();

            var numberOfConfigurations = 1 + projectWeavers
                // ReSharper disable once AssignNullToNotNullAttribute
                // ReSharper disable once PossibleNullReferenceException
                .Select(group => group.Select(item => item.Configuration)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count())
                .DefaultIfEmpty()
                .Max();

            while ((dataGrid.Columns.Count - dataGrid.FrozenColumnCount) > numberOfConfigurations)
            {
                dataGrid.Columns.RemoveAt(numberOfConfigurations + dataGrid.FrozenColumnCount);
            }

            while ((dataGrid.Columns.Count - dataGrid.FrozenColumnCount) < numberOfConfigurations)
            {
                dataGrid.Columns.Add(CreateColumn(dataGrid.Columns.Count - dataGrid.FrozenColumnCount));
            }
        }

        [NotNull]
        private DataGridColumn CreateColumn(int index)
        {
            var configurationBinding = new Binding("Configuration[" + index + "]");
            // ReSharper disable once AssignNullToNotNullAttribute
            var transparentBackgoundSetter = new Setter(Control.BackgroundProperty, Brushes.Transparent);

            var cellStyle = new Style(typeof(DataGridCell))
            {
                Setters =
                {
                    new Setter(Control.BackgroundProperty, new Binding { Source = index, Converter = _indexToBrushConverter }),
                    // ReSharper disable once AssignNullToNotNullAttribute
                    new Setter(Control.ForegroundProperty, new DynamicResourceExtension(SystemColors.WindowTextBrushKey))
                },
                Triggers =
                {
                    new DataTrigger
                    {
                        Binding = configurationBinding,
                        Value = string.Empty,
                        Setters = { transparentBackgoundSetter }
                    },
                    new DataTrigger
                    {
                        Binding = configurationBinding,
                        Value = null,
                        Setters = { transparentBackgoundSetter }
                    }
                }
            };

            return new DataGridTextColumn
            {
                Header = index == 0 ? "Solution" : index.ToString(CultureInfo.CurrentCulture),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                Binding = configurationBinding,
                CellStyle = cellStyle,
                IsReadOnly = true,
            };
        }
    }
}
