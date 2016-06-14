namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Windows.Threading;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;

    [Export]
    public class Solution : ObservableObject, IServiceProvider
    {
        private readonly DispatcherThrottle _deferredUpdateThrottle;
        private readonly ITracer _tracer;
        private readonly IVsServiceProvider _serviceProvider;
        private readonly PerformanceTracer _performanceTracer;

        private readonly ObservableCollection<Project> _projects = new ObservableCollection<Project>();
        private readonly ObservableCollection<SolutionConfiguration> _configurations = new ObservableCollection<SolutionConfiguration>();
        private readonly IObservableCollection<ProjectConfiguration> _specificProjectConfigurations;
        private readonly IObservableCollection<ProjectConfiguration> _projectConfigurations;
        private readonly IObservableCollection<SolutionContext> _solutionContexts;
        private readonly ObservableCollection<ProjectPropertyName> _projectProperties = new ObservableCollection<ProjectPropertyName>();
        private readonly ObservableCollection<string> _projectTypeGuids = new ObservableCollection<string>();

        private readonly EnvDTE.SolutionEvents _solutionEvents;

        private FileSystemWatcher _fileSystemWatcher;

        [ImportingConstructor]
        public Solution(ITracer tracer, IVsServiceProvider serviceProvider, PerformanceTracer performanceTracer)
        {
            Contract.Requires(tracer != null);
            Contract.Requires(serviceProvider != null);
            Contract.Requires(performanceTracer != null);

            _deferredUpdateThrottle = new DispatcherThrottle(DispatcherPriority.ApplicationIdle, Update);

            _tracer = tracer;
            _serviceProvider = serviceProvider;
            _performanceTracer = performanceTracer;

            _specificProjectConfigurations = _projects.ObservableSelectMany(prj => prj.SpecificProjectConfigurations);
            _solutionContexts = SolutionConfigurations.ObservableSelectMany(cfg => cfg.Contexts);
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
            _tracer.WriteLine(action);
            _deferredUpdateThrottle.Tick();
        }

        public ObservableCollection<Project> Projects
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<Project>>() != null);
                return _projects;
            }
        }

        public ObservableCollection<SolutionConfiguration> SolutionConfigurations
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<SolutionConfiguration>>() != null);
                return _configurations;
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

        public IObservableCollection<ProjectConfiguration> ProjectConfigurations
        {
            get
            {
                Contract.Ensures(Contract.Result<IObservableCollection<ProjectConfiguration>>() != null);
                return _projectConfigurations;
            }
        }

        public ITracer Tracer
        {
            get
            {
                Contract.Ensures(Contract.Result<ITracer>() != null);
                return _tracer;
            }
        }

        public IObservableCollection<ProjectConfiguration> SpecificProjectConfigurations
        {
            get
            {
                Contract.Ensures(Contract.Result<IObservableCollection<ProjectConfiguration>>() != null);
                return _specificProjectConfigurations;
            }
        }

        public ObservableCollection<ProjectPropertyName> ProjectProperties
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<ProjectPropertyName>>() != null);
                return _projectProperties;
            }
        }

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
                        Dispatcher.BeginInvoke(DispatcherPriority.Background, () => DeferredOnWatcherChanged(e, retry + 1));
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

            _projectTypeGuids.SynchronizeWith(_projects.SelectMany(p => p.ProjectTypeGuids ?? Enumerable.Empty<string>()).ToArray());

            UpdateReferences();
        }

        private void UpdateReferences()
        {
            var dependencies = new Dictionary<Project, IList<Project>>();

            foreach (var project in _projects)
            {
                project.UpdateReferences();
            }

            foreach (var project in _projects)
            {
                Contract.Assume(project != null);
                foreach (var dependency in project.References)
                {
                    Contract.Assume(dependency != null);
                    dependencies.ForceValue(dependency, _ => new List<Project>()).Add(project);
                }
            }

            foreach (var project in _projects)
            {
                Contract.Assume(project != null);
                project.ReferencedBy.SynchronizeWith(dependencies.ForceValue(project, _ => new List<Project>()));
            }
        }

        private IEnumerable<SolutionConfiguration> GetConfigurations()
        {
            Contract.Ensures(Contract.Result<IEnumerable<SolutionConfiguration>>() != null);

            return DteSolution?.SolutionBuild?.SolutionConfigurations?.OfType<EnvDTE80.SolutionConfiguration2>()
                .Select(item => new SolutionConfiguration(this, item)) ?? Enumerable.Empty<SolutionConfiguration>();
        }

        private IEnumerable<Project> GetProjects(bool retryOnErrors)
        {
            Contract.Ensures(Contract.Result<IEnumerable<Project>>() != null);

            return GetDteProjects(retryOnErrors)
                .Select(project => Project.Create(this, project, retryOnErrors, _tracer))
                .Where(project => project != null);
        }

        private IEnumerable<EnvDTE.Project> GetDteProjects(bool retryOnErrors)
        {
            Contract.Ensures(Contract.Result<IEnumerable<EnvDTE.Project>>() != null);

            var items = new List<EnvDTE.Project>();

            GetDteProjects(DteSolution?.Projects)
                .ForEach(project => GetProjectOrSubProjects(items, project, retryOnErrors));

            return items;
        }

        private void GetProjectOrSubProjects(ICollection<EnvDTE.Project> items, EnvDTE.Project project, bool retryOnErrors)
        {
            Contract.Requires(items != null);

            if (project == null)
                return;

            if (string.Equals(project.Kind, ItemKind.SolutionFolder, StringComparison.OrdinalIgnoreCase))
            {
                GetSubProjects(project).ForEach(p => GetProjectOrSubProjects(items, p, retryOnErrors));
                return;
            }

            if (string.IsNullOrEmpty(project.UniqueName))
                return;

            try
            {
                if (!string.IsNullOrEmpty(project.FullName))
                {
                    items.Add(project);
                }
            }
            catch (Exception ex)
            {
                if (ex is NotImplementedException)
                    if (retryOnErrors)
                        throw new RetryException(ex);

                _tracer.TraceError("Error loading a project: " + ex.Message);
            }
        }

        public IEnumerable<EnvDTE.Project> GetSubProjects(EnvDTE.Project project)
        {
            try
            {
                var projectItems = project?.ProjectItems;

                if (projectItems != null)
                    return projectItems.OfType<EnvDTE.ProjectItem>().Select(item => item.SubProject);
            }
            catch (Exception ex)
            {
                _tracer.TraceError("Error loading sub projects: " + ex.Message);
            }

            return Enumerable.Empty<EnvDTE.Project>();
        }

        public IEnumerable<EnvDTE.Project> GetDteProjects(EnvDTE.Projects projects)
        {
            if (projects == null)
                yield break;

            for (var i = 1; i <= projects.Count; i++)
            {
                EnvDTE.Project item;

                try
                {
                    item = projects.Item(i);
                }
                catch (Exception ex)
                {
                    _tracer.TraceError("Error loading a project: " + ex.Message);
                    continue;
                }

                yield return item;
            }
        }

        private IEnumerable<ProjectPropertyName> GetProjectProperties()
        {
            Contract.Ensures(Contract.Result<IEnumerable<ProjectPropertyName>>() != null);

            return _projects
                .SelectMany(prj => ProjectConfigurations.SelectMany(cfg => cfg.Properties.Values))
                .Select(prop => prop.Name)
                .Distinct()
                .Where(PropertyGrouping.IsNotProjectSpecific)
                .Select(name => new ProjectPropertyName(name, PropertyGrouping.GetPropertyGroupName(name)));
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
        private void ObjectInvariant()
        {
            Contract.Invariant(_tracer != null);
            Contract.Invariant(_serviceProvider != null);
            Contract.Invariant(_performanceTracer != null);
            Contract.Invariant(_projects != null);
            Contract.Invariant(_configurations != null);
        }
    }
}
