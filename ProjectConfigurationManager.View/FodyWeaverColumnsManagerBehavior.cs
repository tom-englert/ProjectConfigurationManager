namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Globalization;
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

            var projectWeavers = FodyWeaver.EnumerateWeavers(solution)
                .Where(weaver => weaver.Project != null)
                .GroupBy(weaver => weaver.WeaverName)
                .ToArray();

            var numberOfConfigurations = 1 + projectWeavers
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
        private static DataGridColumn CreateColumn(int index)
        {
            var configurationBinding = new Binding("Configuration[" + index + "]");
            var transparentBackgoundSetter = new Setter(Control.BackgroundProperty, Brushes.Transparent);

            var cellStyle = new Style(typeof(DataGridCell))
            {
                Setters =
                {
                    new Setter(Control.BackgroundProperty, new SolidColorBrush(BackgroundColors.GetColor(index)))
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
                CellStyle = cellStyle
            };
        }
    }
}
