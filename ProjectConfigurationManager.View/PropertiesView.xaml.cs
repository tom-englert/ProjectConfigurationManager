namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
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
        private readonly ITracer _tracer;

        [ImportingConstructor]
        public PropertiesView(ITracer tracer)
        {
            Contract.Requires(tracer != null);
            _tracer = tracer;

            InitializeComponent();
        }

        private bool FilterPredicate(ProjectConfiguration item, IEnumerable<string> selectedGuids)
        {
            Contract.Requires(selectedGuids != null);

            return (item != null) && item.Project.ProjectTypeGuids.Any(guid => selectedGuids.Contains(guid, StringComparer.OrdinalIgnoreCase));
        }

        private void ProjectTypeGuids_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Contract.Requires(sender != null);

            var listBox = (ListBox)sender;

            var selectedGuids = listBox.SelectedItems.OfType<string>().ToArray();

            DataGridFilter.SetGlobalFilter(DataGrid, item => FilterPredicate(item as ProjectConfiguration, selectedGuids));
        }

        private void ConfirmedCommandConverter_Error(object sender, ErrorEventArgs e)
        {
            _tracer.TraceError(e.GetException());
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_tracer != null);
            Contract.Invariant(DataGrid != null);
        }
    }
}
