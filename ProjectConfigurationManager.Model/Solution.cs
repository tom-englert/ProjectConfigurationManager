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
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows.Threading;

    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;

    [Export]
    public class Solution : ObservableObject, IServiceProvider
    {
        private readonly DispatcherThrottle _deferredUpdateThrottle;
        private readonly ITracer _tracer;
        private readonly IVsServiceProvider _serviceProvider;

        private readonly ObservableCollection<Project> _projects = new ObservableCollection<Project>();
        private readonly ObservableCollection<SolutionConfiguration> _configurations = new ObservableCollection<SolutionConfiguration>();
        private readonly IObservableCollection<ProjectConfiguration> _specificProjectConfigurations;
        private readonly IObservableCollection<ProjectConfiguration> _defaultProjectConfigurations;
        private readonly IObservableCollection<ProjectConfiguration> _projectConfigurations;
        private readonly IObservableCollection<SolutionContext> _solutionContexts;
        private readonly ObservableCollection<ProjectPropertyName> _projectProperties = new ObservableCollection<ProjectPropertyName>();
        private readonly ObservableCollection<string> _projectTypeGuids = new ObservableCollection<string>();

        private readonly EnvDTE.SolutionEvents _solutionEvents;

        private FileSystemWatcher _fileSystemWatcher;

        [ImportingConstructor]
        public Solution(ITracer tracer, IVsServiceProvider serviceProvider)
        {
            Contract.Requires(tracer != null);
            Contract.Requires(serviceProvider != null);

            _deferredUpdateThrottle = new DispatcherThrottle(DispatcherPriority.ContextIdle, Update);

            _tracer = tracer;
            _serviceProvider = serviceProvider;

            _specificProjectConfigurations = Projects.ObservableSelectMany(prj => prj.SpecificProjectConfigurations);
            _solutionContexts = SolutionConfigurations.ObservableSelectMany(cfg => cfg.Contexts);
            _defaultProjectConfigurations = Projects.ObservableSelect(prj => prj.DefaultProjectConfiguration);
            _projectConfigurations = new ObservableCompositeCollection<ProjectConfiguration>(_defaultProjectConfigurations, _specificProjectConfigurations);

            _solutionEvents = Dte.Events.SolutionEvents;

            _solutionEvents.Opened += Solution_Changed;
            _solutionEvents.AfterClosing += Solution_Changed;
            _solutionEvents.ProjectAdded += _ => Solution_Changed();
            _solutionEvents.ProjectRemoved += _ => Solution_Changed();
            _solutionEvents.ProjectRenamed += (_, __) => Solution_Changed();

            Update();
        }

        private void Solution_Changed()
        {
            _deferredUpdateThrottle.Tick();
        }

        public ObservableCollection<Project> Projects => _projects;

        public ObservableCollection<SolutionConfiguration> SolutionConfigurations => _configurations;

        public IObservableCollection<SolutionContext> SolutionContexts => _solutionContexts;

        public IObservableCollection<ProjectConfiguration> ProjectConfigurations => _projectConfigurations;

        public IObservableCollection<ProjectConfiguration> SpecificProjectConfigurations => _specificProjectConfigurations;

        public ObservableCollection<ProjectPropertyName> ProjectProperties => _projectProperties;

        public ObservableCollection<string> ProjectTypeGuids => _projectTypeGuids;

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

        internal void Update()
        {
            SynchronizeCollections();

            SetupFileSystemWatcher();
        }

        private void SetupFileSystemWatcher()
        {
            var solutionFolder = SolutionFolder;

            if (string.Equals(_fileSystemWatcher?.Path, solutionFolder, StringComparison.OrdinalIgnoreCase))
                return;

            var watcher = new FileSystemWatcher(solutionFolder, "*.*")
            {
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite,
                IncludeSubdirectories = true
            };

            watcher.Changed += Watcher_Changed;

            Interlocked.Exchange(ref _fileSystemWatcher, watcher)?.Dispose();
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                var project = Projects.FirstOrDefault(prj => string.Equals(e.FullPath, prj.FullName, StringComparison.OrdinalIgnoreCase));

                if ((project == null) || project.IsSaving)
                    return;

                try
                {
                    project.Reload();
                    SynchronizeCollections();
                }
                catch (ExternalException)
                {
                }
            });
        }

        private void SynchronizeCollections()
        {
            _projects.SynchronizeWith(GetProjects().ToArray());

            _configurations.SynchronizeWith(GetConfigurations().ToArray());

            _projectProperties.SynchronizeWith(GetProjectProperties().ToArray());

            _projectTypeGuids.SynchronizeWith(_projects.SelectMany(p => p.ProjectTypeGuids ?? Enumerable.Empty<string>()).ToArray());
        }

        private IEnumerable<SolutionConfiguration> GetConfigurations()
        {
            Contract.Ensures(Contract.Result<IEnumerable<SolutionConfiguration>>() != null);

            return DteSolution?.SolutionBuild.SolutionConfigurations
                .OfType<EnvDTE80.SolutionConfiguration2>()
                .Select(item => new SolutionConfiguration(this, item)) ?? Enumerable.Empty<SolutionConfiguration>();
        }

        private IEnumerable<Project> GetProjects()
        {
            return GetDteProjects().Select(project => new Project(this, project));
        }

        private IEnumerable<EnvDTE.Project> GetDteProjects()
        {
            var items = new List<EnvDTE.Project>();

            var projects = DteSolution?.Projects;

            if (projects == null)
                return items;

            for (var i = 1; i <= projects.Count; i++)
            {
                try
                {
                    GetProjectOrSubProjects(items, projects.Item(i));
                }
                catch (Exception ex)
                {
                    _tracer.TraceError("Error loading a project: " + ex.Message);
                }
            }

            return items;
        }

        private void GetProjectOrSubProjects(ICollection<EnvDTE.Project> items, EnvDTE.Project project)
        {
            if (project == null)
                return;

            try
            {
                if (!string.Equals(project.Kind, ItemKind.SolutionFolder, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(project.FullName))
                        items.Add(project);

                    return;
                }

                foreach (var projectItem in project.ProjectItems.OfType<EnvDTE.ProjectItem>())
                {
                    try
                    {
                        GetProjectOrSubProjects(items, projectItem.SubProject);
                    }
                    catch (Exception ex)
                    {
                        _tracer.TraceError("Error loading a project: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _tracer.TraceError("Error loading a project: " + ex.Message);
            }
        }

        private IEnumerable<ProjectPropertyName> GetProjectProperties()
        {
            return Projects
                .SelectMany(prj => ProjectConfigurations.SelectMany(cfg => cfg.Properties))
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
            Contract.Invariant(_projects != null);
            Contract.Invariant(_configurations != null);
        }
    }
}
