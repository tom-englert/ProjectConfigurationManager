namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using tomenglertde.ProjectConfigurationManager.Model;

    public class FodyWeaver
    {
        private FodyWeaver([NotNull] string weaverName, [NotNull] string configuration, [CanBeNull] Project project)
        {
            WeaverName = weaverName;
            Configuration = configuration;
            Project = project;
        }

        [NotNull]
        public string WeaverName { get; }

        [NotNull]
        public string Configuration { get; }

        [CanBeNull]
        public Project Project { get; }

        [NotNull, ItemNotNull]
        public static IEnumerable<FodyWeaver> EnumerateWeavers([NotNull] Solution solution)
        {
            var solutionFolder = solution.SolutionFolder;
            if (string.IsNullOrEmpty(solutionFolder))
                return Enumerable.Empty<FodyWeaver>();

            var solutionWeavers = EnumerateWeavers(solutionFolder, null);

            var projectWeavers = solution.Projects.SelectMany(project => EnumerateWeavers(project.Folder, project));

            return solutionWeavers.Concat(projectWeavers);
        }

        [NotNull, ItemNotNull]
        private static IEnumerable<FodyWeaver> EnumerateWeavers([NotNull] string folder, [CanBeNull] Project project)
        {
            var root = GetDocument(Path.Combine(folder, "FodyWeavers.xml"))?.Root;

            if (root == null)
                yield break;

            foreach (var element in root.Elements())
            {
                var weaverName = element.Name.LocalName;
                var configuration = element.ToString(SaveOptions.OmitDuplicateNamespaces);

                yield return new FodyWeaver(weaverName, configuration, project);
            }
        }

        [CanBeNull]
        private static XDocument GetDocument([NotNull] string file)
        {
            try
            {
                return File.Exists(file) ? XDocument.Load(file) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public sealed class FodyWeaverConfiguration
    {
        public FodyWeaverConfiguration([NotNull] string name, [NotNull] ICollection<FodyWeaver> weavers)
        {
            var solutionConfigurations = weavers
                .Where(w => w.Project == null)
                .Take(1)
                .Select(w => w.Configuration)
                .DefaultIfEmpty();

            var projectConfigurations = weavers
                .Where(w => w.Project != null)
                .Select(w => w.Configuration)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            Configurations = solutionConfigurations
                .Concat(projectConfigurations)
                .ToArray();

            Name = name;
            Weavers = weavers;

            Configuration = new ConfigurationIndexer(Configurations);
        }

        [NotNull]
        public string Name { get; }

        [NotNull]
        public ICollection<FodyWeaver> Weavers { get; }

        [NotNull]
        public IIndexer<string> Configuration { get; }

        [NotNull]
        public IList<string> Configurations { get; }

        private sealed class ConfigurationIndexer : IIndexer<string>
        {
            [NotNull]
            private readonly IList<string> _configurations;

            public ConfigurationIndexer([NotNull] IList<string> configurations)
            {
                _configurations = configurations;
            }

            public string this[string key]
            {
                get
                {
                    if (!int.TryParse(key, out var index))
                        return string.Empty;

                    if (index < 0 || index >= _configurations.Count)
                        return string.Empty;

                    return _configurations[index];
                }
                set
                {
                }
            }
        }
    }
}