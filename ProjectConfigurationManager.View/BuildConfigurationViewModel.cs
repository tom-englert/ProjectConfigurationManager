namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    [DisplayName("Build Configuration")]
    [VisualCompositionExport(GlobalId.ShellRegion, Sequence = 1)]
    internal sealed class BuildConfigurationViewModel : ObservableObject
    {
        [ImportingConstructor]
        public BuildConfigurationViewModel([NotNull] Solution solution)
        {
            Contract.Requires(solution != null);

            Solution = solution;
        }

        [NotNull]
        public Solution Solution { get; }

        [NotNull, ItemNotNull]
        public ICollection<ProjectConfiguration> SelectedConfigurations { get; } = new ObservableCollection<ProjectConfiguration>();

        [NotNull]
        public ICommand DeleteCommand => new DelegateCommand(CanDelete, Delete);

        private void Delete()
        {
            var configurations = SelectedConfigurations.ToArray();

            configurations.ForEach(c => c?.Delete());

            Solution.Update();
        }

        private bool CanDelete()
        {
            var canEditAllFiles = SelectedConfigurations
                .Select(cfg => cfg.Project)
                .Distinct()
                // ReSharper disable once PossibleNullReferenceException
                .All(prj => prj.CanEdit());

            var shouldNotBuildAny = SelectedConfigurations
                .All(cfg => !cfg.ShouldBuildInAnyConfiguration());

            return SelectedConfigurations.Any()
                   && canEditAllFiles
                   && shouldNotBuildAny;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(Solution != null);
            Contract.Invariant(SelectedConfigurations != null);
        }
    }
}
