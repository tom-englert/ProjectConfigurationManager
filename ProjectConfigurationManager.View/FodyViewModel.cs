namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.IO;
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
            solution.FileChanged += Solution_FileChanged;

            ConfigurationMappings = solution.Projects.ObservableSelect(project => new FodyConfigurationMapping(project, WeaverConfigurations));
        }

        [NotNull]
        public Solution Solution { get; }

        [NotNull, ItemNotNull]
        public ICollection<FodyConfigurationMapping> ConfigurationMappings { get; }

        [NotNull, ItemNotNull]
        public ICollection<FodyWeaverConfiguration> WeaverConfigurations { get; } = new ObservableCollection<FodyWeaverConfiguration>();

        private void Solution_Changed([NotNull] object sender, [NotNull] EventArgs e)
        {
            UpdateConfigurations();
        }

        private void Solution_FileChanged([NotNull] object sender, [NotNull] FileSystemEventArgs e)
        {
            var changedFile = Path.GetFileName(e.Name);

            if (changedFile?.StartsWith(FodyWeaver.ConfigurationFileName, StringComparison.OrdinalIgnoreCase) != true)
                return;

            UpdateConfigurations();
        }

        [Throttled(typeof(DispatcherThrottle))]
        private void UpdateConfigurations()
        {
            WeaverConfigurations.SynchronizeWith(FodyWeaver.EnumerateWeavers(Solution)
                .GroupBy(weaver => weaver.WeaverName)
                .Select(weavers => new FodyWeaverConfiguration(weavers.Key, weavers.ToArray()))
                .ToArray());

            foreach (var configuration in WeaverConfigurations)
            {
                configuration.Update();
            }

            foreach (var mapping in ConfigurationMappings)
            {
                mapping.Update();
            }
        }
    }
}
