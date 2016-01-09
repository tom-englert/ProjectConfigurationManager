namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Xml.Linq;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;

    public class Project : ObservableObject, IEquatable<Project>
    {
        private static readonly XNamespace _xmlns = XNamespace.Get(@"http://schemas.microsoft.com/developer/msbuild/2003");
        private static readonly XName _propertyGroupName = _xmlns.GetName("PropertyGroup");

        // private static readonly Dictionary<string, Microsoft.Build.Evaluation.Project> _buildProjects = new Dictionary<string, Microsoft.Build.Evaluation.Project>();

        private readonly Solution _solution;
        private readonly EnvDTE.Project _project;
        private readonly ObservableCollection<ProjectConfiguration> _internalSpecificProjectConfigurations = new ObservableCollection<ProjectConfiguration>();
        private readonly ReadOnlyObservableCollection<ProjectConfiguration> _specificProjectConfigurations;
        private readonly IObservableCollection<SolutionContext> _solutionContexts;
        private readonly string _uniqueName;
        private readonly string _name;
        private readonly XDocument _document;
        private readonly IList<string> _propertyNames;
        private readonly IList<XElement> _propertyGroupNodes;

        internal Project(Solution solution, EnvDTE.Project project)
        {
            _solution = solution;
            _project = project;
            _uniqueName = _project.UniqueName;
            _name = _project.Name;

            _specificProjectConfigurations = new ReadOnlyObservableCollection<ProjectConfiguration>(_internalSpecificProjectConfigurations);
            _solutionContexts = _solution.SolutionContexts.ObservableWhere(ctx => ctx.ProjectName == _uniqueName);

            try
            {
                // Can't use msbuild or vs interfaces here, they are much too slow: parse XML directly....
                _document = XDocument.Load(_project.FullName, LoadOptions.PreserveWhitespace);

                _propertyGroupNodes = _document
                    .Descendants(_propertyGroupName)
                    .ToArray();

                _propertyNames = _propertyGroupNodes
                    .SelectMany(group => group.Elements())
                    .Where(node => node.GetAttribute("Condition") == null)
                    .Select(node => node.Name.LocalName)
                    .ToArray();
            }
            catch
            {
            }

            Update();
        }

        public Solution Solution => _solution;

        public string Name => _name;

        public string UniqueName => _uniqueName;

        public IEnumerable<string> PropertyNames => _propertyNames;

        public IObservableCollection<SolutionContext> SolutionContexts => _solutionContexts;

        public ReadOnlyObservableCollection<ProjectConfiguration> SpecificProjectConfigurations => _specificProjectConfigurations;

        public ProjectConfiguration DefaultProjectConfiguration => new ProjectConfiguration(this, _propertyGroupNodes.Where(node => node.GetAttribute("Condition") == null), null, null);

        private static bool MatchesCondition(XElement node, string configuration, string platform)
        {
            var conditionExpression = node.GetAttribute("Condition");
            var condition = string.Join("|", configuration, platform.Replace(" ", ""));

            var match = !string.IsNullOrEmpty(conditionExpression)
                && conditionExpression.Contains("'$(Configuration)|$(Platform)'")
                && conditionExpression.Contains(condition);

            return match;
        }

        internal void Update()
        {
            var configurationManager = _project.ConfigurationManager;

            var projectConfigurations = Enumerable.Empty<ProjectConfiguration>();

            if (configurationManager != null)
            {
                var configurationNames = ((IEnumerable)configurationManager.ConfigurationRowNames)?.OfType<string>();
                var platformNames = ((IEnumerable)configurationManager.PlatformNames)?.OfType<string>();

                if ((configurationNames != null) && (platformNames != null))
                {
                    projectConfigurations = configurationNames
                        .SelectMany(configuration => platformNames.Select(platform => new ProjectConfiguration(this, _propertyGroupNodes.Where(node => MatchesCondition(node, configuration, platform)), configuration, platform)));
                }
            }

            _internalSpecificProjectConfigurations.SynchronizeWith(projectConfigurations.ToArray());
        }

        #region IEquatable implementation

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return _project.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Project);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Project"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="Project"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="Project"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(Project other)
        {
            return InternalEquals(this, other);
        }

        private static bool InternalEquals(Project left, Project right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null))
                return false;
            if (ReferenceEquals(right, null))
                return false;

            return left._project == right._project;
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(Project left, Project right)
        {
            return InternalEquals(left, right);
        }
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(Project left, Project right)
        {
            return !InternalEquals(left, right);
        }

        #endregion

        public override string ToString()
        {
            return Name;
        }
    }
}
