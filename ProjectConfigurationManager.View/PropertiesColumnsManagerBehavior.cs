namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Threading;

    using DataGridExtensions;
    using DataGridExtensions.Framework;

    using JetBrains.Annotations;

    using tomenglertde.ProjectConfigurationManager.Model;
    using tomenglertde.ProjectConfigurationManager.View.Themes;

    using TomsToolbox.Core;
    using TomsToolbox.ObservableCollections;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Interactivity;

    public class PropertiesColumnsManagerBehavior : FrameworkElementBehavior<DataGrid>
    {
        [NotNull]
        private readonly DispatcherThrottle _displayIndexChangedThrottle;
        [NotNull]
        private readonly DispatcherThrottle _columnsCreatedThrottle;

        private IObservableCollection<object> _projectPropertyNames;
        private Configuration _configuration;
        private ITracer _tracer;

        internal static string GetProjectPropertyName([NotNull] DependencyObject obj)
        {
            Contract.Requires(obj != null);
            return (string)obj.GetValue(ProjectPropertyNameProperty);
        }
        internal static void SetProjectPropertyName([NotNull] DependencyObject obj, string value)
        {
            Contract.Requires(obj != null);
            obj.SetValue(ProjectPropertyNameProperty, value);
        }
        internal static readonly DependencyProperty ProjectPropertyNameProperty =
            DependencyProperty.RegisterAttached("ProjectPropertyName", typeof(ProjectPropertyName), typeof(PropertiesColumnsManagerBehavior), new FrameworkPropertyMetadata(null));


        public static string GetPropertyName([NotNull] DependencyObject obj)
        {
            Contract.Requires(obj != null);
            return (string)obj.GetValue(PropertyNameProperty);
        }
        public static void SetPropertyName([NotNull] DependencyObject obj, string value)
        {
            Contract.Requires(obj != null);
            obj.SetValue(PropertyNameProperty, value);
        }
        public static readonly DependencyProperty PropertyNameProperty =
            DependencyProperty.RegisterAttached("PropertyName", typeof(string), typeof(PropertiesColumnsManagerBehavior), new FrameworkPropertyMetadata(null));


        public ICollection Properties
        {
            get { return (ICollection)GetValue(PropertiesProperty); }
            set { SetValue(PropertiesProperty, value); }
        }
        public static readonly DependencyProperty PropertiesProperty =
            DependencyProperty.Register("Properties", typeof(ICollection), typeof(PropertiesColumnsManagerBehavior), new FrameworkPropertyMetadata(null, (sender, e) => ((PropertiesColumnsManagerBehavior)sender).Properties_Changed(e.NewValue as IList<object>)));

        public PropertiesColumnsManagerBehavior()
        {
            _columnsCreatedThrottle = new DispatcherThrottle(DispatcherPriority.Background, ColumnsCreated);
            _displayIndexChangedThrottle = new DispatcherThrottle(DispatcherPriority.Background, ColumnsDisplayIndexChanged);
        }


        protected override void OnAttached()
        {
            base.OnAttached();

            var exportProvider = AssociatedObject?.GetExportProvider();

            _configuration = exportProvider?.GetExportedValue<Configuration>();
            _tracer = exportProvider?.GetExportedValue<ITracer>();

            Initialize();
        }

        private void Properties_Changed(IList<object> propertyGroups)
        {
            if (propertyGroups == null)
                return;

            _projectPropertyNames = propertyGroups.ObservableSelectMany(group => ((CollectionViewGroup)group).Items);
            _projectPropertyNames.CollectionChanged += (sender, e) => ProjectProperties_CollectionChanged(e);

            Initialize();
        }

        private void Initialize()
        {
            if (_projectPropertyNames == null)
                return;

            var dataGrid = AssociatedObject;
            if (dataGrid == null)
                return;

            dataGrid.Columns.AddRange(_projectPropertyNames.OfType<ProjectPropertyName>().Select(CreateColumn));
            dataGrid.ColumnDisplayIndexChanged += (_, __) => _displayIndexChangedThrottle.Tick();
        }

        private void ProjectProperties_CollectionChanged([NotNull] NotifyCollectionChangedEventArgs e)
        {
            Contract.Requires(e != null);

            var dataGrid = AssociatedObject;
            if (dataGrid == null)
                return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var newColumns = e.NewItems?
                        .OfType<ProjectPropertyName>();

                    if (newColumns != null)
                        dataGrid.Columns.AddRange(newColumns.Select(CreateColumn));
                    break;

                case NotifyCollectionChangedAction.Remove:
                    var oldColumns = e.OldItems?
                        .OfType<ProjectPropertyName>()
                        .ToArray();

                    var columnsToRemove = dataGrid.Columns
                        .Where(col => (oldColumns?.Contains(col.GetValue(ProjectPropertyNameProperty))).GetValueOrDefault())
                        .ToArray();

                    foreach (var column in columnsToRemove)
                    {
                        Contract.Assume(column != null);
                        // Hide first as a hint that this column is no longer valid => e.g. OnCopyingCellClipboardContent crashes when called for removed columns.
                        column.Visibility = Visibility.Collapsed;
                        dataGrid.Columns.Remove(column);
                    }

                    break;
            }
        }

        private DataGridColumn CreateColumn([NotNull] ProjectPropertyName projectPropertyName)
        {
            Contract.Requires(projectPropertyName != null);

            var column = new DataGridTextColumn
            {
                Header = new TextBlock { Text = projectPropertyName.DisplayName },
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                Binding = new Binding(@"PropertyValue[" + projectPropertyName.Name + @"]") { Mode = BindingMode.TwoWay },
            };

            column.EnableMultilineEditing();

            column.SetValue(DataGridFilterColumn.TemplateProperty, AssociatedObject?.FindResource(ResourceKeys.MultipleChoiceFilterTemplate));
            column.SetValue(ProjectPropertyNameProperty, projectPropertyName);
            column.SetValue(PropertyNameProperty, projectPropertyName.Name);

            _columnsCreatedThrottle.Tick();

            return column;
        }

        private static int GetDisplayIndex(ProjectPropertyName projectPropertyName, Configuration configuration)
        {
            if ((projectPropertyName == null) || (configuration == null))
                return -1;

            string[] propertyColumnOrder;

            if (!configuration.PropertyColumnOrder.TryGetValue(projectPropertyName.GroupName.Name, out propertyColumnOrder) || (propertyColumnOrder == null))
                return -1;

            var index = propertyColumnOrder.IndexOf(projectPropertyName.Name);

            return index;
        }

        private class ColumInfo
        {
            [NotNull]
            private readonly DataGridColumn _column;

            public ColumInfo([NotNull] DataGridColumn column, Configuration configuration)
            {
                Contract.Requires(column != null);

                _column = column;

                ProjectPropertyName = column.GetValue(ProjectPropertyNameProperty) as ProjectPropertyName;
                DisplayIndex = GetDisplayIndex(ProjectPropertyName, configuration);
            }

            [NotNull]
            public DataGridColumn Column
            {
                get
                {
                    Contract.Ensures(Contract.Result<DataGridColumn>() != null);
                    return _column;
                }
            }

            public ProjectPropertyName ProjectPropertyName { get; }

            public int DisplayIndex { get; }

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            [Conditional("CONTRACTS_FULL")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_column != null);
            }
        }

        private void ColumnsCreated()
        {
            try
            {
                var dataGrid = AssociatedObject;
                if (dataGrid == null)
                    return;

                var frozenColumnCount = dataGrid.FrozenColumnCount;
                var nextIndex = frozenColumnCount;

                dataGrid.Columns
                    .Skip(frozenColumnCount)
                    .Select(col => new ColumInfo(col, _configuration))
                    .Where(item => item.ProjectPropertyName != null)
                    .OrderBy(col => col.ProjectPropertyName.GroupName.Index)
                    .ThenBy(col => col.DisplayIndex)
                    .ForEach(item => item.Column.DisplayIndex = nextIndex++);
            }
            catch (Exception ex)
            {
                _tracer.TraceError(ex);
            }
        }

        private void ColumnsDisplayIndexChanged()
        {
            try
            {
                var dataGrid = AssociatedObject;
                if (dataGrid == null)
                    return;

                if (_configuration == null)
                    return;

                var columnsByGroup = dataGrid.Columns
                    .Skip(dataGrid.FrozenColumnCount)
                    .OrderBy(col => col.DisplayIndex)
                    .Select(col => col.GetValue(ProjectPropertyNameProperty) as ProjectPropertyName)
                    .Where(property => property != null)
                    .GroupBy(property => property.GroupName);

                var propertyColumnOrder = _configuration.PropertyColumnOrder;
                var hasChanged = false;

                foreach (var group in columnsByGroup)
                {
                    var groupName = group?.Key?.Name;
                    if (groupName == null)
                        continue;

                    var columnNames = group.Select(property => property.Name).ToArray();

                    string[] current;

                    if (propertyColumnOrder.TryGetValue(groupName, out current) && (current?.SequenceEqual(columnNames) == true))
                        continue;

                    propertyColumnOrder[groupName] = columnNames;
                    hasChanged = true;
                }

                if (hasChanged)
                    _configuration.Save();

            }
            catch (Exception ex)
            {
                _tracer.TraceError(ex);
            }
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_columnsCreatedThrottle != null);
            Contract.Invariant(_displayIndexChangedThrottle != null);
        }
    }
}
