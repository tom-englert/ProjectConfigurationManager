namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows.Threading;
    using System.Xml;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using Microsoft.VisualStudio.Shell.Interop;

    using Throttle;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    internal sealed class ProjectFile
    {
        private const string ConditionAttributeName = "Condition";

        [NotNull]
        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        [NotNull]
        private readonly Solution _solution;
        [NotNull]
        private readonly Project _project;
        private readonly Guid _projectGuid;

        [CanBeNull]
        private XDocument _document;
        [CanBeNull, ItemNotNull]
        private IList<IPropertyGroup> _propertyGroups;

        [NotNull]
        private XName PropertyGroupNodeName => DefaultNamespace.GetName("PropertyGroup");
        [NotNull]
        private XName ItemDefinitionGroupNodeName => DefaultNamespace.GetName("ItemDefinitionGroup");

        public ProjectFile([NotNull] Solution solution, [NotNull] Project project)
        {
            _solution = solution;
            _project = project;

            FileTime = File.GetLastWriteTime(project.FullName);

            _projectGuid = solution.GetProjectGuid(project.ProjectHierarchy);
        }

        [NotNull, ItemNotNull]
        public IEnumerable<IPropertyGroup> GetPropertyGroups([CanBeNull] string configuration, [CanBeNull] string platform)
        {
            return PropertyGroups
                .Where(group => group.MatchesConfiguration(configuration, platform));
        }

        [CanBeNull]
        public IProjectProperty CreateProperty([NotNull] string propertyName, [CanBeNull] string configuration, [CanBeNull] string platform)
        {
            var parts = propertyName.Split('.');

            if (parts.Length == 2)
            {
                var itemGroupName = parts[0];
                propertyName = parts[1];

                var group = GetPropertyGroups(configuration, platform)
                    .OfType<IItemDefinitionGroup>()
                    .FirstOrDefault();

                // ReSharper disable AssignNullToNotNullAttribute
                group = group ?? CreateNewPropertyGroup(configuration, platform, ItemDefinitionGroupNodeName, element => new ItemDefinitionGroup(this, element));

                return group?.AddProperty(itemGroupName, propertyName);
                // ReSharper enable AssignNullToNotNullAttribute
            }
            else
            {
                var group = GetPropertyGroups(configuration, platform)
                    .OfType<IProjectPropertyGroup>()
                    .FirstOrDefault();

                group = group ?? CreateNewPropertyGroup(configuration, platform, PropertyGroupNodeName, element => new ProjectPropertyGroup(this, element));

                return group?.AddProperty(propertyName);
            }
        }

        [CanBeNull]
        private T CreateNewPropertyGroup<T>([CanBeNull] string configuration, [CanBeNull] string platform, [NotNull] XName nodeName, [NotNull] Func<XElement, T> groupFactory)
            where T : class, IPropertyGroup
        {
            var propertyGroups = _propertyGroups;

            if (propertyGroups == null)
                return null;

            var lastGroupNode = Root
                .Elements(nodeName)
                .LastOrDefault();

            if (lastGroupNode == null)
                return null;

            var newGroupNode = new XElement(nodeName, new XText("\n  "));

            if (!string.IsNullOrEmpty(configuration) && !string.IsNullOrEmpty(platform))
            {
                var conditionExpression = string.Format(CultureInfo.InvariantCulture, "'$(Configuration)|$(Platform)'=='{0}|{1}'", configuration, platform.Replace(" ", ""));
                newGroupNode.Add(new XAttribute(ConditionAttributeName, conditionExpression));
            }

            lastGroupNode.AddAfterSelf(newGroupNode);
            newGroupNode.AddBeforeSelf(new XText("\n  "));

            var group = groupFactory(newGroupNode);

            propertyGroups.Add(group);

            return group;
        }

        public void DeleteProperty([NotNull] string propertyName, [CanBeNull] string configuration, [CanBeNull] string platform)
        {
            var item = GetPropertyGroups(configuration, platform)
                .SelectMany(group => group.Properties)
                .FirstOrDefault(property => property?.Name == propertyName);

            item?.Delete();
        }

        public bool IsSaving { get; private set; }

        public DateTime FileTime { get; private set; }

        internal bool CanEdit()
        {
            return CanCheckout() && IsWritable;
        }

        private bool CanCheckout()
        {
            var service = (IVsQueryEditQuerySave2)_solution.GetService(typeof(SVsQueryEditQuerySave));
            if (service == null)
                return true;

            var files = new[] { _project.FullName };

            return (0 == service.QueryEditFiles(0, files.Length, files, null, null, out var editVerdict, out var _))
                && (editVerdict == (uint)tagVSQueryEditResult.QER_EditOK);
        }

        internal void DeleteConfiguration([CanBeNull] string configuration, [CanBeNull] string platform)
        {
            var groupsToDelete = PropertyGroups
                .Where(group => group.MatchesConfiguration(configuration, platform))
                .ToArray();

            _propertyGroups?.RemoveRange(groupsToDelete);

            groupsToDelete.ForEach(group => group?.Delete());

            SaveChanges();
        }

        [Throttled(typeof(DispatcherThrottle))]
        private void SaveChanges()
        {
            IsSaving = true;

            var outputFileName = _project.FullName;

            _dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
            {
                IsSaving = false;
                FileTime = File.GetLastWriteTime(outputFileName);
            });

            var projectGuid = _projectGuid;
            var solution = _solution.GetService(typeof(SVsSolution)) as IVsSolution4;

            if (!_project.IsSaved)
                return;

            var reloadProject = false;

            if (_project.IsLoaded)
            {
                reloadProject = true;
                solution?.UnloadProject(ref projectGuid, (int)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser);
            }

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
            };

            var backup = File.ReadAllBytes(outputFileName);

            using (var writer = XmlWriter.Create(outputFileName, settings))
            {
                Document.WriteTo(writer);
            }

            if (!reloadProject)
                return;

            var result = solution?.ReloadProject(ref projectGuid);

            if (result == 0)
                return;

            _solution.Tracer.TraceError("Loading project {0} failed after saving - reverting changes.", outputFileName);
            File.WriteAllBytes(outputFileName, backup);

            ReloadProject(_dispatcher, solution, 0, projectGuid);
        }

        private static void ReloadProject([NotNull] Dispatcher dispatcher, [NotNull] IVsSolution4 solution, int retry, Guid projectGuid)
        {
            var hr = solution.ReloadProject(ref projectGuid);
            if (hr == 0)
                return;

            if (retry < 3)
            {
                dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () => ReloadProject(dispatcher, solution, retry + 1, projectGuid));
            }
        }

        private bool IsWritable
        {
            get
            {
                try
                {
                    if ((File.GetAttributes(_project.FullName) & (FileAttributes.ReadOnly | FileAttributes.System)) != 0)
                        return false;

                    using (File.Open(_project.FullName, FileMode.Open, FileAccess.Write))
                    {
                        return true;
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }

                return false;
            }
        }

        [NotNull]
        private XDocument Document => _document ?? (_document = XDocument.Load(_project.FullName, LoadOptions.PreserveWhitespace));

        [NotNull]
        // ReSharper disable once AssignNullToNotNullAttribute
        private XElement Root => Document.Root;

        [NotNull]
        private XNamespace DefaultNamespace => Root.GetDefaultNamespace();

        [NotNull, ItemNotNull]
        internal IEnumerable<IPropertyGroup> PropertyGroups => _propertyGroups ?? (_propertyGroups = GeneratePropertyGroups());

        [NotNull, ItemNotNull]
        private IList<IPropertyGroup> GeneratePropertyGroups()
        {
            var projectPropertyGroups = Root.Elements(PropertyGroupNodeName)
                .Select(node => new ProjectPropertyGroup(this, node))
                .Cast<IPropertyGroup>();

            var itemDefinitionGroups = Root.Elements(ItemDefinitionGroupNodeName)
                .Select(definitionNode => new ItemDefinitionGroup(this, definitionNode))
                .Cast<IPropertyGroup>();

            return projectPropertyGroups
                .Concat(itemDefinitionGroups)
                .ToList();
        }

        private abstract class PropertyGroup : IPropertyGroup
        {
            protected PropertyGroup([NotNull] ProjectFile projectFile, [NotNull] XElement node)
            {
                ProjectFile = projectFile;
                Node = node;

                Properties = new ReadOnlyObservableCollection<IProjectProperty>(Items);
            }

            [NotNull]
            protected ProjectFile ProjectFile { get; }
            [NotNull]
            protected XElement Node { get; }
            [NotNull]
            protected XNamespace Xmlns => Node.Document?.Root?.GetDefaultNamespace() ?? XNamespace.None;

            public IEnumerable<IProjectProperty> Properties { get; }

            public string Label => Node.Attribute("Label")?.Value;

            [NotNull]
            protected ObservableCollection<IProjectProperty> Items { get; } = new ObservableCollection<IProjectProperty>();

            public string ConditionExpression => Node.GetAttribute(ConditionAttributeName);

            public void Delete()
            {
                Node.RemoveSelfAndWhitespace();
            }
        }

        private sealed class ProjectPropertyGroup : PropertyGroup, IProjectPropertyGroup
        {
            public ProjectPropertyGroup([NotNull] ProjectFile projectFile, [NotNull] XElement node)
                : base(projectFile, node)
            {
                Items.AddRange(EnumerateProperties(projectFile, node));
            }

            [NotNull]
            private IEnumerable<ProjectProperty> EnumerateProperties([NotNull] ProjectFile projectFile, [NotNull] XElement groupNode)
            {
                return groupNode.Elements()
                    .Where(node => node != null && node.GetAttribute(ConditionAttributeName) == null)
                    .Select(propertyNode => new ProjectProperty(projectFile, this, propertyNode, propertyNode.Name.LocalName));
            }

            public IProjectProperty AddProperty(string propertyName)
            {
                var propertyNode = new XElement(Xmlns.GetName(propertyName));

                Node.Add(propertyNode);

                var property = new ProjectProperty(ProjectFile, this, propertyNode, propertyNode.Name.LocalName);

                Items.Add(property);

                return property;
            }
        }

        private sealed class ItemDefinitionGroup : PropertyGroup, IItemDefinitionGroup
        {
            public ItemDefinitionGroup([NotNull] ProjectFile projectFile, [NotNull] XElement node)
                : base(projectFile, node)
            {
                Items.AddRange(EnumerateProperties(projectFile, node));
            }

            [NotNull]
            private IEnumerable<ProjectProperty> EnumerateProperties([NotNull] ProjectFile projectFile, [NotNull] XElement groupNode)
            {
                // ReSharper disable PossibleNullReferenceException
                return groupNode.Elements()
                    .SelectMany(propertyGroupNode => propertyGroupNode
                        .Elements()
                        .Select(propertyNode => new ProjectProperty(projectFile, this, propertyNode, propertyGroupNode.Name.LocalName + "." + propertyNode.Name.LocalName))
                    );
                // ReSharper restore PossibleNullReferenceException
            }

            public IProjectProperty AddProperty(string itemGroupName, string propertyName)
            {
                var propertyNode = new XElement(Xmlns.GetName(propertyName));

                var groupNode = Node.Elements().FirstOrDefault(element => element?.Name.LocalName == itemGroupName) ?? Node.AddElement(new XElement(Xmlns.GetName(itemGroupName)));

                groupNode.AddElement(propertyNode);

                var property = new ProjectProperty(ProjectFile, this, propertyNode, groupNode.Name.LocalName + "." + propertyNode.Name.LocalName);

                Items.Add(property);

                return property;
            }

        }

        private sealed class ProjectProperty : IProjectProperty
        {
            [NotNull]
            private readonly ProjectFile _projectFile;
            [NotNull]
            private readonly XElement _node;

            public ProjectProperty([NotNull] ProjectFile projectFile, [NotNull] IPropertyGroup group, [NotNull] XElement node, [NotNull] string name)
            {
                _projectFile = projectFile;
                _node = node;

                Group = group;
                Name = name;
            }

            public string Name { get; }

            public IPropertyGroup Group { get; }

            public string Value
            {
                get
                {
                    var value = _node.Value;
                    return string.IsNullOrWhiteSpace(value) ? string.Empty : value;
                }
                set
                {
                    if (string.Equals(_node.Value, value))
                        return;

                    _node.Value = value;
                    _projectFile.SaveChanges();
                }
            }

            public void Delete()
            {
                _node.RemoveSelfAndWhitespace();
                _projectFile.SaveChanges();
            }
        }
    }
}
