namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;

    public class Project : ObservableObject, IEquatable<Project>
    {
        private const string ProjectTypeGuidsPropertyKey = "ProjectTypeGuids";
        private readonly EnvDTE.Project _project;
        private readonly VSLangProj.VSProject _vsProject;

        private readonly Solution _solution;
        private readonly string _uniqueName;
        private readonly string _name;
        private readonly string _fullName;

        private readonly ObservableCollection<ProjectConfiguration> _internalSpecificProjectConfigurations = new ObservableCollection<ProjectConfiguration>();
        private readonly ReadOnlyObservableCollection<ProjectConfiguration> _specificProjectConfigurations;
        private readonly IObservableCollection<SolutionContext> _solutionContexts;
        private readonly ProjectConfiguration _defaultProjectConfiguration;
        private readonly IObservableCollection<ProjectConfiguration> _projectConfigurations;
        private readonly IIndexer<bool> _isProjectTypeGuidSelected;
        private readonly ObservableCollection<Project> _referencedBy = new ObservableCollection<Project>();
        private readonly ObservableCollection<Project> _references = new ObservableCollection<Project>();

        private ProjectFile _projectFile;

        private Project(Solution solution, EnvDTE.Project project)
        {
            Contract.Requires(solution != null);
            Contract.Requires(project != null);
            Contract.Requires(!string.IsNullOrEmpty(project.FullName));
            Contract.Requires(!string.IsNullOrEmpty(project.UniqueName));

            _solution = solution;
            _project = project;
            _vsProject = project.Object as VSLangProj.VSProject;
            _uniqueName = _project.UniqueName;
            _name = _project.Name;
            _fullName = _project.FullName;

            _projectFile = new ProjectFile(solution, this);

            _defaultProjectConfiguration = new ProjectConfiguration(this, null, null);
            _specificProjectConfigurations = new ReadOnlyObservableCollection<ProjectConfiguration>(_internalSpecificProjectConfigurations);
            _solutionContexts = _solution.SolutionContexts.ObservableWhere(context => context.ProjectName == _uniqueName);
            _projectConfigurations = ObservableCompositeCollection.FromSingleItemAndList(_defaultProjectConfiguration, _internalSpecificProjectConfigurations);
            _isProjectTypeGuidSelected = new ProjectTypeGuidIndexer(_defaultProjectConfiguration);

            Update();
        }

        internal static Project Create(Solution solution, EnvDTE.Project project, bool retryOnErrors, ITracer tracer)
        {
            if (project == null)
                return null;

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
                if (retryOnErrors && (ex.GetType() == typeof(IOException)))
                    throw new RetryException(ex);

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

        public IObservableCollection<ProjectConfiguration> ProjectConfigurations
        {
            get
            {
                Contract.Ensures(Contract.Result<IObservableCollection<ProjectConfiguration>>() != null);
                return _projectConfigurations;
            }
        }

        public IIndexer<bool> IsProjectTypeGuidSelected => _isProjectTypeGuidSelected;

        public bool IsSaving => _projectFile.IsSaving;

        public DateTime FileTime => _projectFile.FileTime;

        public ObservableCollection<Project> References
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<Project>>() != null);

                return _references;
            }
        }

        public ObservableCollection<Project> ReferencedBy
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<Project>>() != null);

                return _referencedBy;
            }
        }

        private IEnumerable<VSLangProj.Reference> GetReferences()
        {
            Contract.Ensures(Contract.Result<IEnumerable<VSLangProj.Reference>>() != null);

            return VsProjectReferences ?? MpfProjectReferences;
        }

        private IEnumerable<VSLangProj.Reference> MpfProjectReferences
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<VSLangProj.Reference>>() != null);
                try
                {
                    var projectItems = _project.ProjectItems;
                    Contract.Assume(projectItems != null);

                    return projectItems
                        .Cast<EnvDTE.ProjectItem>()
                        .Select(p => p.Object)
                        .OfType<VSLangProj.References>()
                        .Take(1)
                        .SelectMany(references => references.Cast<VSLangProj.Reference>());
                }
                catch (ExternalException)
                {
                }

                return null;
            }
        }

        private IEnumerable<VSLangProj.Reference> VsProjectReferences
        {
            get
            {
                try
                {
                    return _vsProject?.References?.Cast<VSLangProj.Reference>();
                }
                catch (ExternalException)
                {
                    return Enumerable.Empty<VSLangProj.Reference>();
                }
            }
        }

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

        internal bool IsLoaded
        {
            get
            {
                try
                {
                    return _project.Saved || _project.IsDirty;
                }
                catch
                {
                    // project is currently unloaded...
                    return false;
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

        internal void UpdateReferences()
        {
            var projectReferences = GetReferences()
                .Where(reference => reference.GetSourceProject() != null)
                .Where(reference => reference.CopyLocal)
                .Select(reference => _solution.Projects.SingleOrDefault(p => string.Equals(p.UniqueName, reference.SourceProject.UniqueName, StringComparison.OrdinalIgnoreCase)))
                .Where(project => project != null)
                .ToArray();

            _references.SynchronizeWith(projectReferences);
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

            return (_defaultProjectConfiguration.PropertyValue[ProjectTypeGuidsPropertyKey] ?? ProjectTypeGuid.Unspecified)
                .Split(';')
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrEmpty(item))
                .ToArray();
        }

        private class ProjectTypeGuidIndexer : IIndexer<bool>
        {
            private readonly ProjectConfiguration _configuration;

            public ProjectTypeGuidIndexer(ProjectConfiguration configuration)
            {
                Contract.Requires(configuration != null);

                _configuration = configuration;
            }

            public bool this[string projectTypeGuid]
            {
                get
                {
                    return ProjectTypeGuids.Contains(projectTypeGuid, StringComparer.OrdinalIgnoreCase);
                }
                set
                {
                    ProjectTypeGuids = value
                        ? new[] { projectTypeGuid }.Concat(ProjectTypeGuids).Distinct(StringComparer.OrdinalIgnoreCase)
                        : ProjectTypeGuids.Where(item => !item.Equals(projectTypeGuid, StringComparison.OrdinalIgnoreCase));
                }
            }

            private IEnumerable<string> ProjectTypeGuids
            {
                get
                {
                    return _configuration.PropertyValue[ProjectTypeGuidsPropertyKey]
                        ?.Split(';')
                        .Select(item => item.Trim())
                        .Where(item => !string.IsNullOrEmpty(item)) ?? Enumerable.Empty<string>();
                }
                set
                {
                    _configuration.PropertyValue[ProjectTypeGuidsPropertyKey] = string.Join(";", value);
                }
            }


            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_configuration != null);
            }
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
            Contract.Invariant(_internalSpecificProjectConfigurations != null);
            Contract.Invariant(_specificProjectConfigurations != null);
            Contract.Invariant(_projectConfigurations != null);
            Contract.Invariant(_referencedBy != null);
            Contract.Invariant(_references != null);
        }
    }
}
