namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Windows.Threading;
    using System.Xml;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using Microsoft.VisualStudio.Shell.Interop;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    internal class ProjectFile
    {
        private const string ConditionAttributeName = "Condition";

        [NotNull]
        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        [NotNull]
        private readonly DispatcherThrottle _deferredSaveThrottle;
        [NotNull]
        private readonly Solution _solution;
        [NotNull]
        private readonly Project _project;
        private readonly Guid _projectGuid;

        private XDocument _document;
        private IProjectPropertyGroup[] _propertyGroups;

        [NotNull]
        private XName _propertyGroupNodeName => DefaultNamespace.GetName("PropertyGroup");

        public ProjectFile([NotNull] Solution solution, [NotNull] Project project)
        {
            Contract.Requires(solution != null);
            Contract.Requires(project != null);

            _deferredSaveThrottle = new DispatcherThrottle(SaveProjectFile);
            _solution = solution;
            _project = project;

            FileTime = File.GetLastWriteTime(project.FullName);

            _projectGuid = solution.GetProjectGuid(project.ProjectHierarchy);
        }

        [NotNull]
        public IEnumerable<IProjectPropertyGroup> GetPropertyGroups(string configuration, string platform)
        {
            Contract.Ensures(Contract.Result<IEnumerable<IProjectPropertyGroup>>() != null);
            return PropertyGroups.Where(group => group.MatchesConfiguration(configuration, platform));
        }

        public IProjectProperty CreateProperty([NotNull] string propertyName, string configuration, string platform)
        {
            Contract.Requires(propertyName != null);

            var group = GetPropertyGroups(configuration, platform).FirstOrDefault();

            return group?.AddProperty(propertyName);
        }

        public void DeleteProperty([NotNull] string propertyName, string configuration, string platform)
        {
            Contract.Requires(propertyName != null);

            var item = GetPropertyGroups(configuration, platform)
                .SelectMany(group => group.Properties)
                .FirstOrDefault(property => property.Name == propertyName);

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
            uint editVerdict;
            uint moreInfo;

            return (0 == service.QueryEditFiles(0, files.Length, files, null, null, out editVerdict, out moreInfo))
                && (editVerdict == (uint)tagVSQueryEditResult.QER_EditOK);
        }

        internal void DeleteConfiguration(string configuration, string platform)
        {
            var groupsToDelete = PropertyGroups.Where(group => group.MatchesConfiguration(configuration, platform)).ToArray();

            _propertyGroups = PropertyGroups.Except(groupsToDelete).ToArray();

            groupsToDelete.ForEach(group => group.Delete());

            SaveChanges();
        }

        private void SaveChanges()
        {
            _deferredSaveThrottle.Tick();
        }

        private void SaveProjectFile()
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

            Contract.Assume(solution != null);

            if (!_project.IsSaved)
                return;

            var reloadProject = false;

            if (_project.IsLoaded)
            {
                reloadProject = true;
                solution.UnloadProject(ref projectGuid, (int)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser);
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

            var result = solution.ReloadProject(ref projectGuid);

            if (result == 0)
                return;

            _solution.Tracer.TraceError("Loading project {0} failed after saving - reverting changes.", outputFileName);
            File.WriteAllBytes(outputFileName, backup);

            ReloadProject(_dispatcher, solution, 0, projectGuid);
        }

        private static void ReloadProject([NotNull] Dispatcher dispatcher, [NotNull] IVsSolution4 solution, int retry, Guid projectGuid)
        {
            Contract.Requires(dispatcher != null);
            Contract.Requires(solution != null);

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
        private XDocument Document
        {
            get
            {
                Contract.Ensures(Contract.Result<XDocument>() != null);
                return _document ?? (_document = XDocument.Load(_project.FullName, LoadOptions.PreserveWhitespace));
            }
        }

        [NotNull]
        private XNamespace DefaultNamespace
        {
            get
            {
                Contract.Ensures(Contract.Result<XNamespace>() != null);
                return Document.Root?.GetDefaultNamespace() ?? XNamespace.None;
            }
        }

        [NotNull, ItemNotNull]
        internal IEnumerable<IProjectPropertyGroup> PropertyGroups
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<IProjectPropertyGroup>>() != null);
                return _propertyGroups ?? (_propertyGroups = GeneratePropertyGroups());
            }
        }

        [NotNull, ItemNotNull]
        private IProjectPropertyGroup[] GeneratePropertyGroups()
        {
            Contract.Ensures(Contract.Result<IProjectPropertyGroup[]>() != null);

            return Document.Descendants(_propertyGroupNodeName)
                .Where(node => node.Parent?.Name.LocalName == "Project")
                .Select(node => new ProjectPropertyGroup(this, node))
                .ToArray();
        }

        private class ProjectPropertyGroup : IProjectPropertyGroup
        {
            [NotNull]
            private readonly ProjectFile _projectFile;
            [NotNull]
            private readonly XElement _propertyGroupNode;
            [NotNull]
            private readonly ObservableCollection<ProjectProperty> _properties;
            [NotNull]
            private XNamespace _xmlns => _propertyGroupNode.Document?.Root?.GetDefaultNamespace() ?? XNamespace.None;

            public ProjectPropertyGroup([NotNull] ProjectFile projectFile, [NotNull] XElement propertyGroupNode)
            {
                Contract.Requires(projectFile != null);
                Contract.Requires(propertyGroupNode != null);

                _projectFile = projectFile;
                _propertyGroupNode = propertyGroupNode;

                _properties = new ObservableCollection<ProjectProperty>(
                    _propertyGroupNode.Elements()
                        .Where(node => node.GetAttribute(ConditionAttributeName) == null)
                        .Select(node => new ProjectProperty(projectFile, node)));
            }

            public IEnumerable<IProjectProperty> Properties => _properties;

            public IProjectProperty AddProperty(string propertyName)
            {
                var node = new XElement(_xmlns.GetName(propertyName));

                var lastNode = _propertyGroupNode.LastNode;

                if (lastNode?.NodeType == XmlNodeType.Text)
                {
                    var lastDelimiter = lastNode.PreviousNode?.PreviousNode as XText;
                    var whiteSpace = new XText(lastDelimiter?.Value ?? "\n    ");
                    lastNode.AddBeforeSelf(whiteSpace, node);
                }
                else
                {
                    _propertyGroupNode.Add(node);
                }

                var property = new ProjectProperty(_projectFile, node);

                _properties.Add(property);

                return property;
            }

            public string ConditionExpression => _propertyGroupNode.GetAttribute(ConditionAttributeName);

            public void Delete()
            {
                _propertyGroupNode.RemoveSelfAndWhitespace();
            }

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            [Conditional("CONTRACTS_FULL")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_projectFile != null);
                Contract.Invariant(_propertyGroupNode != null);
                Contract.Invariant(_properties != null);
            }
        }

        private class ProjectProperty : IProjectProperty
        {
            [NotNull]
            private readonly ProjectFile _projectFile;
            [NotNull]
            private readonly XElement _node;

            public ProjectProperty([NotNull] ProjectFile projectFile, [NotNull] XElement node)
            {
                Contract.Requires(projectFile != null);
                Contract.Requires(node != null);

                _projectFile = projectFile;
                _node = node;
            }

            public string Name => _node.Name.LocalName;

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

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            [Conditional("CONTRACTS_FULL")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_projectFile != null);
                Contract.Invariant(_node != null);
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_project != null);
            Contract.Invariant(_solution != null);
            Contract.Invariant(_dispatcher != null);
            Contract.Invariant(_deferredSaveThrottle != null);
        }
    }
}
