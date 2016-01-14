namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Controls;

    using DataGridExtensions;

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for PropertiesView.xaml
    /// </summary>
    [DataTemplate(typeof(PropertiesViewModel))]
    public partial class PropertiesView
    {
        public PropertiesView()
        {
            InitializeComponent();
        }

        private bool FilterPredicate(ProjectConfiguration item, IEnumerable<string> selectedGuids)
        {
            return (item != null) && item.Project.ProjectTypeGuids.Any(guid => selectedGuids.Contains(guid, StringComparer.OrdinalIgnoreCase));
        }

        private void ProjectTypeGuids_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)sender;

            var selectedGuids = listBox.SelectedItems.OfType<string>().ToArray();

            DataGridFilter.SetGlobalFilter(DataGrid, item => FilterPredicate(item as ProjectConfiguration, selectedGuids));
        }
    }
}
