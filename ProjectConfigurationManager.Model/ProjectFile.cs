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

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;

    using TomsToolbox.Desktop;

    class ProjectFile
    {
        private static readonly XNamespace _xmlns = XNamespace.Get(@"http://schemas.microsoft.com/developer/msbuild/2003");
        private static readonly XName _propertyGroupNodeName = _xmlns.GetName("PropertyGroup");
        private const string _conditionAttributeName = "Condition";

        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        private readonly DispatcherThrottle _deferredSaveThrottle;
        private readonly XDocument _document;
        private readonly Solution _solution;
        private readonly EnvDTE.Project _project;
        private readonly ProjectPropertyGroup[] _propertyGroups;
        private readonly Guid _projectGuid;
        private readonly string _fullName;

        public ProjectFile(Solution solution, EnvDTE.Project project)
        {
            Contract.Requires(solution != null);
            Contract.Requires(project != null);

            _deferredSaveThrottle = new DispatcherThrottle(SaveProjectFile);
            _solution = solution;
            _project = project;
            _fullName = project.FullName;

            _projectGuid = GetProjectGuid(solution, project);

            try
            {
                // Can't use msbuild or vs interfaces here, they are much too slow: parse XML directly....
                _document = XDocument.Load(_fullName, LoadOptions.None);

                _propertyGroups = _document
                    .Descendants(_propertyGroupNodeName)
                    .Where(node => node.Parent?.Name.LocalName == "Project")
                    .Select(node => new ProjectPropertyGroup(this, node))
                    .ToArray();
            }
            catch
            {
            }
        }

        public IEnumerable<IProjectPropertyGroup> GetPropertyGroups(string configuration, string platform)
        {
            return _propertyGroups.Where(group => group.MatchesConfiguration(configuration, platform));
        }

        public IProjectProperty CreateProperty(string propertyName, string configuration, string platform)
        {
            var group = GetPropertyGroups(configuration, platform).FirstOrDefault();

            return group?.AddProperty(propertyName);
        }

        public bool IsSaving { get; private set; }

        private void SaveChanges()
        {
            _deferredSaveThrottle.Tick();
        }

        private void SaveProjectFile()
        {
            IsSaving = true;

            _dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () => IsSaving = false);

            var projectGuid = _projectGuid;
            var solution = _solution.GetService(typeof(SVsSolution)) as IVsSolution4;

            Contract.Assume(solution != null);

            if (!IsSaved)
                return;

            solution.UnloadProject(ref projectGuid, (int)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser);

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  "
            };

            using (var writer = XmlWriter.Create(_fullName, settings))
            {
                _document.WriteTo(writer);
            }

            solution.ReloadProject(ref projectGuid);
        }

        private bool IsSaved
        {
            get
            {
                try
                {
                    return _project.Saved;
                }
                catch
                {
                    // project is currently unloaded...
                    return true;
                }
            }
        }

        private static Guid GetProjectGuid(IServiceProvider serviceProvider, EnvDTE.Project project)
        {
            var solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            Contract.Assume(solution != null);

            IVsHierarchy projectHierarchy;
            ErrorHandler.ThrowOnFailure(solution.GetProjectOfUniqueName(project.UniqueName, out projectHierarchy));
            Contract.Assume(projectHierarchy != null);

            Guid projectGuid;
            ErrorHandler.ThrowOnFailure(solution.GetGuidOfProject(projectHierarchy, out projectGuid));
            return projectGuid;
        }

        public bool CanEdit()
        {
            if (!IsSaved)
                return false;

            var service = (IVsQueryEditQuerySave2)_solution.GetService(typeof(SVsQueryEditQuerySave));
            if (service != null)
            {
                var files = new[] { _fullName };
                uint editVerdict;
                uint moreInfo;

                if ((0 != service.QueryEditFiles(0, files.Length, files, null, null, out editVerdict, out moreInfo))
                    || (editVerdict != (uint)tagVSQueryEditResult.QER_EditOK))
                {
                    return false;
                }
            }

            return IsWritable;
        }

        private bool IsWritable
        {
            get
            {
                try
                {
                    if ((File.GetAttributes(_fullName) & (FileAttributes.ReadOnly | FileAttributes.System)) != 0)
                        return false;

                    using (File.Open(_fullName, FileMode.Open, FileAccess.Write))
                    {
                        return true;
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }

                return false;
            }
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
                        .Where(node => node.GetAttribute(_conditionAttributeName) == null)
                        .Select(node => new ProjectProperty(projectFile, node)));
            }

            public IEnumerable<IProjectProperty> Properties => _properties;

            public IProjectProperty AddProperty(string propertyName)
            {
                var node = new XElement(_xmlns.GetName(propertyName));

                _propertyGroupNode.Add(node);

                var property = new ProjectProperty(_projectFile, node);

                _properties.Add(property);

                return property;
            }

            public bool MatchesConfiguration(string configuration, string platform)
            {
                var conditionExpression = _propertyGroupNode.GetAttribute(_conditionAttributeName);

                if (string.IsNullOrEmpty(conditionExpression))
                    return string.IsNullOrEmpty(configuration) && string.IsNullOrEmpty(platform);

                if (string.IsNullOrEmpty(configuration) || string.IsNullOrEmpty(platform))
                    return false;

                conditionExpression = conditionExpression.Replace(" ", "");

                if (!conditionExpression.Contains("$(Configuration)|$(Platform)"))
                    return false;

                var condition = string.Join("|", configuration, platform).Replace(" ", "");

                return conditionExpression.Contains(condition);
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
            Contract.Invariant(_document != null);
            Contract.Invariant(_project != null);
            Contract.Invariant(_propertyGroups != null);
        }
    }
}
