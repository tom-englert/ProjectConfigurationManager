namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows.Data;

    using JetBrains.Annotations;

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;
    using TomsToolbox.Wpf.Composition;

    [DisplayName("Dependencies")]
    [VisualCompositionExport(GlobalId.ShellRegion, Sequence = 4)]
    public sealed class ProjectDependenciesViewModel : ObservableObject
    {
        [NotNull, ItemNotNull]
        private readonly IObservableCollection<ProjectDependency> _references;
        [NotNull, ItemNotNull]
        private readonly IObservableCollection<ProjectDependency> _referencedBy;

        public ProjectDependenciesViewModel([NotNull] Solution solution)
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

        [NotNull, ItemNotNull]
        public ICollection<ProjectDependencyGroup> Groups { get; }

        public void UpdateSelection([CanBeNull] Project project, bool value)
        {
            _references.Concat(_referencedBy)
                // ReSharper disable once PossibleNullReferenceException
                .SelectMany(p => p.DescendantsAndSelf)
                // ReSharper disable once PossibleNullReferenceException
                .ForEach(p => p.IsProjectSelected = value && (p.Project == project));
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_references != null);
            Contract.Invariant(_referencedBy != null);
        }
    }

    public sealed class ProjectDependencyGroup : ObservableObject
    {
        public ProjectDependencyGroup([NotNull] string name, [NotNull, ItemNotNull] IList<ProjectDependency> items)
        {
            Contract.Requires(items != null);

            Items = items.ToCollectionView();
            Name = name;
        }

        [NotNull]
        public string Name { get; }

        [NotNull, ItemNotNull]
        public ICollectionView Items { get; }
    }

    public sealed class ProjectDependency : ObservableObject
    {
        [NotNull]
        private readonly ProjectDependenciesViewModel _model;
        [NotNull, ItemNotNull]
        private readonly IList<ProjectDependency> _children;

        public ProjectDependency([NotNull] ProjectDependenciesViewModel model, [CanBeNull] ProjectDependency parent, [NotNull] Project project, [NotNull] Func<Project, IList<Project>> getChildProjectsCallback)
        {
            Contract.Requires(model != null);
            Contract.Requires(project != null);
            Contract.Requires(getChildProjectsCallback != null);

            _model = model;

            Level = (parent?.Level ?? -1) + 1;
            Project = project;

            _children = GetChildren(project, getChildProjectsCallback);

            Children = _children.ToCollectionView();
        }

        [NotNull]
        public Project Project { get; }

        [NotNull, ItemNotNull]
        public ICollectionView Children { get; }

        public int Level { get; }

        public bool IsSelected { get; [UsedImplicitly] set; }

        [UsedImplicitly]
        private void OnIsSelectedChanged()
        {
            _model.UpdateSelection(Project, IsSelected);
        }

        public bool IsProjectSelected { get; set; }

        [NotNull, ItemNotNull]
        public IEnumerable<ProjectDependency> DescendantsAndSelf
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<ProjectDependency>>() != null);

                yield return this;

                foreach (var item in _children.SelectMany(p => p.DescendantsAndSelf))
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    yield return item;
                }
            }

        }

        [ContractVerification(false)]
        [NotNull, ItemNotNull]
        private IObservableCollection<ProjectDependency> GetChildren([NotNull] Project project, [NotNull] Func<Project, IList<Project>> getChildProjectsCallback)
        {
            Contract.Requires(project != null);
            Contract.Requires(getChildProjectsCallback != null);
            Contract.Ensures(Contract.Result<IObservableCollection<ProjectDependency>>() != null);

            // ReSharper disable once AssignNullToNotNullAttribute
            return getChildProjectsCallback(project)?.ObservableSelect(p => new ProjectDependency(_model, this, p, getChildProjectsCallback));
        }

        public override string ToString()
        {
            return Project.Name;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(Project != null);
            Contract.Invariant(_model != null);
            Contract.Invariant(_children != null);
        }
    }

    internal static class ExtensionMethods
    {
        [NotNull, ItemNotNull]
        public static ICollectionView ToCollectionView([NotNull, ItemNotNull] this IList<ProjectDependency> items)
        {
            Contract.Requires(items != null);
            Contract.Ensures(Contract.Result<ICollectionView>() != null);

            var view = new ListCollectionView((IList)items);

            // ReSharper disable once PossibleNullReferenceException
            view.SortDescriptions.Add(new SortDescription("Project.Name", ListSortDirection.Ascending));

            return view;
        }
    }
}
