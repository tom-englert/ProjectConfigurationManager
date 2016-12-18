namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    using DataGridExtensions;

    using JetBrains.Annotations;

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    [DisplayName("Properties")]
    [VisualCompositionExport(GlobalId.ShellRegion, Sequence = 2)]
    class PropertiesViewModel : ObservableObject
    {
        [NotNull]
        private readonly Solution _solution;

        [ImportingConstructor]
        public PropertiesViewModel([NotNull] Solution solution)
        {
            Contract.Requires(solution != null);

            _solution = solution;
        }

        [NotNull]
        public Solution Solution
        {
            get
            {
                Contract.Ensures(Contract.Result<Solution>() != null);

                return _solution;
            }
        }

        public static ICommand CopyCommand => new DelegateCommand<DataGrid>(CanCopy, Copy);

        private static void Copy([NotNull] DataGrid dataGrid)
        {
            Contract.Requires(dataGrid != null);

            dataGrid.GetCellSelection().SetClipboardData();
        }

        private static bool CanCopy(DataGrid dataGrid)
        {
            return dataGrid?.HasRectangularCellSelection() ?? false;
        }

        public static ICommand PasteCommand => new DelegateCommand<DataGrid>(CanPaste, Paste);

        private static void Paste([NotNull] DataGrid dataGrid)
        {
            Contract.Requires(dataGrid != null);

            var data = ClipboardHelper.GetClipboardDataAsTable();
            if (data == null)
                return;

            dataGrid.PasteCells(data);
            dataGrid.CommitEdit();
        }

        private static bool CanPaste(DataGrid dataGrid)
        {
            return Clipboard.ContainsText() && (dataGrid?.SelectedCells?.Any(cell => cell.Column.IsReadOnly) == false);
        }

        public static ICommand DeleteCommand => new DelegateCommand<DataGrid>(CanDelete, Delete);

        private static void Delete([NotNull] DataGrid dataGrid)
        {
            Contract.Requires(dataGrid != null);
            Contract.Requires(dataGrid.SelectedCells != null);

            foreach (var cell in dataGrid.SelectedCells)
            {
                var configuration = (ProjectConfiguration)cell.Item;
                if (configuration == null)
                    return;

                var propertyName = (string)cell.Column?.GetValue(PropertiesColumnsManagerBehavior.PropertyNameProperty);
                if (string.IsNullOrEmpty(propertyName))
                    continue;

                var projectPropertyName = cell.Column?.GetValue(PropertiesColumnsManagerBehavior.ProjectPropertyNameProperty) as ProjectPropertyName;
                if (projectPropertyName == null)
                {
                    configuration = configuration.Project.DefaultProjectConfiguration;
                }

                configuration.DeleteProperty(propertyName);
            }
        }

        private static bool CanDelete(DataGrid dataGrid)
        {
            return dataGrid?.SelectedCells?.Any(cell => cell.Column.IsReadOnly) == false;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_solution != null);
        }
    }
}
