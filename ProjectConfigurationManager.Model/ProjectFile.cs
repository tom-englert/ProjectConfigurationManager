namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Windows.Threading;
    using System.Xml;
    using System.Xml.Linq;

    using Microsoft.VisualStudio.Shell.Interop;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    class ProjectFile
    {
        private const string ConditionAttributeName = "Condition";

        private static readonly XNamespace _xmlns = XNamespace.Get(@"http://schemas.microsoft.com/developer/msbuild/2003");
        private static readonly XName _propertyGroupNodeName = _xmlns.GetName("PropertyGroup");

        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        private readonly DispatcherThrottle _deferredSaveThrottle;
        private readonly Solution _solution;
        private readonly Project _project;
        private readonly Guid _projectGuid;

        private XDocument _document;
        private ProjectPropertyGroup[] _propertyGroups;

        public ProjectFile(Solution solution, Project project)
        {
            Contract.Requires(solution != null);
            Contract.Requires(project != null);

            _deferredSaveThrottle = new DispatcherThrottle(SaveProjectFile);
            _solution = solution;
            _project = project;

            FileTime = File.GetLastWriteTime(project.FullName);

            _projectGuid = GetProjectGuid(solution, project.UniqueName);
        }

        public IEnumerable<IProjectPropertyGroup> GetPropertyGroups(string configuration, string platform)
        {
            Contract.Ensures(Contract.Result<IEnumerable<IProjectPropertyGroup>>() != null);
            return PropertyGroups.Where(group => group.MatchesConfiguration(configuration, platform));
        }

        public IProjectProperty CreateProperty(string propertyName, string configuration, string platform)
        {
            Contract.Requires(propertyName != null);

            var group = GetPropertyGroups(configuration, platform).FirstOrDefault();

            return group?.AddProperty(propertyName);
        }

        public void DeleteProperty(string propertyName, string configuration, string platform)
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

        internal bool HasConfiguration(string configuration, string platform)
        {
            return PropertyGroups.Any(group => group.MatchesConfiguration(configuration, platform));
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

            _dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
            {
                IsSaving = false;
                FileTime = File.GetLastWriteTime(_project.FullName);
            });

            var projectGuid = _projectGuid;
            var solution = _solution.GetService(typeof(SVsSolution)) as IVsSolution4;

            Contract.Assume(solution != null);

            if (!_project.IsSaved)
                return;

            solution.UnloadProject(ref projectGuid, (int)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser);

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  "
            };

            using (var writer = XmlWriter.Create(_project.FullName, settings))
            {
                Document.WriteTo(writer);
            }

            solution.ReloadProject(ref projectGuid);
        }

        private static Guid GetProjectGuid(IServiceProvider serviceProvider, string uniqueName)
        {
            Contract.Requires(serviceProvider != null);
            Contract.Requires(uniqueName != null);

            var solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            Contract.Assume(solution != null);

            IVsHierarchy projectHierarchy;
            solution.GetProjectOfUniqueName(uniqueName, out projectHierarchy);
            Contract.Assume(projectHierarchy != null);

            Guid projectGuid;
            solution.GetGuidOfProject(projectHierarchy, out projectGuid);
            return projectGuid;
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

        private XDocument Document
        {
            get
            {
                Contract.Ensures(Contract.Result<XDocument>() != null);
                return _document ?? (_document = XDocument.Load(_project.FullName, LoadOptions.PreserveWhitespace));
            }
        }

        private IEnumerable<ProjectPropertyGroup> PropertyGroups
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<ProjectPropertyGroup>>() != null);
                return _propertyGroups ?? (_propertyGroups = GeneratePropertyGroups());
            }
        }

        private ProjectPropertyGroup[] GeneratePropertyGroups()
        {
            return Document.Descendants(_propertyGroupNodeName)
                .Where(node => node.Parent?.Name.LocalName == "Project")
                .Select(node => new ProjectPropertyGroup(this, node))
                .ToArray();
        }

        private class ProjectPropertyGroup : IProjectPropertyGroup
        {
            private readonly ProjectFile _projectFile;
            private readonly XElement _propertyGroupNode;
            private readonly ObservableCollection<ProjectProperty> _properties;

            public ProjectPropertyGroup(ProjectFile projectFile, XElement propertyGroupNode)
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

            public bool MatchesConfiguration(string configuration, string platform)
            {
                var conditionExpression = _propertyGroupNode.GetAttribute(ConditionAttributeName);

                if (string.IsNullOrEmpty(conditionExpression))
                    return string.IsNullOrEmpty(configuration) && string.IsNullOrEmpty(platform);

                if (string.IsNullOrEmpty(configuration))
                    return false;

                conditionExpression = conditionExpression.Replace(" ", "");

                if (!conditionExpression.Contains("$(Configuration)") || !conditionExpression.Contains("=="))
                    return false;

                if (!conditionExpression.Contains(configuration))
                    return false;

                if (!conditionExpression.Contains("$(Platform)"))
                    return platform == "Any CPU";

                return conditionExpression.Contains(platform.Replace(" ", ""));
            }

            public void Delete()
            {
                _propertyGroupNode.RemoveSelfAndWhitespace();
            }

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_propertyGroupNode != null);
                Contract.Invariant(_properties != null);
            }
        }

        private class ProjectProperty : IProjectProperty
        {
            private readonly ProjectFile _projectFile;
            private readonly XElement _node;

            public ProjectProperty(ProjectFile projectFile, XElement node)
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
                    return _node.Value;
                }
                set
                {
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
            private void ObjectInvariant()
            {
                Contract.Invariant(_projectFile != null);
                Contract.Invariant(_node != null);
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_project != null);
        }
    }
}
