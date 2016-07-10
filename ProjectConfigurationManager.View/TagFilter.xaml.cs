namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;

    using DataGridExtensions;

    using TomsToolbox.Core;
    using TomsToolbox.Wpf;

    /// <summary>
    /// Interaction logic for MultipleChoiceFilter.xaml
    /// </summary>
    public partial class TagFilter
    {
        internal static readonly Regex Regex = new Regex(@"\W+", RegexOptions.Compiled);
        private readonly ObservableCollection<string> _tags = new ObservableCollection<string>();

        private ListBox _listBox;


        public TagFilter()
        {
            InitializeComponent();
        }

        public TagsContentFilter Filter
        {
            get { return (TagsContentFilter)GetValue(FilterProperty); }
            set { SetValue(FilterProperty, value); }
        }
        /// <summary>
        /// Identifies the Filter dependency property
        /// </summary>
        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.Register("Filter", typeof(TagsContentFilter), typeof(TagFilter), new FrameworkPropertyMetadata(new TagsContentFilter(null), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (sender, e) => ((TagFilter)sender).Filter_Changed()));


        public IList<string> Values
        {
            get { return (IList<string>)GetValue(ValuesProperty); }
            set { SetValue(ValuesProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="Values"/> dependency property
        /// </summary>
        public static readonly DependencyProperty ValuesProperty =
            DependencyProperty.Register("Values", typeof(IList<string>), typeof(TagFilter), new FrameworkPropertyMetadata(null, (sender, e) => ((TagFilter)sender).Values_Changed((IList<string>)e.NewValue)));


        public IList<string> Tags => _tags;


        private void Values_Changed(IEnumerable<string> newValue)
        {
            if (newValue == null)
                _tags.Clear();
            else
                _tags.SynchronizeWith(new[] { string.Empty }.Concat(newValue.SelectMany(x => Regex.Split(x))).Distinct().ToArray());
        }

        /// <summary>When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.</summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var filterColumnControl = this.TryFindAncestor<DataGridFilterColumnControl>();
            Contract.Assume(filterColumnControl != null);

            BindingOperations.SetBinding(this, FilterProperty, new Binding { Source = filterColumnControl, Path = new PropertyPath(DataGridFilterColumnControl.FilterProperty) });
            BindingOperations.SetBinding(this, ValuesProperty, new Binding { Source = filterColumnControl, Path = new PropertyPath(nameof(DataGridFilterColumnControl.SourceValues)) });

            var dataGrid = filterColumnControl.TryFindAncestor<DataGrid>();
            Contract.Assume(dataGrid != null);
            ((INotifyCollectionChanged)dataGrid.Items).CollectionChanged += (_, __) => BindingOperations.GetBindingExpression(this, ValuesProperty)?.UpdateTarget();

            _listBox = Template?.FindName("ListBox", this) as ListBox;
            if (_listBox == null)
                return;

            var filter = Filter;

            if (filter?.Items == null)
            {
                _listBox.SelectAll();
            }

            var items = _listBox.Items as INotifyCollectionChanged;
            items.CollectionChanged += ListBox_ItemsCollectionChanged;
        }

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

            Filter = new TagsContentFilter(areAllItemsSelected ? null : selectedItems);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_tags != null);
        }
    }
    public class TagsContentFilter : IContentFilter
    {
        public TagsContentFilter(IEnumerable<string> items)
        {
            Items = items?.ToArray();
        }

        public IList<string> Items
        {
            get;
        }

        public bool IsMatch(object value)
        {
            var input = value as string;
            if (string.IsNullOrWhiteSpace(input))
                return Items?.Contains(string.Empty) ?? true;

            var tags = TagFilter.Regex.Split(input);

            return Items?.ContainsAny(tags) ?? true;
        }
    }
}
