namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using tomenglertde.ProjectConfigurationManager.Model;

    using Throttle;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;
    using TomsToolbox.Wpf.Composition;

    [DisplayName("Fody")]
    [VisualCompositionExport(GlobalId.ShellRegion, Sequence = 6)]
    internal sealed class FodyViewModel : INotifyPropertyChanged

    {
        [NotNull]
        private readonly Solution _solution;
        [NotNull, ItemNotNull]
        private FodyWeaver[] _weavers = new FodyWeaver[0];

        [ImportingConstructor]
        public FodyViewModel([NotNull] Solution solution)
        {
            _solution = solution;
            solution.Changed += Solution_Changed;
            solution.FileChanged += Solution_FileChanged;

            ConfigurationMappings = solution.Projects.ObservableSelect(project => new FodyConfigurationMapping(this, project));

            UpdateConfigurations();
        }

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
            var weavers = FodyWeaver.EnumerateWeavers(_solution)
                .ToArray();

            var sortedNames = GenerateSortedWeaverNames(weavers);

            var configurations = weavers
                .GroupBy(weaver => weaver.WeaverName)
                // ReSharper disable once AssignNullToNotNullAttribute
                .Select(weaversByName => new FodyWeaverConfiguration(this, weaversByName.Key, weaversByName.OrderBy(w => w?.Configuration).ToArray(), sortedNames.IndexOf(weaversByName.Key)))
                .ToArray();

            _weavers = weavers;
            WeaverConfigurations.Clear();
            WeaverConfigurations.AddRange(configurations);

            foreach (var configuration in configurations)
            {
                configuration?.Update();
            }

            foreach (var mapping in ConfigurationMappings)
            {
                mapping.Update();
            }
        }

        [NotNull, ItemNotNull]
        private static ICollection<string> GenerateSortedWeaverNames([NotNull, ItemNotNull] IList<FodyWeaver> allWeavers)
        {
            var indexedNames = new List<string>();

            var weaversByProject = allWeavers.GroupBy(weaver => weaver.Project);

            foreach (var weaversOfProject in weaversByProject)
            {
                if (weaversOfProject.Key == null)
                    continue;

                var weavers = weaversOfProject
                    .OrderBy(item => item?.Index)
                    .ToArray();

                for (var index = 0; index < weavers.Length; index++)
                {
                    var weaver = weavers[index];
                    Debug.Assert(weaver != null, nameof(weaver) + " != null");
                    var weaverName = weaver.WeaverName;

                    if (indexedNames.Contains(weaverName))
                        continue;

                    var weaversAfter = weavers.Skip(index + 1).ToArray();
                    var targetIndex = weaversAfter
                        .Select(item => indexedNames.IndexOf(item?.WeaverName))
                        .Where(i => i >= 0)
                        .DefaultIfEmpty(-1)
                        .Min();

                    if (targetIndex >= 0)
                    {
                        indexedNames.Insert(targetIndex, weaverName);
                    }
                    else
                    {
                        indexedNames.Add(weaverName);
                    }
                }
            }

            return indexedNames;
        }

        public void OnWeaverIndexChanged()
        {
            var weaversByProject = _weavers.GroupBy(weaver => weaver.Project);

            foreach (var weaversOfProject in weaversByProject)
            {
                var project = weaversOfProject.Key;
                if (project == null)
                    continue;

                var projectFolder = project.Folder;

                var document = FodyWeaver.LoadDocument(projectFolder);
                var root = document?.Root;
                if (root == null)
                    continue;

                if (!SortWeavers(root))
                    continue;

                FodyWeaver.SaveDocument(projectFolder, document);
            }

            UpdateConfigurations();
        }

        public bool SortWeavers([NotNull] XElement root)
        {
            var orderedWeaverNames = WeaverConfigurations
                .OrderBy(item => item.Index)
                .Select(item => item.Name)
                .ToArray();

            var weaverElements = root.Elements()
                .ToArray();

            var sortedWeavers = weaverElements
                .OrderBy(w => orderedWeaverNames.IndexOf(w?.Name.LocalName))
                .ToArray();

            if (weaverElements.SequenceEqual(sortedWeavers))
                return false;

            foreach (var weaver in weaverElements)
            {
                // ReSharper disable once PossibleNullReferenceException
                weaver.Remove();
            }

            foreach (var weaver in sortedWeavers)
            {
                root.Add(weaver);
            }

            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator, UsedImplicitly]
        private void OnPropertyChanged([NotNull] string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
