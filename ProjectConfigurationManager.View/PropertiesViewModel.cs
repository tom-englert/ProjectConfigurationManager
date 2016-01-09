namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf.Composition;

    [DisplayName("Properties")]
    [VisualCompositionExport(GlobalId.ShellRegion, Sequence = 2)]
    class PropertiesViewModel : ObservableObject, IComposablePart
    {
        private readonly Solution _solution;
        private PropertyGrouping _propertyGrouping;

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

        public PropertyGrouping PropertyGrouping
        {
            get
            {
                return _propertyGrouping;
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
