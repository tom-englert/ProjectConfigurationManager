namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;

    using DataGridExtensions;

    using JetBrains.Annotations;

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Converters;

    /// <summary>
    /// Interaction logic for PropertiesView.xaml
    /// </summary>
    [DataTemplate(typeof(PropertiesViewModel))]
    public partial class PropertiesView
    {
        [NotNull]
        private readonly ITracer _tracer;

        [ImportingConstructor]
        public PropertiesView([NotNull] ExportProvider exportProvider, [NotNull] ITracer tracer)
        {
            Contract.Requires(exportProvider != null);
            Contract.Requires(tracer != null);

            this.SetExportProvider(exportProvider);
            _tracer = tracer;

            InitializeComponent();
        }

        private static bool FilterPredicate(ProjectConfiguration item, [NotNull] IEnumerable<string> selectedGuids)
        {
            Contract.Requires(selectedGuids != null);

            return (item != null) && item.Project.ProjectTypeGuids.All(guid => selectedGuids.Contains(guid, StringComparer.OrdinalIgnoreCase));
        }

        private void ProjectTypeGuids_SelectionChanged([NotNull] object sender, SelectionChangedEventArgs e)
        {
            Contract.Requires(sender != null);

            var listBox = (ListBox)sender;

            var selectedGuids = listBox.SelectedItems.OfType<string>().ToArray();

            DataGridFilter.SetGlobalFilter(DataGrid, item => FilterPredicate(item as ProjectConfiguration, selectedGuids));
        }

        private void ProjectTypeGuids_Loaded([NotNull] object sender, RoutedEventArgs e)
        {
            Contract.Requires(sender != null);

            var listBox = (ListBox)sender;

            listBox.BeginInvoke(() => listBox.SelectAll());
        }

        private void ConfirmedCommandConverter_Error(object sender, ErrorEventArgs e)
        {
            var exception = e.GetException();
            if (exception == null)
                return;

            _tracer.TraceError(exception);
        }

        private void ConfirmedCommandConverter_OnExecuting(object sender, ConfirmedCommandEventArgs e)
        {
            WaitCursor.StartLocal(this);
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
