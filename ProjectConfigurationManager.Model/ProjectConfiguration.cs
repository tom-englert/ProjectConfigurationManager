namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows.Threading;

    using JetBrains.Annotations;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    public class ProjectConfiguration : ObservableObject, IEquatable<ProjectConfiguration>
    {
        [NotNull]
        private readonly Project _project;
        [NotNull]
        private readonly IIndexer<bool?> _shouldBuild;
        [NotNull]
        private readonly IIndexer<string> _propertyValue;

        [NotNull]
        private IDictionary<string, IProjectProperty> _properties = new Dictionary<string, IProjectProperty>();

        internal ProjectConfiguration([NotNull] Project project, string configuration, string platform)
        {
            Contract.Requires(project != null);

            _project = project;
            Configuration = configuration;
            Platform = platform;

            _shouldBuild = new ShouldBuildIndexer(this);
            _propertyValue = new PropertyValueIndexer(this);
        }

        [NotNull]
        public Project Project
        {
            get
            {
                Contract.Ensures(Contract.Result<Project>() != null);
                return _project;
            }
        }

        public string Configuration { get; }

        public string Platform { get; }

        [NotNull]
        public IIndexer<bool?> ShouldBuild
        {
            get
            {
                Contract.Ensures(Contract.Result<IIndexer<bool?>>() != null);
                return _shouldBuild;
            }
        }

        [NotNull]
        public IIndexer<string> PropertyValue
        {
            get
            {
                Contract.Ensures(Contract.Result<IIndexer<string>>() != null);
                return _propertyValue;
            }
        }

        [NotNull]
        internal IDictionary<string, IProjectProperty> Properties
        {
            get
            {
                Contract.Ensures(Contract.Result<IDictionary<string, IProjectProperty>>() != null);

                return new ReadOnlyDictionary<string, IProjectProperty>(_properties);
            }
        }

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
                .Distinct(new DelegateEqualityComparer<IProjectProperty>(property => property.Name))
                .ToDictionary(property => property.Name);

            _properties = new Dictionary<string, IProjectProperty>(properties);

            OnPropertyChanged(nameof(PropertyValue));
        }

        private class ShouldBuildIndexer : IIndexer<bool?>
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
                        _projectConfiguration.Project.SpecificProjectConfigurations.ForEach(pc => pc.OnPropertyChanged(nameof(ShouldBuild)));
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

        private class PropertyValueIndexer : IIndexer<string>
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
                get
                {
                    return _projectConfiguration.Properties.GetValueOrDefault(propertyName)?.Value;
                }
                set
                {
                    IProjectProperty property;

                    if (!_projectConfiguration.Properties.TryGetValue(propertyName, out property) || (property == null))
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
                OnPropertyChanged(nameof(PropertyValue));
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
            return Project.GetHashCode() + (Configuration?.GetHashCode()).GetValueOrDefault() + (Platform?.GetHashCode()).GetValueOrDefault();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ProjectConfiguration);
        }

        /// <summary>
        /// Determines whether the specified <see cref="ProjectConfiguration"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="ProjectConfiguration"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="ProjectConfiguration"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(ProjectConfiguration other)
        {
            return InternalEquals(this, other);
        }

        [ContractVerification(false)]
        private static bool InternalEquals(ProjectConfiguration left, ProjectConfiguration right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null))
                return false;
            if (ReferenceEquals(right, null))
                return false;

            return (left.Project == right.Project)
                   && (left.Configuration == right.Configuration)
                   && (left.Platform == right.Platform);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(ProjectConfiguration left, ProjectConfiguration right)
        {
            return InternalEquals(left, right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(ProjectConfiguration left, ProjectConfiguration right)
        {
            return !InternalEquals(left, right);
        }

        #endregion

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_project != null);
            Contract.Invariant(_shouldBuild != null);
            Contract.Invariant(_propertyValue != null);
            Contract.Invariant(_properties != null);
        }
    }
}
