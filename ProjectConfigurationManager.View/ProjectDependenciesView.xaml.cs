namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Markup;
    using System.Windows.Threading;

    using JetBrains.Annotations;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for ProjectDependenciesView.xaml
    /// </summary>
    [DataTemplate(typeof(ProjectDependenciesViewModel))]
    public partial class ProjectDependenciesView
    {
        [CanBeNull, ItemNotNull]
        private TreeViewItem[] _ancestors;

        public ProjectDependenciesView()
        {
            InitializeComponent();
            Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
        }

        private void TreeViewItem_Expanded([NotNull] object sender, [NotNull] RoutedEventArgs e)
        {
            ExpandItems(sender, true);
        }

        private void TreeViewItem_Collapsed([NotNull] object sender, [NotNull] RoutedEventArgs e)
        {
            ExpandItems(sender, false);
        }

        private void ExpandItems([NotNull] object sender, bool isExpanded)
        {
            if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                return;

            var treeViewItem = (sender as TreeViewItem);
            if (treeViewItem == null)
                return;

            var dispatcher = Dispatcher;
            if (dispatcher == null)
                return;

            // We get events from all ancestors, too! Must block them, else we would always expand/collapse the whole tree.
            if (_ancestors == null)
            {
                // This is the initial event, trigged by the user.
                _ancestors = treeViewItem.Ancestors().OfType<TreeViewItem>().ToArray();
            }
            else if (_ancestors.Contains(treeViewItem))
            {
                // This is a TreeView generated event of any ancestor of the initial item.
                return;
            }

            treeViewItem.VisualDescendants()
                .OfType<TreeViewItem>()
                // ReSharper disable once PossibleNullReferenceException
                .ForEach(i => dispatcher.BeginInvoke(DispatcherPriority.Input, () => i.IsExpanded = isExpanded));

            dispatcher.BeginInvoke(DispatcherPriority.Background, () => _ancestors = null);
        }
    }
}
