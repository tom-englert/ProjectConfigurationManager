using System.Diagnostics.Contracts;

namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;
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

        public ICommand UnloadProjectsCommand => new DelegateCommand<IEnumerable>(UnloadProjects);

        private void UnloadProjects([NotNull] IEnumerable projects)
        {
            Contract.Requires(projects != null);

            foreach (var project in projects.OfType<Project>())
            {
                if (project.IsLoaded)
                {
                    project.UnloadProject();
                }
            }
        }
    }
}
