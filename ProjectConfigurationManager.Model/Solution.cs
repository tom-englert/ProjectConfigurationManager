namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Windows.Input;
    using System.Windows.Threading;

    using JetBrains.Annotations;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;

    using Throttle;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;

    [Export]
    public sealed class Solution : ObservableObject, IServiceProvider
    {
        [NotNull]
        private readonly IVsServiceProvider _serviceProvider;
        [NotNull]
        private readonly PerformanceTracer _performanceTracer;

        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        [CanBeNull, UsedImplicitly]
        private readonly EnvDTE.SolutionEvents _solutionEvents; // => need to hold a ref to events!
        [CanBeNull]
        private FileSystemWatcher _fileSystemWatcher;

        [ImportingConstructor]
        public Solution([NotNull] ITracer tracer, [NotNull] IVsServiceProvider serviceProvider, [NotNull] PerformanceTracer performanceTracer)
        {
            Tracer = tracer;
            _serviceProvider = serviceProvider;
            _performanceTracer = performanceTracer;

            SpecificProjectConfigurations = Projects.ObservableSelectMany(prj => prj.SpecificProjectConfigurations);
            ProjectConfigurations = Projects.ObservableSelectMany(prj => prj.ProjectConfigurations);

            var solutionEvents = Dte?.Events?.SolutionEvents;
            if (solutionEvents != null)
            {
                solutionEvents.Opened += () => OnSolutionChanged("Solution opened");
                solutionEvents.AfterClosing += () => OnSolutionChanged("Solution after closing");
                solutionEvents.ProjectAdded += _ => OnSolutionChanged("Project added");
                solutionEvents.ProjectRemoved += _ => OnSolutionChanged("Project removed");
                solutionEvents.ProjectRenamed += (_, __) => OnSolutionChanged("Project renamed");
            }

            _solutionEvents = solutionEvents;

            Update(0);

            CommandManager.RequerySuggested += (_, __) => Projects.ForEach(proj => proj?.InvalidateState());
        }

        public event EventHandler Changed;

        public event EventHandler<FileSystemEventArgs> FileChanged;

        private void OnSolutionChanged([NotNull] string action)
        {
            Tracer.WriteLine(action);

            Update();
        }

        [NotNull, ItemNotNull]
        public ObservableCollection<Project> Projects { get; } = new ObservableCollection<Project>();

        [NotNull, ItemNotNull]
        public ObservableCollection<SolutionConfiguration> SolutionConfigurations { get; } = new ObservableCollection<SolutionConfiguration>();

        [NotNull, ItemNotNull]
        public IObservableCollection<ProjectConfiguration> ProjectConfigurations { get; }

        [NotNull]
        public ITracer Tracer { get; }

        [NotNull, ItemNotNull]
        public IObservableCollection<ProjectConfiguration> SpecificProjectConfigurations { get; }

        [NotNull, ItemNotNull]
        public ObservableCollection<ProjectPropertyName> ProjectProperties { get; } = new ObservableCollection<ProjectPropertyName>();

        [NotNull, ItemNotNull]
        public ObservableCollection<string> ProjectTypeGuids { get; } = new ObservableCollection<string>();

        [CanBeNull]
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
                    // solution not available
                }

                return null;
            }
        }

        [CanBeNull]
        public object GetService([NotNull] Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }

        [Throttled(typeof(DispatcherThrottle), (int)DispatcherPriority.ApplicationIdle)]
        public void Update()
        {
            Update(0);
        }

        private void Update(int retry)
        {
            using (_performanceTracer.Start("Update"))
            {
                try
                {
                    SynchronizeCollections(retry < 3);
                    SetupFileSystemWatcher();
                    Changed?.Invoke(this, EventArgs.Empty);
                }
                catch (RetryException)
                {
                    Tracer.WriteLine("Retry Update...");
                    // could not access the project file, retry later...
                    Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () => Update(retry + 1));
                }
                catch (Exception ex)
                {
                    Tracer.TraceError(ex);
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
                    watcher = new FileSystemWatcher(solutionFolder)
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
                Tracer.TraceError(ex);
            }
        }

        private void Watcher_Changed([NotNull] object sender, [NotNull] FileSystemEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, () => DeferredOnWatcherChanged(e, 0));
        }

        private void DeferredOnWatcherChanged([NotNull] FileSystemEventArgs e, int retry)
        {
            try
            {
                FileChanged?.Invoke(this, e);

                var project = Projects.FirstOrDefault(prj => string.Equals(e.FullPath, prj.FullName, StringComparison.OrdinalIgnoreCase));

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

                Tracer.TraceError(ex);
            }
        }

        private void SynchronizeCollections(bool retryOnErrors)
        {
            Projects.SynchronizeWith(GetProjects(retryOnErrors).ToArray());

            SolutionConfigurations.SynchronizeWith(GetConfigurations().ToArray());

            ProjectProperties.SynchronizeWith(GetProjectProperties().ToArray());

            ProjectTypeGuids.SynchronizeWith(Projects.SelectMany(p => p.ProjectTypeGuids).ToArray());

            UpdateReferences();
        }

        private void UpdateReferences()
        {
            var dependencies = new Dictionary<Project, IList<Project>>();

            foreach (var project in Projects)
            {
                project.UpdateReferences();
            }

            foreach (var project in Projects)
            {
                foreach (var dependency in project.References)
                {
                    dependencies.ForceValue(dependency, _ => new List<Project>())?.Add(project);
                }
            }

            foreach (var project in Projects)
            {
                var dependentProjects = dependencies.ForceValue(project, _ => new List<Project>());
                Debug.Assert(dependentProjects != null, nameof(dependentProjects) + " != null");
                project.ReferencedBy.SynchronizeWith(dependentProjects);
            }
        }

        [NotNull, ItemNotNull]
        private IEnumerable<SolutionConfiguration> GetConfigurations()
        {
            return DteSolution?.SolutionBuild?.SolutionConfigurations?.OfType<EnvDTE80.SolutionConfiguration2>()
                .Select(item => new SolutionConfiguration(this, item)) ?? Enumerable.Empty<SolutionConfiguration>();
        }

        [NotNull, ItemNotNull]
        private IEnumerable<Project> GetProjects(bool retryOnErrors)
        {
            var solution = (IVsSolution)GetService(typeof(IVsSolution));

            foreach (var projectHierarchy in GetProjectsInSolution(solution, __VSENUMPROJFLAGS.EPF_ALLINSOLUTION))
            {

                string fullName = null;
                // ReSharper disable once SuspiciousTypeConversion.Global
                var vsProject = projectHierarchy as IVsProject;
                vsProject?.GetMkDocument(VSConstants.VSITEMID_ROOT, out fullName);

                if (string.IsNullOrEmpty(fullName))
                {
                    // unloaded project?
                    projectHierarchy.GetCanonicalName(VSConstants.VSITEMID_ROOT, out fullName);
                }

                // Skip web projects, we can't edit them.
                if (!Uri.TryCreate(fullName, UriKind.Absolute, out var projectUri) || !projectUri.IsFile || !File.Exists(fullName))
                    continue;

                var existing = Projects.FirstOrDefault(p => string.Equals(p.FullName, fullName, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.Reload(projectHierarchy);
                    yield return existing;
                }

                var project = Project.Create(this, fullName, projectHierarchy, retryOnErrors, Tracer);
                if (project != null)
                    yield return project;
            }
        }

        [NotNull, ItemNotNull]
        private static IEnumerable<IVsHierarchy> GetProjectsInSolution([CanBeNull] IVsSolution solution, __VSENUMPROJFLAGS flags)
        {
            if (solution == null)
                yield break;

            var guid = Guid.Empty;
            solution.GetProjectEnum((uint)flags, ref guid, out var enumHierarchies);
            if (enumHierarchies == null)
                yield break;

            var hierarchy = new IVsHierarchy[1];
            while (enumHierarchies.Next(1, hierarchy, out var fetched) == VSConstants.S_OK && fetched == 1)
            {
                if (hierarchy.Length > 0 && hierarchy[0] != null)
                    yield return hierarchy[0];
            }
        }

        [NotNull, ItemNotNull]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private IEnumerable<ProjectPropertyName> GetProjectProperties()
        {
            return Projects
                .SelectMany(prj => ProjectConfigurations.SelectMany(cfg => cfg.Properties.Values))
                .Distinct(new DelegateEqualityComparer<IProjectProperty>(p => p.Name))
                .Where(p => PropertyGroupName.IsNotProjectSpecific(p.Name))
                .Select(p => new ProjectPropertyName(p))
                .OrderBy(item => item.GroupName.Index);
        }

        // ReSharper disable once SuspiciousTypeConversion.Global
        [CanBeNull, ItemNotNull]
        private EnvDTE80.Solution2 DteSolution => Dte?.Solution as EnvDTE80.Solution2;

        [CanBeNull]
        private EnvDTE80.DTE2 Dte => _serviceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;

        [CanBeNull, UsedImplicitly]
        private EnvDTE.Globals Globals
        {
            get
            {
                var solution = DteSolution;
                var fullName = solution?.FullName;
                return string.IsNullOrEmpty(fullName) ? null : solution.Globals;
            }
        }
    }
}
