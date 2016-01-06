namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;

    using EnvDTE;

    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;

    [Export]
    public class Solution : ObservableObject
    {
        private readonly ITracer _tracer;
        private readonly IVsServiceProvider _serviceProvider;

        private readonly ObservableCollection<Project> _projects = new ObservableCollection<Project>();
        private readonly ObservableCollection<SolutionConfiguration> _configurations = new ObservableCollection<SolutionConfiguration>();
        private readonly IObservableCollection<ProjectConfiguration> _projectConfigurations;
        private readonly IObservableCollection<SolutionContext> _solutionContexts;
        private readonly SolutionEvents _solutionEvents;
        private ProjectItemsEvents _solutionItemEvents;

        [ImportingConstructor]
        public Solution(ITracer tracer, IVsServiceProvider serviceProvider)
        {
            Contract.Requires(tracer != null);
            Contract.Requires(serviceProvider != null);

            _tracer = tracer;
            _serviceProvider = serviceProvider;

            _projectConfigurations = Projects.ObservableSelectMany(prj => prj.ProjectConfigurations);
            _solutionContexts = SolutionConfigurations.ObservableSelectMany(cfg => cfg.Contexts);

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
            Update();
        }

        public ObservableCollection<Project> Projects => _projects;

        public ObservableCollection<SolutionConfiguration> SolutionConfigurations => _configurations;

        public IObservableCollection<SolutionContext> SolutionContexts => _solutionContexts;

        public IObservableCollection<ProjectConfiguration> ProjectConfigurations => _projectConfigurations;

        public string SolutionFolder
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                try
                {
                    return Path.GetDirectoryName(FullName) ?? string.Empty;
                }
                catch
                {
                }

                return string.Empty;
            }
        }

        public string FullName => DteSolution?.FullName;

        internal void Update()
        {
            _projects.SynchronizeWith(GetProjects().ToArray());

            _configurations.SynchronizeWith(GetConfigurations().ToArray());
        }

        private IEnumerable<SolutionConfiguration> GetConfigurations()
        {
            return DteSolution.SolutionBuild.SolutionConfigurations
                .OfType<EnvDTE80.SolutionConfiguration2>()
                .Select(item => new SolutionConfiguration(this, item));
        }

        private IEnumerable<Project> GetProjects()
        {
            Contract.Ensures(Contract.Result<IEnumerable<Project>>() != null);

            var projects = DteSolution?.Projects;

            if (projects == null)
                yield break;

            for (var i = 1; i <= projects.Count; i++)
            {
                EnvDTE.Project project;
                try
                {
                    project = projects.Item(i);
                    if (project == null)
                        continue;
                }
                catch
                {
                    _tracer.TraceError("Error loading project #" + i);
                    continue;
                }

                yield return new Project(this, project);
            }
        }

        // ReSharper disable once SuspiciousTypeConversion.Global
        private EnvDTE80.Solution2 DteSolution => Dte?.Solution as EnvDTE80.Solution2;

        private EnvDTE80.DTE2 Dte => _serviceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;

        private EnvDTE.Globals Globals
        {
            get
            {
                var solution = DteSolution;

                return string.IsNullOrEmpty(FullName) ? null : solution?.Globals;
            }
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_tracer != null);
            Contract.Invariant(_serviceProvider != null);
            Contract.Invariant(_projects != null);
            Contract.Invariant(_configurations != null);
        }
    }
}
