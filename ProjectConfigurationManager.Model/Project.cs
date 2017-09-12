namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using JetBrains.Annotations;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;

    [Equals]
    public sealed class Project : INotifyPropertyChanged
    {
        private const string ProjectTypeGuidsPropertyKey = "ProjectTypeGuids";

        [NotNull]
        private readonly ObservableCollection<ProjectConfiguration> _internalSpecificProjectConfigurations = new ObservableCollection<ProjectConfiguration>();

        private Project([NotNull] Solution solution, [NotNull] string fullName, [NotNull] IVsHierarchy projectHierarchy)
        {
            Contract.Requires(solution != null);
            Contract.Requires(fullName != null);
            Contract.Requires(projectHierarchy != null);

            Solution = solution;
            FullName = fullName;
            ProjectHierarchy = projectHierarchy;

            ProjectFile = new ProjectFile(solution, this);

            DefaultProjectConfiguration = new ProjectConfiguration(this, null, null);
            SpecificProjectConfigurations = new ReadOnlyObservableCollection<ProjectConfiguration>(_internalSpecificProjectConfigurations);

            ProjectConfigurations = ObservableCompositeCollection.FromSingleItemAndList(DefaultProjectConfiguration, _internalSpecificProjectConfigurations);
            IsProjectTypeGuidSelected = new ProjectTypeGuidIndexer(DefaultProjectConfiguration);

            Update();
        }

        [CanBeNull]
        internal static Project Create([NotNull] Solution solution, [NotNull] string fullName, [NotNull] IVsHierarchy projectHierarchy, bool retryOnErrors, [NotNull] ITracer tracer)
        {
            Contract.Requires(solution != null);
            Contract.Requires(fullName != null);
            Contract.Requires(projectHierarchy != null);
            Contract.Requires(tracer != null);

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
        public IVsHierarchy ProjectHierarchy { get; private set; }

        [NotNull, UsedImplicitly]
        public Solution Solution { get; }

        [NotNull]
        public string Name => DteProject?.Name ?? Path.GetFileNameWithoutExtension(FullName);

        [NotNull, UsedImplicitly]
        public string UniqueName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                var solutionFolder = Solution.SolutionFolder;
                if (string.IsNullOrEmpty(solutionFolder))
                    return FullName;

                var solutionFolderUri = new Uri(solutionFolder + Path.DirectorySeparatorChar, UriKind.Absolute);
                var projectUri = new Uri(FullName, UriKind.Absolute);

                var uniqueName = Uri.UnescapeDataString(solutionFolderUri
                        .MakeRelativeUri(projectUri)
                        .ToString())
                    .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

                Contract.Assume(DteProject == null || DteProject.UniqueName == uniqueName);

                return uniqueName;
            }
        }

        [NotNull]
        // ReSharper disable once AssignNullToNotNullAttribute
        public string RelativePath => Path.GetDirectoryName(UniqueName);

        [NotNull, UsedImplicitly]
        public string SortKey => Name + " (" + RelativePath + ")";

        [NotNull]
        public string FullName { get; }

        [NotNull, ItemNotNull]
        public IList<string> ProjectTypeGuids => RetrieveProjectTypeGuids();

        [NotNull, ItemNotNull]
        public IEnumerable<SolutionContext> SolutionContexts => Solution.SolutionConfigurations.SelectMany(cfg => cfg.Contexts).Where(context => context?.ProjectName == UniqueName);

        [NotNull, ItemNotNull]
        public ReadOnlyObservableCollection<ProjectConfiguration> SpecificProjectConfigurations { get; }

        [NotNull]
        public ProjectConfiguration DefaultProjectConfiguration { get; }

        [NotNull, ItemNotNull]
        public IObservableCollection<ProjectConfiguration> ProjectConfigurations { get; }

        [NotNull, UsedImplicitly]
        public IIndexer<bool> IsProjectTypeGuidSelected { get; }

        public bool IsSaving => ProjectFile.IsSaving;

        public DateTime FileTime => ProjectFile.FileTime;

        [NotNull]
        public ObservableCollection<Project> References { get; } = new ObservableCollection<Project>();

        [NotNull]
        public ObservableCollection<Project> ReferencedBy { get; } = new ObservableCollection<Project>();

        [NotNull]
        private IEnumerable<VSLangProj.Reference> GetReferences()
        {
            Contract.Ensures(Contract.Result<IEnumerable<VSLangProj.Reference>>() != null);

            return VsProjectReferences ?? MpfProjectReferences ?? Enumerable.Empty<VSLangProj.Reference>();
        }

        [CanBeNull]
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
                        .Select(p => p?.Object)
                        .OfType<VSLangProj.References>()
                        .Take(1)
                        // ReSharper disable once AssignNullToNotNullAttribute
                        .SelectMany(references => references.Cast<VSLangProj.Reference>());
                }
                catch
                {
                    // not an MPF project
                }

                return null;
            }
        }

        [CanBeNull]
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

        [CanBeNull]
        private EnvDTE.Project DteProject
        {
            get
            {
                ProjectHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object obj);
                return obj as EnvDTE.Project;

            }
        }

        public bool IsSaved
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
        internal ProjectFile ProjectFile { get; private set; }

        public bool CanEdit()
        {
            return IsSaved && ProjectFile.CanEdit();
        }

        public void Reload([NotNull] IVsHierarchy hierarchy)
        {
            Contract.Requires(hierarchy != null);

            ProjectHierarchy = hierarchy;

            Reload();
        }

        public void Reload()
        {
            ProjectFile = new ProjectFile(Solution, this);

            Update();
        }

        public void UnloadProject()
        {
            var solution = Solution.GetService(typeof(SVsSolution)) as IVsSolution4;

            var projectGuid = Solution.GetProjectGuid(ProjectHierarchy);

            solution?.UnloadProject(ref projectGuid, (int) _VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser);
        }

        private void Update()
        {
            var projectConfigurations = this.GetProjectConfigurations().ToArray();

            _internalSpecificProjectConfigurations.SynchronizeWith(projectConfigurations);

            DefaultProjectConfiguration.SetProjectFile(ProjectFile);

            _internalSpecificProjectConfigurations.ForEach(config => config?.SetProjectFile(ProjectFile));

            InvalidateState();
        }

        internal void InvalidateState()
        {
            OnPropertyChanged(nameof(IsSaved));
            OnPropertyChanged(nameof(IsLoaded));
        }

        internal void UpdateReferences()
        {
            var projectReferences = GetReferences()
                .Select(GetSourceProjectFullName)
                .Where(fullName => fullName != null)
                .Select(fullName => Solution.Projects.SingleOrDefault(p => string.Equals(p.FullName, fullName, StringComparison.OrdinalIgnoreCase)))
                .Where(project => project != null)
                .ToArray();

            References.SynchronizeWith(projectReferences);
        }

        [CanBeNull]
        internal IProjectProperty CreateProperty([NotNull] string propertyName, [CanBeNull] string configuration, [CanBeNull] string platform)
        {
            Contract.Requires(propertyName != null);

            return ProjectFile.CreateProperty(propertyName, configuration, platform);
        }

        internal void DeleteProperty([NotNull] string propertyName, [CanBeNull] string configuration, [CanBeNull] string platform)
        {
            Contract.Requires(propertyName != null);

            ProjectFile.DeleteProperty(propertyName, configuration, platform);
        }

        internal void Delete([NotNull] ProjectConfiguration configuration)
        {
            Contract.Requires(configuration != null);

            if (_internalSpecificProjectConfigurations.Remove(configuration))
            {
                ProjectFile.DeleteConfiguration(configuration.Configuration, configuration.Platform);
            }
        }

        [NotNull]
        private string[] RetrieveProjectTypeGuids()
        {
            Contract.Ensures(Contract.Result<string[]>() != null);

            return (DefaultProjectConfiguration.PropertyValue[ProjectTypeGuidsPropertyKey] ?? ProjectTypeGuid.Unspecified)
                .Split(';')
                // ReSharper disable once PossibleNullReferenceException
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrEmpty(item))
                .ToArray();
        }

        [CanBeNull]
        private static string GetSourceProjectFullName([NotNull] VSLangProj.Reference reference)
        {
            Contract.Requires(reference != null);

            try
            {
                return reference.SourceProject?.FullName;
            }
            catch
            {
                // invalid reference
            }

            return null;
        }

        private sealed class ProjectTypeGuidIndexer : IIndexer<bool>
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
                get => ProjectTypeGuids.Contains(projectTypeGuid, StringComparer.OrdinalIgnoreCase);
                set
                {
                    ProjectTypeGuids = value
                        ? new[] { projectTypeGuid }.Concat(ProjectTypeGuids).Distinct(StringComparer.OrdinalIgnoreCase)
                        : ProjectTypeGuids.Where(item => !item.Equals(projectTypeGuid, StringComparison.OrdinalIgnoreCase));
                }
            }

            [NotNull, ItemNotNull]
            private IEnumerable<string> ProjectTypeGuids
            {
                get
                {
                    return _configuration.PropertyValue[ProjectTypeGuidsPropertyKey]
                        ?.Split(';')
                        .Select(item => item?.Trim())
                        .Where(item => !string.IsNullOrEmpty(item)) ?? Enumerable.Empty<string>();
                }
                set => _configuration.PropertyValue[ProjectTypeGuidsPropertyKey] = string.Join(";", value);
            }


            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            [Conditional("CONTRACTS_FULL")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_configuration != null);
            }
        }

        [CustomGetHashCode, UsedImplicitly]
        private int CustomGetHashCode()
        {
            return ProjectHierarchy.GetHashCode();
        }

        [CustomEqualsInternal, UsedImplicitly]
        private bool CustomEquals([NotNull] Project other)
        {
            return ProjectHierarchy == other.ProjectHierarchy;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return Name;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(!string.IsNullOrEmpty(FullName));
            Contract.Invariant(Solution != null);
            Contract.Invariant(ProjectFile != null);
            Contract.Invariant(DefaultProjectConfiguration != null);
            Contract.Invariant(_internalSpecificProjectConfigurations != null);
            Contract.Invariant(SpecificProjectConfigurations != null);
            Contract.Invariant(ProjectConfigurations != null);
            Contract.Invariant(ReferencedBy != null);
            Contract.Invariant(References != null);
            Contract.Invariant(ProjectHierarchy != null);
            Contract.Invariant(IsProjectTypeGuidSelected != null);
        }
    }
}
