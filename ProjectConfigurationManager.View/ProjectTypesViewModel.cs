namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.Collections;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    [DisplayName("Project Types")]
    [VisualCompositionExport(GlobalId.ShellRegion, Sequence = 3)]
    internal sealed class ProjectTypesViewModel : ObservableObject
    {
        public ProjectTypesViewModel([NotNull] Solution solution)
        {
            Solution = solution;
        }

        [NotNull]
        public Solution Solution { get; }

        [NotNull, UsedImplicitly]
        public static ICommand UnloadProjectsCommand => new DelegateCommand<IEnumerable>(UnloadProjects);

        private static void UnloadProjects([NotNull, ItemNotNull] IEnumerable projects)
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
