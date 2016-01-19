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

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    [DisplayName("Properties")]
    [VisualCompositionExport(GlobalId.ShellRegion, Sequence = 2)]
    class PropertiesViewModel : ObservableObject, IComposablePart
    {
        private readonly Solution _solution;

        [ImportingConstructor]
        public PropertiesViewModel(Solution solution)
        {
            Contract.Requires(solution != null);

            _solution = solution;

            // var propertyNames = solution.ProjectProperties.Select(p => p.Name).ToArray();

            //_propertyGrouping.Groups = new[]
            //{
            //    new PropertyGroup { Name = "Global", Properties = new[] { "ProjectGuid", "OutputType" } },
            //    new PropertyGroup { Name = "CodeContracts", Properties = propertyNames.Where(name => name.StartsWith("CodeContracts", StringComparison.Ordinal)).ToArray()},
            //    new PropertyGroup { Name = "Publish", Properties = new[] { "" }},
            //};
        }

        public Solution Solution
        {
            get
            {
                Contract.Ensures(Contract.Result<Solution>() != null);

                return _solution;
            }
        }

        public ICommand CopyCommand => new DelegateCommand<DataGrid>(CanCopy, Copy);

        private void Copy(DataGrid dataGrid)
        {
            Contract.Requires(dataGrid != null);
            dataGrid.GetCellSelection().SetClipboardData();
        }

        private bool CanCopy(DataGrid dataGrid)
        {
            if (dataGrid == null)
                return false;

            return dataGrid.HasRectangularCellSelection();
        }

        public ICommand PasteCommand => new DelegateCommand<DataGrid>(CanPaste, Paste);

        private void Paste(DataGrid dataGrid)
        {
            Contract.Requires(dataGrid != null);
            dataGrid.PasteCells(ClipboardHelper.GetClipboardDataAsTable());
        }

        private bool CanPaste(DataGrid dataGrid)
        {
            if (dataGrid == null)
                return false;

            return Clipboard.ContainsText() && !dataGrid.SelectedCells.Any(cell => cell.Column.IsReadOnly);
        }

        public ICommand DeleteCommand => new DelegateCommand<DataGrid>(CanDelete, Delete);

        private void Delete(DataGrid dataGrid)
        {
            Contract.Requires(dataGrid != null);
            foreach (var cell in dataGrid.SelectedCells)
            {
                var configuration = (ProjectConfiguration)cell.Item;
                var propertyName = (ProjectPropertyName)cell.Column.GetValue(ProperitesColumnsMananger.ProjectConfigurationProperty);

                configuration.DeleteProperty(propertyName.Name);
            }
        }

        private bool CanDelete(DataGrid dataGrid)
        {
            if (dataGrid == null)
                return false;

            return !dataGrid.SelectedCells.Any(cell => cell.Column.IsReadOnly);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_solution != null);
        }
    }
}
