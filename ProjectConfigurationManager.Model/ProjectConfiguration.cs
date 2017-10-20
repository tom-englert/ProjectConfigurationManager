namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows.Threading;

    using Equatable;

    using JetBrains.Annotations;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    [ImplementsEquatable]
    public sealed class ProjectConfiguration : INotifyPropertyChanged
    {
        [NotNull]
        private IDictionary<string, IProjectProperty> _properties = new Dictionary<string, IProjectProperty>();

        internal ProjectConfiguration([NotNull] Project project, [CanBeNull] string configuration, [CanBeNull] string platform)
        {
            Contract.Requires(project != null);

            Project = project;
            Configuration = configuration;
            Platform = platform;

            ShouldBuild = new ShouldBuildIndexer(this);
            PropertyValue = new PropertyValueIndexer(this);
        }

        [NotNull, Equals]
        public Project Project { get; }

        [CanBeNull, Equals]
        public string Configuration { get; }

        [CanBeNull, Equals]
        public string Platform { get; }

        [NotNull] // ReSharper disable once MemberCanBePrivate.Global - used in column binding
        public IIndexer<bool?> ShouldBuild { get; }

        [NotNull]
        public IIndexer<string> PropertyValue { get; }

        [NotNull]
        internal IDictionary<string, IProjectProperty> Properties => new ReadOnlyDictionary<string, IProjectProperty>(_properties);

        public void Delete()
        {
            Project.Delete(this);
        }

        public bool ShouldBuildInAnyConfiguration()
        {
            return Project.SolutionContexts.Any(ctx => (ctx.ConfigurationName == Configuration) && (ctx.PlatformName == Platform));
        }

        internal void SetProjectFile([NotNull] ProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);

            var properties = projectFile
                .GetPropertyGroups(Configuration, Platform)
                .SelectMany(group => group.Properties)
                // ReSharper disable once PossibleNullReferenceException
                .Distinct(new DelegateEqualityComparer<IProjectProperty>(property => property.Name))
                // ReSharper disable once PossibleNullReferenceException
                .ToDictionary(property => property.Name);

            _properties = new Dictionary<string, IProjectProperty>(properties);

            OnPropertyChanged(nameof(PropertyValue));
        }

        private sealed class ShouldBuildIndexer : IIndexer<bool?>
        {
            [NotNull]
            private readonly ProjectConfiguration _projectConfiguration;

            public ShouldBuildIndexer([NotNull] ProjectConfiguration projectConfiguration)
            {
                Contract.Requires(projectConfiguration != null);

                _projectConfiguration = projectConfiguration;
            }

            public bool? this[string solutionConfiguration]
            {
                get
                {
                    var projectSolutionContexts = _projectConfiguration.Project.SolutionContexts;

                    var context = projectSolutionContexts
                        .SingleOrDefault(ctx => (ctx.SolutionConfiguration.UniqueName == solutionConfiguration)
                                                && (ctx.ConfigurationName == _projectConfiguration.Configuration)
                                                && (ctx.PlatformName == _projectConfiguration.Platform));

                    return context == null ? false : context.ShouldBuild ? (bool?)true : null;
                }
                set
                {
                    var projectSolutionContexts = _projectConfiguration.Project.SolutionContexts;

                    var context = projectSolutionContexts
                        .FirstOrDefault(ctx => ctx.SolutionConfiguration.UniqueName == solutionConfiguration);

                    Contract.Assume(context != null);

                    var configChanged = context.SetConfiguration(_projectConfiguration);

                    context.ShouldBuild = value.HasValue;

                    if (!configChanged)
                        return;

                    Dispatcher.CurrentDispatcher.BeginInvoke(() =>
                    {
                        foreach (var configuration in _projectConfiguration.Project.SpecificProjectConfigurations)
                        {
                            configuration.OnPropertyChanged(nameof(ShouldBuild));
                        }
                    });
                }
            }

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            [Conditional("CONTRACTS_FULL")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_projectConfiguration != null);
            }
        }

        private sealed class PropertyValueIndexer : IIndexer<string>
        {
            [NotNull]
            private readonly ProjectConfiguration _projectConfiguration;

            public PropertyValueIndexer([NotNull] ProjectConfiguration projectConfiguration)
            {
                Contract.Requires(projectConfiguration != null);

                _projectConfiguration = projectConfiguration;
            }

            public string this[string propertyName]
            {
                get => _projectConfiguration.Properties.GetValueOrDefault(propertyName)?.Value;
                set
                {
                    if (!_projectConfiguration.Properties.TryGetValue(propertyName, out var property) || (property == null))
                    {
                        if (string.IsNullOrEmpty(value)) // do not create empty entries.
                            return;

                        property = _projectConfiguration.CreateProperty(propertyName);
                    }

                    if (property == null)
                        throw new ArgumentException(@"Unable to create property: " + propertyName, nameof(propertyName));

                    property.Value = value ?? string.Empty;

                    // Defer property change notifications, else bulk operations on data grid will fail...
                    Dispatcher.CurrentDispatcher.BeginInvoke(() => _projectConfiguration.OnPropertyChanged(nameof(PropertyValue)));
                }
            }

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            [Conditional("CONTRACTS_FULL")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_projectConfiguration != null);
            }
        }

        [CanBeNull]
        private IProjectProperty CreateProperty([NotNull] string propertyName)
        {
            Contract.Requires(propertyName != null);

            var property = Project.CreateProperty(propertyName, Configuration, Platform);

            _properties.Add(propertyName, property);

            return property;
        }

        public void DeleteProperty([NotNull] string propertyName)
        {
            Contract.Requires(propertyName != null);

            Project.DeleteProperty(propertyName, Configuration, Platform);

            if (_properties.Remove(propertyName))
            {
                OnPropertyChanged(nameof(PropertyValue));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([NotNull] string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(Project != null);
            Contract.Invariant(ShouldBuild != null);
            Contract.Invariant(PropertyValue != null);
            Contract.Invariant(_properties != null);
        }
    }
}
