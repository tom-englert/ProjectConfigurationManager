namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Windows.Threading;

    using JetBrains.Annotations;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;

    [Export]
    public class Solution : ObservableObject, IServiceProvider
    {
        [NotNull]
        private readonly DispatcherThrottle _deferredUpdateThrottle;
        [NotNull]
        private readonly ITracer _tracer;
        [NotNull]
        private readonly IVsServiceProvider _serviceProvider;
        [NotNull]
        private readonly PerformanceTracer _performanceTracer;

        [NotNull]
        private readonly ObservableCollection<Project> _projects = new ObservableCollection<Project>();
        [NotNull]
        private readonly ObservableCollection<SolutionConfiguration> _configurations = new ObservableCollection<SolutionConfiguration>();
        [NotNull]
        private readonly IObservableCollection<ProjectConfiguration> _specificProjectConfigurations;
        [NotNull]
        private readonly IObservableCollection<ProjectConfiguration> _projectConfigurations;
        [NotNull]
        private readonly ObservableCollection<ProjectPropertyName> _projectProperties = new ObservableCollection<ProjectPropertyName>();
        [NotNull]
        private readonly ObservableCollection<string> _projectTypeGuids = new ObservableCollection<string>();

        private readonly EnvDTE.SolutionEvents _solutionEvents;

        private FileSystemWatcher _fileSystemWatcher;

        [ImportingConstructor]
        public Solution([NotNull] ITracer tracer, [NotNull] IVsServiceProvider serviceProvider, [NotNull] PerformanceTracer performanceTracer)
        {
            Contract.Requires(tracer != null);
            Contract.Requires(serviceProvider != null);
            Contract.Requires(performanceTracer != null);

            _deferredUpdateThrottle = new DispatcherThrottle(DispatcherPriority.ContextIdle, Update);

            _tracer = tracer;
            _serviceProvider = serviceProvider;
            _performanceTracer = performanceTracer;

            _specificProjectConfigurations = _projects.ObservableSelectMany(prj => prj.SpecificProjectConfigurations);
            _projectConfigurations = _projects.ObservableSelectMany(prj => prj.ProjectConfigurations);

            _solutionEvents = Dte?.Events?.SolutionEvents;
            if (_solutionEvents != null)
            {
                _solutionEvents.Opened += () => Solution_Changed("Solution opened");
                _solutionEvents.AfterClosing += () => Solution_Changed("Solution after closing");
                _solutionEvents.ProjectAdded += _ => Solution_Changed("Project added");
                _solutionEvents.ProjectRemoved += _ => Solution_Changed("Project removed");
                _solutionEvents.ProjectRenamed += (_, __) => Solution_Changed("Project renamed");
            }

            Update();
        }

        private void Solution_Changed(string action)
        {
            if (_projects.Any(p => p.IsSaving))
                return;

            _tracer.WriteLine(action);
            _deferredUpdateThrottle.Tick();
        }

        [NotNull]
        public ObservableCollection<Project> Projects
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<Project>>() != null);
                return _projects;
            }
        }

        [NotNull]
        public ObservableCollection<SolutionConfiguration> SolutionConfigurations
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<SolutionConfiguration>>() != null);
                return _configurations;
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

        [NotNull]
        public ITracer Tracer
        {
            get
            {
                Contract.Ensures(Contract.Result<ITracer>() != null);
                return _tracer;
            }
        }

        [NotNull]
        public IObservableCollection<ProjectConfiguration> SpecificProjectConfigurations
        {
            get
            {
                Contract.Ensures(Contract.Result<IObservableCollection<ProjectConfiguration>>() != null);
                return _specificProjectConfigurations;
            }
        }

        [NotNull]
        public ObservableCollection<ProjectPropertyName> ProjectProperties
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<ProjectPropertyName>>() != null);
                return _projectProperties;
            }
        }

        [NotNull]
        public ObservableCollection<string> ProjectTypeGuids
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<string>>() != null);
                return _projectTypeGuids;
            }
        }

        public string SolutionFolder
        {
            get
            {
                try
                {
                    var fullName = DteSolution?.FullName;

                    if (!string.IsNullOrEmpty(fullName))
                        return Path.GetDirectoryName(fullName);
                }
                catch
                {
                }

                return null;
            }
        }

        public object GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }

        public void Update()
        {
            Update(0);
        }

        public void Update(int retry)
        {
            using (_performanceTracer.Start("Update"))
            {
                try
                {
                    SynchronizeCollections(retry < 3);
                    SetupFileSystemWatcher();
                }
                catch (RetryException)
                {
                    _tracer.WriteLine("Retry Update...");
                    // could not access the project file, retry later...
                    Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () => Update(retry + 1));
                }
                catch (Exception ex)
                {
                    _tracer.TraceError(ex);
                }
            }
        }

        private void SetupFileSystemWatcher()
        {
            try
            {
                var solutionFolder = SolutionFolder;

                if (string.Equals(_fileSystemWatcher?.Path, solutionFolder, StringComparison.OrdinalIgnoreCase))
                    return;

                FileSystemWatcher watcher = null;

                if (solutionFolder != null)
                {
                    watcher = new FileSystemWatcher(solutionFolder, "*.*")
                    {
                        EnableRaisingEvents = true,
                        NotifyFilter = NotifyFilters.LastWrite,
                        IncludeSubdirectories = true
                    };

                    watcher.Changed += Watcher_Changed;
                }

                Interlocked.Exchange(ref _fileSystemWatcher, watcher)?.Dispose();
            }
            catch (Exception ex)
            {
                _tracer.TraceError(ex);
            }
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, () => DeferredOnWatcherChanged(e, 0));
        }

        private void DeferredOnWatcherChanged(FileSystemEventArgs e, int retry)
        {
            try
            {
                var project = _projects.FirstOrDefault(prj => string.Equals(e.FullPath, prj.FullName, StringComparison.OrdinalIgnoreCase));

                if ((project == null) || project.IsSaving || (project.FileTime == File.GetLastWriteTime(project.FullName)))
                    return;

                using (_performanceTracer.Start("Update"))
                {
                    try
                    {
                        project.Reload();
                    }
                    catch (Exception ex)
                    {
                        if ((retry >= 3) || (ex.GetType() != typeof(IOException)))
                            throw;

                        // could not access the project file, retry later...
                        Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () => DeferredOnWatcherChanged(e, retry + 1));
                        return;
                    }

                    SynchronizeCollections(retry < 3);
                }
            }
            catch (Exception ex)
            {
                if (ex is RetryException)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, () => DeferredOnWatcherChanged(e, retry + 1));
                }

                _tracer.TraceError(ex);
            }
        }

        private void SynchronizeCollections(bool retryOnErrors)
        {
            _projects.SynchronizeWith(GetProjects(retryOnErrors).ToArray());

            _configurations.SynchronizeWith(GetConfigurations().ToArray());

            _projectProperties.SynchronizeWith(GetProjectProperties().ToArray());

            _projectTypeGuids.SynchronizeWith(_projects.SelectMany(p => p.ProjectTypeGuids).ToArray());

            UpdateReferences();
        }

        private void UpdateReferences()
        {
            var dependencies = new Dictionary<Project, IList<Project>>();

            foreach (var project in _projects)
            {
                project?.UpdateReferences();
            }

            foreach (var project in _projects)
            {
                Contract.Assume(project != null);
                foreach (var dependency in project.References)
                {
                    Contract.Assume(dependency != null);
                    dependencies.ForceValue(dependency, _ => new List<Project>())?.Add(project);
                }
            }

            foreach (var project in _projects)
            {
                Contract.Assume(project != null);
                var dependentProjects = dependencies.ForceValue(project, _ => new List<Project>());
                Contract.Assume(dependentProjects != null);
                project.ReferencedBy.SynchronizeWith(dependentProjects);
            }
        }

        [NotNull]
        private IEnumerable<SolutionConfiguration> GetConfigurations()
        {
            Contract.Ensures(Contract.Result<IEnumerable<SolutionConfiguration>>() != null);

            return DteSolution?.SolutionBuild?.SolutionConfigurations?.OfType<EnvDTE80.SolutionConfiguration2>()
                .Select(item => new SolutionConfiguration(this, item)) ?? Enumerable.Empty<SolutionConfiguration>();
        }

        [NotNull]
        private IEnumerable<Project> GetProjects(bool retryOnErrors)
        {
            Contract.Ensures(Contract.Result<IEnumerable<Project>>() != null);

            var solution = (IVsSolution)GetService(typeof(IVsSolution));

            foreach (var projectHierarchy in GetProjectsInSolution(solution, __VSENUMPROJFLAGS.EPF_ALLINSOLUTION))
            {
                Uri projectUri;

                string fullName = null;
                var vsProject = projectHierarchy as IVsProject;
                vsProject?.GetMkDocument(VSConstants.VSITEMID_ROOT, out fullName);

                if (string.IsNullOrEmpty(fullName))
                {
                    // unloaded project?
                    projectHierarchy.GetCanonicalName(VSConstants.VSITEMID_ROOT, out fullName);
                }

                // Skip web pojects, we can't edit them.
                if (!Uri.TryCreate(fullName, UriKind.Absolute, out projectUri) || !projectUri.IsFile || !File.Exists(fullName))
                    continue;

                var existing = _projects.FirstOrDefault(p => string.Equals(p.FullName, fullName, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.Reload(projectHierarchy);
                    yield return existing;
                }

                var project = Project.Create(this, fullName, projectHierarchy, retryOnErrors, _tracer);
                if (project != null)
                    yield return project;
            }
        }

        [NotNull, ItemNotNull]
        private static IEnumerable<IVsHierarchy> GetProjectsInSolution(IVsSolution solution, __VSENUMPROJFLAGS flags)
        {
            Contract.Ensures(Contract.Result<IEnumerable<IVsHierarchy>>() != null);

            if (solution == null)
                yield break;

            IEnumHierarchies enumHierarchies;
            var guid = Guid.Empty;
            solution.GetProjectEnum((uint)flags, ref guid, out enumHierarchies);
            if (enumHierarchies == null)
                yield break;

            var hierarchy = new IVsHierarchy[1];
            uint fetched;
            while (enumHierarchies.Next(1, hierarchy, out fetched) == VSConstants.S_OK && fetched == 1)
            {
                if (hierarchy.Length > 0 && hierarchy[0] != null)
                    yield return hierarchy[0];
            }
        }

        [NotNull]
        private IEnumerable<ProjectPropertyName> GetProjectProperties()
        {
            Contract.Ensures(Contract.Result<IEnumerable<ProjectPropertyName>>() != null);

            return _projects
                .SelectMany(prj => ProjectConfigurations.SelectMany(cfg => cfg.Properties.Values))
                .Select(prop => prop.Name)
                .Distinct()
                .Where(PropertyGroupName.IsNotProjectSpecific)
                .Select(name => new ProjectPropertyName(name, PropertyGroupName.GetGroupForProperty(name)))
                .OrderBy(item => item.GroupName.Index);
        }

        // ReSharper disable once SuspiciousTypeConversion.Global
        private EnvDTE80.Solution2 DteSolution => Dte?.Solution as EnvDTE80.Solution2;

        private EnvDTE80.DTE2 Dte => _serviceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;

        private EnvDTE.Globals Globals
        {
            get
            {
                var solution = DteSolution;
                var fullName = solution?.FullName;
                return string.IsNullOrEmpty(fullName) ? null : solution?.Globals;
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_deferredUpdateThrottle != null);
            Contract.Invariant(_tracer != null);
            Contract.Invariant(_serviceProvider != null);
            Contract.Invariant(_performanceTracer != null);
            Contract.Invariant(_projects != null);
            Contract.Invariant(_configurations != null);
            Contract.Invariant(_specificProjectConfigurations != null);
            Contract.Invariant(_projectConfigurations != null);
            Contract.Invariant(_projectProperties != null);
            Contract.Invariant(_projectTypeGuids != null);
        }
    }
}
