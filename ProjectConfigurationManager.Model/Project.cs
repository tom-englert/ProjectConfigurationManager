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

        private Project(Solution solution, EnvDTE.Project project)
        {
            Contract.Requires(solution != null);
            Contract.Requires(project != null);
            Contract.Requires(!string.IsNullOrEmpty(project.FullName));
            Contract.Requires(!string.IsNullOrEmpty(project.UniqueName));

            _solution = solution;
            _project = project;
            _uniqueName = _project.UniqueName;
            _name = _project.Name;
            _fullName = _project.FullName;

            _projectFile = new ProjectFile(solution, this);

            _defaultProjectConfiguration = new ProjectConfiguration(this, null, null);
            _specificProjectConfigurations = new ReadOnlyObservableCollection<ProjectConfiguration>(_internalSpecificProjectConfigurations);
            _solutionContexts = _solution.SolutionContexts.ObservableWhere(context => context.ProjectName == _uniqueName);

            Update();
        }

        internal static Project Create(Solution solution, EnvDTE.Project project, ITracer tracer)
        {
            try
            {
                Uri projectUri;

                // Skip web pojects, we can't edit them.
                if (Uri.TryCreate(project.FullName, UriKind.Absolute, out projectUri) && projectUri.IsFile)
                {
                    return new Project(solution, project);
                }
            }
            catch (Exception ex)
            {
                tracer.TraceError(ex);
            }

            return null;
        }

        public Solution Solution
        {
            get
            {
                Contract.Ensures(Contract.Result<Solution>() != null);
                return _solution;
            }
        }

        public string Name
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return _name;
            }
        }

        public string UniqueName
        {
            get
            {
                Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));
                return _uniqueName;
            }
        }

        public string RelativePath
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return Path.GetDirectoryName(UniqueName);
            }
        }

        public string SortKey
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return _name + " (" + RelativePath + ")";
            }
        }

        public string FullName
        {
            get
            {
                Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));
                return _fullName;
            }
        }

        public string[] ProjectTypeGuids
        {
            get
            {
                Contract.Ensures(Contract.Result<string[]>() != null);
                return RetrieveProjectTypeGuids();
            }
        }

        public IObservableCollection<SolutionContext> SolutionContexts
        {
            get
            {
                Contract.Ensures(Contract.Result<IObservableCollection<SolutionContext>>() != null);
                return _solutionContexts;
            }
        }

        public ReadOnlyObservableCollection<ProjectConfiguration> SpecificProjectConfigurations
        {
            get
            {
                Contract.Ensures(Contract.Result<ReadOnlyObservableCollection<ProjectConfiguration>>() != null);
                return _specificProjectConfigurations;
            }
        }

        public ProjectConfiguration DefaultProjectConfiguration
        {
            get
            {
                Contract.Ensures(Contract.Result<ProjectConfiguration>() != null);
                return _defaultProjectConfiguration;
            }
        }

        public bool IsSaving => _projectFile.IsSaving;

        public DateTime FileTime => _projectFile.FileTime;

        internal bool IsSaved
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

        public bool CanEdit()
        {
            return IsSaved && _projectFile.CanEdit();
        }

        public void Reload()
        {
            _projectFile = new ProjectFile(_solution, this);

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
                        .SelectMany(configuration => platformNames
                            .Where(platform => _projectFile.HasConfiguration(configuration, platform))
                            .Select(platform => new ProjectConfiguration(this, configuration, platform)));
                }
            }

            _internalSpecificProjectConfigurations.SynchronizeWith(projectConfigurations.ToArray());

            _defaultProjectConfiguration.SetProjectFile(_projectFile);

            _internalSpecificProjectConfigurations.ForEach(config => config.SetProjectFile(_projectFile));

        }

        internal IProjectProperty CreateProperty(string propertyName, string configuration, string platform)
        {
            Contract.Requires(propertyName != null);

            return _projectFile.CreateProperty(propertyName, configuration, platform);
        }

        internal void DeleteProperty(string propertyName, string configuration, string platform)
        {
            Contract.Requires(propertyName != null);

            _projectFile.DeleteProperty(propertyName, configuration, platform);
        }

        internal void Delete(ProjectConfiguration configuration)
        {
            Contract.Requires(configuration != null);

            if (_internalSpecificProjectConfigurations.Remove(configuration))
            {
                _projectFile.DeleteConfiguration(configuration.Configuration, configuration.Platform);
            }
        }

        private string[] RetrieveProjectTypeGuids()
        {
            Contract.Ensures(Contract.Result<string[]>() != null);

            return (_defaultProjectConfiguration.PropertyValue["ProjectTypeGuids"] ?? ProjectTypeGuid.Unspecified)
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
            return _name;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_name != null);
            Contract.Invariant(!string.IsNullOrEmpty(_fullName));
            Contract.Invariant(!string.IsNullOrEmpty(_uniqueName));
            Contract.Invariant(_solution != null);
            Contract.Invariant(_project != null);
            Contract.Invariant(_projectFile != null);
            Contract.Invariant(_defaultProjectConfiguration != null);
            Contract.Invariant(_solutionContexts != null);
        }
    }
}
