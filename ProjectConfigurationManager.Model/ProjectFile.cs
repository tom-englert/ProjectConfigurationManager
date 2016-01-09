using System.Diagnostics.Contracts;
namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Xml.Linq;

    using TomsToolbox.Desktop;

    class ProjectFile
    {
        private static readonly XNamespace _xmlns = XNamespace.Get(@"http://schemas.microsoft.com/developer/msbuild/2003");
        private static readonly XName _propertyGroupNodeName = _xmlns.GetName("PropertyGroup");
        private const string _conditionAttributeName = "Condition";

        private readonly XDocument _document;
        private readonly EnvDTE.Project _project;
        private readonly ProjectPropertyGroup[] _propertyGroups;

        public ProjectFile(EnvDTE.Project project)
        {
            Contract.Requires(project != null);

            _project = project;

            try
            {
                // Can't use msbuild or vs interfaces here, they are much too slow: parse XML directly....
                _document = XDocument.Load(_project.FullName, LoadOptions.PreserveWhitespace);

                _propertyGroups = _document
                    .Descendants(_propertyGroupNodeName)
                    .Select(node => new ProjectPropertyGroup(this, node))
                    .ToArray();
            }
            catch
            {
            }
        }

        public IEnumerable<IProjectPropertyGroup> PropertyGroups => _propertyGroups;

        public IEnumerable<IProjectPropertyGroup> GetPropertyGroups(string configuration, string platform)
        {
            return _propertyGroups.Where(group => group.MatchesConfiguration(configuration, platform));
        }

        private void SaveChanges()
        {
            if (!_project.Saved)
                throw new InvalidOperationException("the project has local changes.");

            _document.Save(_project.FullName, SaveOptions.None);
        }

        private class ProjectPropertyGroup : IProjectPropertyGroup
        {
            private readonly XElement _propertyGroupNode;
            private readonly ProjectProperty[] _properties;

            public ProjectPropertyGroup(ProjectFile projectFile, XElement propertyGroupNode)
            {
                Contract.Requires(projectFile != null);
                Contract.Requires(propertyGroupNode != null);

                _propertyGroupNode = propertyGroupNode;

                _properties = _propertyGroupNode.Elements()
                    .Where(node => node.GetAttribute(_conditionAttributeName) == null)
                    .Select(node => new ProjectProperty(projectFile, node))
                    .ToArray();
            }

            public IEnumerable<IProjectProperty> Properties => _properties;

            public bool MatchesConfiguration(string configuration, string platform)
            {
                var conditionExpression = _propertyGroupNode.GetAttribute(_conditionAttributeName);

                if (string.IsNullOrEmpty(conditionExpression))
                    return string.IsNullOrEmpty(configuration) && string.IsNullOrEmpty(platform);

                if (string.IsNullOrEmpty(configuration) || string.IsNullOrEmpty(platform))
                    return false;

                var condition = string.Join("|", configuration, platform.Replace(" ", ""));

                return conditionExpression.Contains("$(Configuration)|$(Platform)")
                    && conditionExpression.Contains(condition);
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
