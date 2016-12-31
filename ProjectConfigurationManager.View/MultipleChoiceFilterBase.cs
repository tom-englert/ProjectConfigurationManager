namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;

    using DataGridExtensions;

    using JetBrains.Annotations;

    using TomsToolbox.Wpf;

    [TemplatePart(Name = "PART_ListBox", Type = typeof(ListBox))]
    public abstract class MultipleChoiceFilterBase : Control
    {
        private ListBox _listBox;

        static MultipleChoiceFilterBase()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultipleChoiceFilterBase), new FrameworkPropertyMetadata(typeof(MultipleChoiceFilterBase)));
        }

        protected MultipleChoiceFilterBase()
        {
            Values = new ObservableCollection<string>();
        }

        public MultipleChoiceContentFilterBase Filter
        {
            get { return (MultipleChoiceContentFilterBase)GetValue(FilterProperty); }
            set { SetValue(FilterProperty, value); }
        }
        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.Register("Filter", typeof(MultipleChoiceContentFilterBase), typeof(MultipleChoiceFilterBase), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (sender, e) => ((MultipleChoiceFilterBase)sender).Filter_Changed()));


        private static readonly DependencyProperty SourceValuesProperty =
            DependencyProperty.Register("SourceValues", typeof(IList<string>), typeof(MultipleChoiceFilterBase), new FrameworkPropertyMetadata(null, (sender, e) => ((MultipleChoiceFilterBase)sender).SourceValues_Changed((IList<string>)e.NewValue)));

        private void SourceValues_Changed(IEnumerable<string> newValue)
        {
            OnSourceValuesChanged(newValue);
        }

        [NotNull]
        public IList<string> Values
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            get { return (IList<string>)GetValue(ValuesProperty); }
            private set { SetValue(ValuesPropertyKey, value); }
        }
        private static readonly DependencyPropertyKey ValuesPropertyKey =
            DependencyProperty.RegisterReadOnly("Values", typeof(IList<string>), typeof(MultipleChoiceFilterBase), new FrameworkPropertyMetadata());
        public static readonly DependencyProperty ValuesProperty = ValuesPropertyKey.DependencyProperty;


        /// <summary>When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.</summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var filterColumnControl = this.TryFindAncestor<DataGridFilterColumnControl>();
            Contract.Assume(filterColumnControl != null);

            BindingOperations.SetBinding(this, FilterProperty, new Binding { Source = filterColumnControl, Path = new PropertyPath(DataGridFilterColumnControl.FilterProperty) });
            BindingOperations.SetBinding(this, SourceValuesProperty, new Binding { Source = filterColumnControl, Path = new PropertyPath(nameof(DataGridFilterColumnControl.SourceValues)) });

            var dataGrid = filterColumnControl.TryFindAncestor<DataGrid>();
            if (dataGrid == null)
                return;

            var dataGridItems = (INotifyCollectionChanged)dataGrid.Items;
            dataGridItems.CollectionChanged += (_, __) => BindingOperations.GetBindingExpression(this, SourceValuesProperty)?.UpdateTarget();

            _listBox = Template?.FindName("PART_ListBox", this) as ListBox;
            if (_listBox == null)
                return;

            var filter = Filter;

            if (filter?.Items == null)
            {
                _listBox.SelectAll();
            }

            _listBox.SelectionChanged += ListBox_SelectionChanged;
            var items = (INotifyCollectionChanged)_listBox.Items;

            items.CollectionChanged += ListBox_ItemsCollectionChanged;
        }

        [NotNull]
        protected abstract MultipleChoiceContentFilterBase CreateFilter(IEnumerable<string> items);

        protected abstract void OnSourceValuesChanged(IEnumerable<string> newValue);

        private void ListBox_ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            var filter = Filter;

            if (filter?.Items == null)
            {
                _listBox?.SelectAll();
            }
        }

        private void Filter_Changed()
        {
            var listBox = _listBox;
            if (listBox == null)
                return;

            var filter = Filter;
            if (filter?.Items == null)
            {
                listBox.SelectAll();
                return;
            }

            if (listBox.SelectedItems.Count != 0)
                return;

            foreach (var item in filter.Items)
            {
                listBox.SelectedItems.Add(item);
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)sender;
            Contract.Assume(listBox != null);

            var selectedItems = listBox.SelectedItems.Cast<string>().ToArray();

            var areAllItemsSelected = listBox.Items.Count == selectedItems.Length;

            Filter = CreateFilter(areAllItemsSelected ? null : selectedItems);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(Values != null);
        }
    }

    public abstract class MultipleChoiceContentFilterBase : IContentFilter
    {
        protected MultipleChoiceContentFilterBase(IEnumerable<string> items)
        {
            Items = items?.ToArray();
        }

        public IList<string> Items
        {
            get;
        }

        public abstract bool IsMatch(object value);
    }
}
