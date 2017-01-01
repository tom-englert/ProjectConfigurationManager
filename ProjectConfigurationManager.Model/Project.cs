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

    using JetBrains.Annotations;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;

    public class Project : ObservableObject, IEquatable<Project>
    {
        private const string ProjectTypeGuidsPropertyKey = "ProjectTypeGuids";

        [NotNull]
        private readonly Solution _solution;
        [NotNull]
        private readonly string _fullName;

        [NotNull]
        private readonly ObservableCollection<ProjectConfiguration> _internalSpecificProjectConfigurations = new ObservableCollection<ProjectConfiguration>();
        [NotNull]
        private readonly ReadOnlyObservableCollection<ProjectConfiguration> _specificProjectConfigurations;
        [NotNull]
        private readonly ProjectConfiguration _defaultProjectConfiguration;
        [NotNull]
        private readonly IObservableCollection<ProjectConfiguration> _projectConfigurations;
        private readonly IIndexer<bool> _isProjectTypeGuidSelected;
        [NotNull]
        private readonly ObservableCollection<Project> _referencedBy = new ObservableCollection<Project>();
        [NotNull]
        private readonly ObservableCollection<Project> _references = new ObservableCollection<Project>();

        [NotNull]
        private IVsHierarchy _projectHierarchy;
        [NotNull]
        private ProjectFile _projectFile;

        private Project([NotNull] Solution solution, [NotNull] string fullName, [NotNull] IVsHierarchy projectHierarchy)
        {
            Contract.Requires(solution != null);
            Contract.Requires(projectHierarchy != null);

            _solution = solution;
            _fullName = fullName;
            _projectHierarchy = projectHierarchy;

            _projectFile = new ProjectFile(solution, this);

            _defaultProjectConfiguration = new ProjectConfiguration(this, null, null);
            _specificProjectConfigurations = new ReadOnlyObservableCollection<ProjectConfiguration>(_internalSpecificProjectConfigurations);

            _projectConfigurations = ObservableCompositeCollection.FromSingleItemAndList(_defaultProjectConfiguration, _internalSpecificProjectConfigurations);
            _isProjectTypeGuidSelected = new ProjectTypeGuidIndexer(_defaultProjectConfiguration);

            Update();
        }

        internal static Project Create([NotNull] Solution solution, [NotNull] string fullName, [NotNull] IVsHierarchy projectHierarchy, bool retryOnErrors, ITracer tracer)
        {
            Contract.Requires(solution != null);
            Contract.Requires(projectHierarchy != null);

            try
            {
                return new Project(solution, fullName, projectHierarchy);
            }
            catch (Exception ex)
            {
                if (retryOnErrors && (ex.GetType() == typeof(IOException)))
                    throw new RetryException(ex);

                tracer.TraceError(ex);
            }

            return null;
        }

        [NotNull]
        public IVsHierarchy ProjectHierarchy
        {
            get
            {
                Contract.Ensures(Contract.Result<IVsHierarchy>() != null);
                return _projectHierarchy;
            }
        }

        [NotNull]
        public Solution Solution
        {
            get
            {
                Contract.Ensures(Contract.Result<Solution>() != null);
                return _solution;
            }
        }

        [NotNull]
        public string Name
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return DteProject?.Name ?? Path.GetFileNameWithoutExtension(_fullName);
            }
        }

        [NotNull]
        public string UniqueName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                var solutionFolder = _solution.SolutionFolder;
                if (string.IsNullOrEmpty(solutionFolder))
                    return _fullName;

                var solutionFolderUri = new Uri(solutionFolder + Path.DirectorySeparatorChar, UriKind.Absolute);
                var projectUri = new Uri(_fullName, UriKind.Absolute);

                var uniqueName = Uri.UnescapeDataString(solutionFolderUri
                        .MakeRelativeUri(projectUri)
                        .ToString())
                    .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

                Contract.Assume(DteProject == null || DteProject.UniqueName == uniqueName);

                return uniqueName;
            }
        }

        [NotNull]
        public string RelativePath
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return Path.GetDirectoryName(UniqueName);
            }
        }

        [NotNull]
        public string SortKey
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return Name + " (" + RelativePath + ")";
            }
        }

        [NotNull]
        public string FullName
        {
            get
            {
                Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));
                return _fullName;
            }
        }

        [NotNull]
        public IList<string> ProjectTypeGuids
        {
            get
            {
                Contract.Ensures(Contract.Result<IList<string>>() != null);
                return RetrieveProjectTypeGuids();
            }
        }

        [NotNull]
        public IEnumerable<SolutionContext> SolutionContexts
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<SolutionContext>>() != null);
                return _solution.SolutionConfigurations.SelectMany(cfg => cfg.Contexts).Where(context => context.ProjectName == UniqueName);
            }
        }

        [NotNull]
        public ReadOnlyObservableCollection<ProjectConfiguration> SpecificProjectConfigurations
        {
            get
            {
                Contract.Ensures(Contract.Result<ReadOnlyObservableCollection<ProjectConfiguration>>() != null);
                return _specificProjectConfigurations;
            }
        }

        [NotNull]
        public ProjectConfiguration DefaultProjectConfiguration
        {
            get
            {
                Contract.Ensures(Contract.Result<ProjectConfiguration>() != null);
                return _defaultProjectConfiguration;
            }
        }

        [NotNull]
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

        [NotNull]
        public ObservableCollection<Project> References
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<Project>>() != null);

                return _references;
            }
        }

        [NotNull]
        public ObservableCollection<Project> ReferencedBy
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<Project>>() != null);

                return _referencedBy;
            }
        }

        [NotNull]
        private IEnumerable<VSLangProj.Reference> GetReferences()
        {
            Contract.Ensures(Contract.Result<IEnumerable<VSLangProj.Reference>>() != null);

            return VsProjectReferences ?? MpfProjectReferences ?? Enumerable.Empty<VSLangProj.Reference>();
        }

        [ContractVerification(false)]
        private IEnumerable<VSLangProj.Reference> MpfProjectReferences
        {
            get
            {
                try
                {
                    var projectItems = DteProject?.ProjectItems;

                    return projectItems?
                        .Cast<EnvDTE.ProjectItem>()
                        .Select(p => p.Object)
                        .OfType<VSLangProj.References>()
                        .Take(1)
                        .SelectMany(references => references.Cast<VSLangProj.Reference>());
                }
                catch
                {
                }

                return null;
            }
        }

        [ContractVerification(false)]
        private IEnumerable<VSLangProj.Reference> VsProjectReferences
        {
            get
            {
                try
                {
                    var vsProject = DteProject?.Object as VSLangProj.VSProject;

                    return vsProject?.References?.Cast<VSLangProj.Reference>();
                }
                catch
                {
                    return null;
                }
            }
        }

        internal EnvDTE.Project DteProject
        {
            get
            {
                object obj;
                _projectHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out obj);
                return obj as EnvDTE.Project;

            }
        }

        internal bool IsSaved
        {
            get
            {
                try
                {
                    return DteProject?.Saved ?? true;
                }
                catch
                {
                    // project is currently unloaded...
                    return true;
                }
            }
        }

        public bool IsLoaded
        {
            get
            {
                try
                {
                    var dteProject = DteProject;

                    return dteProject != null && (dteProject.Saved || dteProject.IsDirty);
                }
                catch
                {
                    // project is currently unloaded...
                    return false;
                }
            }
        }

        [NotNull]
        internal ProjectFile ProjectFile
        {
            get
            {
                Contract.Ensures(Contract.Result<ProjectFile>() != null);
                return _projectFile;
            }
        }

        public bool CanEdit()
        {
            return IsSaved && _projectFile.CanEdit();
        }

        public void Reload([NotNull] IVsHierarchy hierarchy)
        {
            Contract.Requires(hierarchy != null);

            _projectHierarchy = hierarchy;

            Reload();
        }

        public void Reload()
        {
            _projectFile = new ProjectFile(_solution, this);

            Update();

            OnPropertyChanged(nameof(IsLoaded));
        }

        internal void Update()
        {
            var projectConfigurations = this.GetProjectConfigurations().ToArray();

            _internalSpecificProjectConfigurations.SynchronizeWith(projectConfigurations);
            _defaultProjectConfiguration.SetProjectFile(_projectFile);
            _internalSpecificProjectConfigurations.ForEach(config => config.SetProjectFile(_projectFile));
        }

        internal void UpdateReferences()
        {
            var projectReferences = GetReferences()
                .Select(GetSourceProjectFullName)
                .Where(fullName => fullName != null)
                .Select(fullName => _solution.Projects.SingleOrDefault(p => string.Equals(p.FullName, fullName, StringComparison.OrdinalIgnoreCase)))
                .Where(project => project != null)
                .ToArray();

            _references.SynchronizeWith(projectReferences);
        }

        internal IProjectProperty CreateProperty([NotNull] string propertyName, string configuration, string platform)
        {
            Contract.Requires(propertyName != null);

            return _projectFile.CreateProperty(propertyName, configuration, platform);
        }

        internal void DeleteProperty([NotNull] string propertyName, string configuration, string platform)
        {
            Contract.Requires(propertyName != null);

            _projectFile.DeleteProperty(propertyName, configuration, platform);
        }

        internal void Delete([NotNull] ProjectConfiguration configuration)
        {
            Contract.Requires(configuration != null);

            if (_internalSpecificProjectConfigurations.Remove(configuration))
            {
                _projectFile.DeleteConfiguration(configuration.Configuration, configuration.Platform);
            }
        }

        [NotNull]
        private string[] RetrieveProjectTypeGuids()
        {
            Contract.Ensures(Contract.Result<string[]>() != null);

            return (_defaultProjectConfiguration.PropertyValue[ProjectTypeGuidsPropertyKey] ?? ProjectTypeGuid.Unspecified)
                .Split(';')
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrEmpty(item))
                .ToArray();
        }

        private static string GetSourceProjectFullName([NotNull] VSLangProj.Reference reference)
        {
            Contract.Requires(reference != null);

            try
            {
                return reference.SourceProject?.FullName;
            }
            catch
            {
            }

            return null;
        }

        private class ProjectTypeGuidIndexer : IIndexer<bool>
        {
            [NotNull]
            private readonly ProjectConfiguration _configuration;

            public ProjectTypeGuidIndexer([NotNull] ProjectConfiguration configuration)
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
                    _configuration.PropertyValue[ProjectTypeGuidsPropertyKey] = value == null ? null : string.Join(";", value);
                }
            }


            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            [Conditional("CONTRACTS_FULL")]
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
            return _projectHierarchy.GetHashCode();
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

            return left._projectHierarchy == right._projectHierarchy;
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
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(!string.IsNullOrEmpty(_fullName));
            Contract.Invariant(_solution != null);
            Contract.Invariant(_projectFile != null);
            Contract.Invariant(_defaultProjectConfiguration != null);
            Contract.Invariant(_internalSpecificProjectConfigurations != null);
            Contract.Invariant(_specificProjectConfigurations != null);
            Contract.Invariant(_projectConfigurations != null);
            Contract.Invariant(_referencedBy != null);
            Contract.Invariant(_references != null);
            Contract.Invariant(_projectHierarchy != null);
        }
    }
}
