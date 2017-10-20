namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
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

    using Throttle;

    using TomsToolbox.Core;
    using TomsToolbox.ObservableCollections;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Interactivity;

    public sealed class PropertiesColumnsManagerBehavior : FrameworkElementBehavior<DataGrid>
    {
        [CanBeNull, ItemNotNull]
        private IObservableCollection<object> _projectPropertyNames;
        [CanBeNull]
        private Configuration _configuration;
        [CanBeNull]
        private ITracer _tracer;

        [NotNull]
        internal static readonly DependencyProperty ProjectPropertyNameProperty =
            DependencyProperty.RegisterAttached("ProjectPropertyName", typeof(ProjectPropertyName), typeof(PropertiesColumnsManagerBehavior), new FrameworkPropertyMetadata(null));


        [CanBeNull]
        public static string GetPropertyName([NotNull] DependencyObject obj)
        {
            Contract.Requires(obj != null);
            return (string)obj.GetValue(PropertyNameProperty);
        }
        public static void SetPropertyName([NotNull] DependencyObject obj, [CanBeNull] string value)
        {
            Contract.Requires(obj != null);
            obj.SetValue(PropertyNameProperty, value);
        }
        [NotNull]
        public static readonly DependencyProperty PropertyNameProperty =
            DependencyProperty.RegisterAttached("PropertyName", typeof(string), typeof(PropertiesColumnsManagerBehavior), new FrameworkPropertyMetadata(null));


        [CanBeNull, ItemNotNull, UsedImplicitly]
        public ICollection Properties
        {
            get => (ICollection)GetValue(PropertiesProperty);
            set => SetValue(PropertiesProperty, value);
        }
        [NotNull]
        public static readonly DependencyProperty PropertiesProperty =
            // ReSharper disable once PossibleNullReferenceException
            DependencyProperty.Register("Properties", typeof(ICollection), typeof(PropertiesColumnsManagerBehavior), new FrameworkPropertyMetadata(null, (sender, e) => ((PropertiesColumnsManagerBehavior)sender).Properties_Changed(e.NewValue as IList<object>)));


        protected override void OnAttached()
        {
            base.OnAttached();

            var exportProvider = AssociatedObject?.GetExportProvider();

            _configuration = exportProvider?.GetExportedValue<Configuration>();
            _tracer = exportProvider?.GetExportedValue<ITracer>();

            Initialize();
        }

        private void Properties_Changed([CanBeNull, ItemNotNull] IList<object> propertyGroups)
        {
            if (propertyGroups == null)
                return;

            _projectPropertyNames = propertyGroups.ObservableSelectMany(group => ((CollectionViewGroup)group).Items);
            // ReSharper disable once AssignNullToNotNullAttribute
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
            dataGrid.ColumnDisplayIndexChanged += (_, __) => OnColumnsDisplayIndexChanged();
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
                        // ReSharper disable once PossibleNullReferenceException
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

        [NotNull]
        private DataGridColumn CreateColumn([NotNull] ProjectPropertyName projectPropertyName)
        {
            Contract.Requires(projectPropertyName != null);
            Contract.Ensures(Contract.Result<DataGridColumn>() != null);

            var column = new DataGridTextColumn
            {
                Header = new TextBlock { Text = projectPropertyName.DisplayName },
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                Binding = new Binding(@"PropertyValue[" + projectPropertyName.Name + @"]") { Mode = BindingMode.TwoWay },
            };

            column.EnableMultilineEditing();

            // ReSharper disable once AssignNullToNotNullAttribute
            column.SetValue(DataGridFilterColumn.TemplateProperty, AssociatedObject?.FindResource(ResourceKeys.MultipleChoiceFilterTemplate));
            column.SetValue(ProjectPropertyNameProperty, projectPropertyName);
            column.SetValue(PropertyNameProperty, projectPropertyName.Name);

            OnColumnCreated();

            return column;
        }

        private static int GetDisplayIndex([CanBeNull] ProjectPropertyName projectPropertyName, [CanBeNull] Configuration configuration)
        {
            if ((projectPropertyName == null) || (configuration == null))
                return -1;

            var columnOrder = configuration.PropertyColumnOrder;
            if (columnOrder == null)
                return -1;

            if (!columnOrder.TryGetValue(projectPropertyName.GroupName.Name, out var propertyColumnOrder) || (propertyColumnOrder == null))
                return -1;

            var index = propertyColumnOrder.IndexOf(projectPropertyName.Name);

            return index;
        }

        private sealed class ColumInfo
        {
            [NotNull]
            private readonly DataGridColumn _column;

            public ColumInfo([NotNull] DataGridColumn column, [CanBeNull] Configuration configuration)
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

            [CanBeNull]
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

        [Throttled(typeof(DispatcherThrottle), (int)DispatcherPriority.Background)]
        private void OnColumnCreated()
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
                    // ReSharper disable once AssignNullToNotNullAttribute
                    .Select(col => new ColumInfo(col, _configuration))
                    .Where(col => col.ProjectPropertyName != null)
                    .OrderBy(col => col.ProjectPropertyName.GroupName.Index)
                    // ReSharper disable PossibleNullReferenceException
                    .ThenBy(col => col.DisplayIndex)
                    .ForEach(col => col.Column.DisplayIndex = nextIndex++);
                // ReSharper restore PossibleNullReferenceException
            }
            catch (Exception ex)
            {
                _tracer?.TraceError(ex);
            }
        }

        [Throttled(typeof(DispatcherThrottle), (int)DispatcherPriority.Background)]
        private void OnColumnsDisplayIndexChanged()
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
                    // ReSharper disable once PossibleNullReferenceException
                    .OrderBy(col => col.DisplayIndex)
                    // ReSharper disable once PossibleNullReferenceException
                    .Select(col => col.GetValue(ProjectPropertyNameProperty) as ProjectPropertyName)
                    .Where(property => property != null)
                    .GroupBy(property => property.GroupName);

                var propertyColumnOrder = _configuration.PropertyColumnOrder ?? ImmutableDictionary<string, string[]>.Empty;

                foreach (var group in columnsByGroup)
                {
                    var groupName = group.Key?.Name;
                    if (groupName == null)
                        continue;

                    // ReSharper disable once PossibleNullReferenceException
                    var columnNames = group.Select(property => property.Name).ToArray();

                    // ReSharper disable once PossibleNullReferenceException
                    if (propertyColumnOrder.TryGetValue(groupName, out var current) && (current?.SequenceEqual(columnNames) == true))
                        continue;

                    _configuration.PropertyColumnOrder = propertyColumnOrder.SetItem(groupName, columnNames);
                }
            }
            catch (Exception ex)
            {
                _tracer?.TraceError(ex);
            }
        }
    }
}
