namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Linq;

    using JetBrains.Annotations;

    using tomenglertde.ProjectConfigurationManager.Model;

    using Throttle;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;
    using TomsToolbox.Wpf.Composition;

    [DisplayName("Fody")]
    [VisualCompositionExport(GlobalId.ShellRegion, Sequence = 6)]
    internal sealed class FodyViewModel
    {
        [ImportingConstructor]
        public FodyViewModel([NotNull] Solution solution)
        {
            Solution = solution;
            solution.Changed += Solution_Changed;

            ConfigurationMappings = solution.Projects.ObservableSelect(project => new FodyConfigurationMapping(project, WeaverConfigurations));
        }

        [NotNull]
        public Solution Solution { get; }

        public ICollection<FodyConfigurationMapping> ConfigurationMappings { get; }

        [NotNull]
        public ICollection<FodyWeaverConfiguration> WeaverConfigurations { get; } = new ObservableCollection<FodyWeaverConfiguration>();

        private void Solution_Changed([NotNull] object sender, [NotNull] EventArgs e)
        {
            UpdateConfigurations();
        }

        [Throttled(typeof(DispatcherThrottle))]
        private void UpdateConfigurations()
        {
            WeaverConfigurations.SynchronizeWith(FodyWeaver.EnumerateWeavers(Solution)
                .GroupBy(weaver => weaver.WeaverName)
                .Select(weavers => new FodyWeaverConfiguration(weavers.Key, weavers.ToArray()))
                .ToArray());
        }
    }

    public class FodyConfigurationMapping
    {
        public FodyConfigurationMapping([NotNull] Project project, [NotNull] ICollection<FodyWeaverConfiguration> weaverConfigurations)
        {
            Project = project;
            Configuration = new ConfigurationIndexer(project, weaverConfigurations);
        }

        private class ConfigurationIndexer : IIndexer<int>
        {
            [NotNull]
            private readonly ICollection<FodyWeaverConfiguration> _weaverConfigurations;
            [NotNull]
            private readonly Project _project;

            public ConfigurationIndexer([NotNull] Project project, [NotNull] ICollection<FodyWeaverConfiguration> weaverConfigurations)
            {
                _weaverConfigurations = weaverConfigurations;
                _project = project;
            }

            public int this[string weaver]
            {
                get
                {
                    var weaverConfiguration = _weaverConfigurations.FirstOrDefault(w => w.Name == weaver);
                    var projectConfiguration = weaverConfiguration?.Weavers.FirstOrDefault(w => w.Project == _project)?.Configuration;
                    if (projectConfiguration == null)
                        return string.IsNullOrEmpty(weaverConfiguration?.Configuration["0"]) ? -1 : 0;

                    var weaverConfigurations = weaverConfiguration.Configurations;
                    var weaverProjectConfiguration = weaverConfigurations.LastOrDefault(c => c == projectConfiguration);
                    if (weaverProjectConfiguration == null)
                        return -1;

                    return weaverConfigurations.IndexOf(weaverProjectConfiguration);
                }
                set => throw new NotImplementedException();
            }
        }

        [NotNull]
        public Project Project { get; }

        [NotNull]
        public IIndexer<int> Configuration { get; }
    }
}
