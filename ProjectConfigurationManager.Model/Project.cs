namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;

    public class Project : ObservableObject, IEquatable<Project>
    {
        private readonly EnvDTE.Project _project;

        private readonly Solution _solution;
        private readonly string _uniqueName;
        private readonly string _name;
        private readonly string _fullName;

        private readonly ObservableCollection<ProjectConfiguration> _internalSpecificProjectConfigurations = new ObservableCollection<ProjectConfiguration>();
        private readonly ReadOnlyObservableCollection<ProjectConfiguration> _specificProjectConfigurations;
        private readonly IObservableCollection<SolutionContext> _solutionContexts;
        private readonly ProjectConfiguration _defaultProjectConfiguration;

        private ProjectFile _projectFile;

        internal Project(Solution solution, EnvDTE.Project project)
        {
            Contract.Requires(solution != null);
            Contract.Requires(project != null);

            _solution = solution;
            _project = project;
            _uniqueName = _project.UniqueName;
            _name = _project.Name;
            _fullName = _project.FullName;

            _projectFile = new ProjectFile(solution, project);

            _defaultProjectConfiguration = new ProjectConfiguration(this, null, null);
            _specificProjectConfigurations = new ReadOnlyObservableCollection<ProjectConfiguration>(_internalSpecificProjectConfigurations);
            _solutionContexts = _solution.SolutionContexts.ObservableWhere(context => context.ProjectName == _uniqueName);

            Update();
        }

        public Solution Solution => _solution;

        public string Name => _name;

        public string UniqueName => _uniqueName;

        public string RelativePath => Path.GetDirectoryName(UniqueName);

        public string SortKey => _name + " (" + RelativePath + ")";

        public string FullName => _fullName;

        public string[] ProjectTypeGuids => GetProjectTypeGuids();

        public IObservableCollection<SolutionContext> SolutionContexts => _solutionContexts;

        public ReadOnlyObservableCollection<ProjectConfiguration> SpecificProjectConfigurations => _specificProjectConfigurations;

        public ProjectConfiguration DefaultProjectConfiguration => _defaultProjectConfiguration;

        public bool IsSaving => _projectFile.IsSaving;

        public bool CanEdit()
        {
            return _projectFile.CanEdit();
        }

        public void Reload()
        {
            _projectFile = new ProjectFile(_solution, _project);

            Update();
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
                        .SelectMany(configuration => platformNames.Select(platform => new ProjectConfiguration(this, configuration, platform)));
                }
            }

            _internalSpecificProjectConfigurations.SynchronizeWith(projectConfigurations.ToArray());

            _defaultProjectConfiguration.SetProjectFile(_projectFile);

            foreach (var config in _internalSpecificProjectConfigurations)
            {
                config.SetProjectFile(_projectFile);
            }
        }

        internal IProjectProperty CreateProperty(string propertyName, string configuration, string platform)
        {
            return _projectFile.CreateProperty(propertyName, configuration, platform);
        }

        private string[] GetProjectTypeGuids()
        {
            return (_defaultProjectConfiguration.PropertyValue["ProjectTypeGuids"] ?? ProjectTypeGuid.Other)
                .Split(';')
                .Select(item => item.Trim())
                .ToArray();
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

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_solution != null);
            Contract.Invariant(_project != null);
            Contract.Invariant(_projectFile != null);
        }
    }
}
