namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Markup;

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
            Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
        }

        private static bool FilterPredicate([CanBeNull] ProjectConfiguration item, [NotNull, ItemNotNull] IEnumerable<string> selectedGuids)
        {
            Contract.Requires(selectedGuids != null);

            return (item != null) && item.Project.ProjectTypeGuids.All(guid => selectedGuids.Contains(guid, StringComparer.OrdinalIgnoreCase));
        }

        private void ProjectTypeGuids_SelectionChanged([NotNull] object sender, [NotNull] SelectionChangedEventArgs e)
        {
            Contract.Requires(sender != null);

            var listBox = (ListBox)sender;

            var selectedGuids = listBox.SelectedItems.OfType<string>().ToArray();

            var dataGrid = DataGrid;
            Contract.Assume(dataGrid != null);
            DataGridFilter.SetGlobalFilter(dataGrid, item => FilterPredicate(item as ProjectConfiguration, selectedGuids));
        }

        private void ProjectTypeGuids_Loaded([NotNull] object sender, [NotNull] RoutedEventArgs e)
        {
            Contract.Requires(sender != null);

            var listBox = (ListBox)sender;

            listBox.BeginInvoke(() => listBox.SelectAll());
        }

        private void ConfirmedCommandConverter_Error([NotNull] object sender, [NotNull] ErrorEventArgs e)
        {
            var exception = e.GetException();
            if (exception == null)
                return;

            _tracer.TraceError(exception);
        }

        private void ConfirmedCommandConverter_OnExecuting([NotNull] object sender, [NotNull] ConfirmedCommandEventArgs e)
        {
            WaitCursor.StartLocal(this);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_tracer != null);
            Contract.Invariant(DataGrid != null);
        }
    }
}
