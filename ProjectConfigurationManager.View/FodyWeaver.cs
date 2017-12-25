namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using tomenglertde.ProjectConfigurationManager.Model;

    public class FodyWeaver
    {
        public const string ConfigurationFileName = "FodyWeavers.xml";

        private FodyWeaver([NotNull] string weaverName, [NotNull] string configuration, [CanBeNull] Project project, int index)
        {
            WeaverName = weaverName;
            Configuration = configuration;
            Project = project;
            Index = index;
        }

        [NotNull]
        public string WeaverName { get; }

        [NotNull]
        public string Configuration { get; }

        [CanBeNull]
        public Project Project { get; }

        public int Index { get; }

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
            var root = LoadDocument(folder)?.Root;

            if (root == null)
                yield break;

            var index = 0;

            foreach (var element in root.Elements())
            {
                Debug.Assert(element != null, nameof(element) + " != null");

                var weaverName = element.Name.LocalName;
                var configuration = element.ToString(SaveOptions.OmitDuplicateNamespaces);

                yield return new FodyWeaver(weaverName, configuration, project, index++);
            }
        }

        [CanBeNull]
        public static XDocument LoadDocument([NotNull] string folder)
        {
            try
            {
                var file = Path.Combine(folder, ConfigurationFileName);

                return File.Exists(file) ? XDocument.Load(file) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        [CanBeNull]
        public static string SaveDocument([NotNull] string folder, [NotNull] XDocument document)
        {
            try
            {
                var file = Path.Combine(folder, ConfigurationFileName);

                document.Save(file, SaveOptions.OmitDuplicateNamespaces);

                return file;
            }
            catch (Exception)
            {
                return null;
                // TODO: log error...
            }
        }
    }
}