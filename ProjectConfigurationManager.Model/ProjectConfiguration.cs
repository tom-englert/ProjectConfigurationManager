namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows.Threading;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    public class ProjectConfiguration : ObservableObject, IEquatable<ProjectConfiguration>
    {
        private readonly string _configuration;
        private readonly string _platform;
        private readonly Project _project;
        private readonly IIndexer<bool?> _shouldBuild;
        private readonly IIndexer<string> _propertyValue;

        private IDictionary<string, IProjectProperty> _properties = new Dictionary<string, IProjectProperty>();

        internal ProjectConfiguration(Project project, string configuration, string platform)
        {
            Contract.Requires(project != null);

            _project = project;
            _configuration = configuration;
            _platform = platform;

            _shouldBuild = new ShouldBuildIndexer(this);
            _propertyValue = new PropertyValueIndexer(this);
        }

        public Project Project
        {
            get
            {
                Contract.Ensures(Contract.Result<Project>() != null);
                return _project;
            }
        }

        public string Configuration => _configuration;

        public string Platform => _platform;

        public IIndexer<bool?> ShouldBuild
        {
            get
            {
                Contract.Ensures(Contract.Result<IIndexer<bool?>>() != null);
                return _shouldBuild;
            }
        }

        public IIndexer<string> PropertyValue
        {
            get
            {
                Contract.Ensures(Contract.Result<IIndexer<string>>() != null);
                return _propertyValue;
            }
        }

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

        internal void SetProjectFile(ProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);

            var properties = projectFile
                .GetPropertyGroups(_configuration, _platform)
                .SelectMany(group => group.Properties)
                .Distinct(new DelegateEqualityComparer<IProjectProperty>(property => property.Name))
                .ToDictionary(property => property.Name);

            _properties = new Dictionary<string, IProjectProperty>(properties);

            OnPropertyChanged(nameof(PropertyValue));
        }

        private void InvalidateShouldBuild()
        {
            OnPropertyChanged(nameof(ShouldBuild));
        }

        private class ShouldBuildIndexer : IIndexer<bool?>
        {
            private readonly ProjectConfiguration _projectConfiguration;

            public ShouldBuildIndexer(ProjectConfiguration projectConfiguration)
            {
                Contract.Requires(projectConfiguration != null);

                _projectConfiguration = projectConfiguration;
            }

            public bool? this[string solutionConfiguration]
            {
                get
                {
                    var context = _projectConfiguration.Project.SolutionContexts
                        .SingleOrDefault(ctx => (ctx.SolutionConfiguration.UniqueName == solutionConfiguration)
                                                && (ctx.ConfigurationName == _projectConfiguration.Configuration)
                                                && (ctx.PlatformName == _projectConfiguration.Platform));

                    return context == null ? false : context.ShouldBuild ? (bool?)true : null;
                }
                set
                {
                    var context = _projectConfiguration.Project.SolutionContexts
                        .FirstOrDefault(ctx => ctx.SolutionConfiguration.UniqueName == solutionConfiguration);

                    Contract.Assume(context != null);

                    var configChanged = context.SetConfiguration(_projectConfiguration);

                    context.ShouldBuild = value.HasValue;

                    if (!configChanged)
                        return;

                    Dispatcher.CurrentDispatcher.BeginInvoke(() =>
                    {
                        _projectConfiguration.Project.SpecificProjectConfigurations.ForEach(pc => pc.InvalidateShouldBuild());
                    });
                }
            }

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_projectConfiguration != null);
            }
        }

        private class PropertyValueIndexer : IIndexer<string>
        {
            private readonly ProjectConfiguration _projectConfiguration;

            public PropertyValueIndexer(ProjectConfiguration projectConfiguration)
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
                        throw new ArgumentException("Unable to create property: " + propertyName, nameof(propertyName));

                    property.Value = value ?? string.Empty;
                }
            }

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_projectConfiguration != null);
            }
        }

        private IProjectProperty CreateProperty(string propertyName)
        {
            Contract.Requires(propertyName != null);

            var property = Project.CreateProperty(propertyName, _configuration, _platform);

            _properties.Add(propertyName, property);

            return property;
        }

        public void DeleteProperty(string propertyName)
        {
            Contract.Requires(propertyName != null);

            Project.DeleteProperty(propertyName, _configuration, _platform);

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
            return Project.GetHashCode() + (_configuration?.GetHashCode()).GetValueOrDefault() + (_platform?.GetHashCode()).GetValueOrDefault();
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
                   && (left._configuration == right._configuration)
                   && (left._platform == right._platform);
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
        private void ObjectInvariant()
        {
            Contract.Invariant(_project != null);
            Contract.Invariant(_shouldBuild != null);
            Contract.Invariant(_propertyValue != null);
            Contract.Invariant(_properties != null);
        }
    }
}
