namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.ComponentModel;

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf.Composition;

    [DisplayName("Project Types")]
    [VisualCompositionExport(GlobalId.ShellRegion, Sequence = 3)]
    class ProjectTypesViewModel : ObservableObject
    {
        private readonly Solution _solution;

        public ProjectTypesViewModel(Solution solution)
        {
            _solution = solution;
        }

        public Solution Solution => _solution;
    }
}
