namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;
    using TomsToolbox.Wpf.Composition;

    [DisplayName("Dependencies")]
    [VisualCompositionExport(GlobalId.ShellRegion, Sequence = 4)]
    public class ProjectDependenciesViewModel : ObservableObject
    {
        private readonly IObservableCollection<ProjectDependency> _references;
        private readonly IObservableCollection<ProjectDependency> _referencedBy;

        public ProjectDependenciesViewModel(Solution solution)
        {
            Contract.Requires(solution != null);

            _references = solution.Projects.ObservableSelect(project => new ProjectDependency(this, null, project, proj => proj.References));
            _referencedBy = solution.Projects.ObservableSelect(project => new ProjectDependency(this, null, project, proj => proj.ReferencedBy));

            Groups = new[]
            {
                new ProjectDependencyGroup("References", _references),
                new ProjectDependencyGroup("Referenced By", _referencedBy),
            };
        }

        public ICollection<ProjectDependencyGroup> Groups { get; }

        public void UpdateSelection(Project project, bool value)
        {
            _references.Concat(_referencedBy)
                .SelectMany(p => p.DescendantsAndSelf)
                .ForEach(p => p.IsProjectSelected = value && (p.Project == project));
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_references != null);
            Contract.Invariant(_referencedBy != null);
        }
    }

    public class ProjectDependencyGroup : ObservableObject
    {
        public ProjectDependencyGroup(string name, IObservableCollection<ProjectDependency> items)
        {
            Items = items;
            Name = name;
        }

        public string Name { get; }
        public IObservableCollection<ProjectDependency> Items { get; }
    }

    public class ProjectDependency : ObservableObject
    {
        private readonly ProjectDependenciesViewModel _model;
        private bool _isSelected;
        private bool _isProjectSelected;
        private readonly Project _project;

        public ProjectDependency(ProjectDependenciesViewModel model, ProjectDependency parent, Project project, Func<Project, IList<Project>> getChildProjectsCallback)
        {
            Contract.Requires(model != null);
            Contract.Requires(project != null);
            Contract.Requires(getChildProjectsCallback != null);

            _model = model;

            Level = (parent?.Level ?? -1) + 1;
            _project = project;

            Children = GetChildren(project, getChildProjectsCallback);
        }

        public Project Project
        {
            get
            {
                Contract.Ensures(Contract.Result<Project>() != null);
                return _project;
            }
        }

        public ICollection<ProjectDependency> Children { get; }

        public int Level { get; }

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    _model.UpdateSelection(_project, value);
                }
            }
        }

        public bool IsProjectSelected
        {
            get
            {
                return _isProjectSelected;
            }
            set
            {
                SetProperty(ref _isProjectSelected, value);
            }
        }

        public IEnumerable<ProjectDependency> DescendantsAndSelf
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<ProjectDependency>>() != null);

                yield return this;

                foreach (var item in Children.SelectMany(p => p.DescendantsAndSelf))
                {
                    yield return item;
                }
            }

        }

        [ContractVerification(false)]
        private IObservableCollection<ProjectDependency> GetChildren(Project project, Func<Project, IList<Project>> getChildProjectsCallback)
        {
            Contract.Requires(project != null);
            Contract.Requires(getChildProjectsCallback != null);

            return getChildProjectsCallback(project)?.ObservableSelect(p => new ProjectDependency(_model, this, p, getChildProjectsCallback));
        }

        public override string ToString()
        {
            return _project.Name;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_project != null);
            Contract.Invariant(_model != null);
        }
    }
}
