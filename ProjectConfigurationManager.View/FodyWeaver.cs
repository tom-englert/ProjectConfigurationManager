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
        public const string ConfigurationFileName = "FodyWeavers.xml";

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
            var root = GetDocument(Path.Combine(folder, ConfigurationFileName))?.Root;

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
}