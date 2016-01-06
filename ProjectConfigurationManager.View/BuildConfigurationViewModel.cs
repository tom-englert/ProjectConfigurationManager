namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf.Composition;

    [DisplayName("Build Configuration")]
    [VisualCompositionExport(GlobalId.ShellRegion)]
    class BuildConfigurationViewModel : ObservableObject, IComposablePart
    {
        private readonly Solution _solution;

        [ImportingConstructor]
        public BuildConfigurationViewModel(Solution solution)
        {
            Contract.Requires(solution != null);

            _solution = solution;
        }

        public Solution Solution
        {
            get
            {
                Contract.Ensures(Contract.Result<Solution>() != null);

                return _solution;
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_solution != null);
        }
    }
}
