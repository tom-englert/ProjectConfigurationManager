namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
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
        private IDictionary<string, IProjectProperty> _properties = new Dictionary<string, IProjectProperty>();

        internal ProjectConfiguration(Project project, string configuration, string platform)
        {
            Contract.Requires(project != null);

            _project = project;
            _configuration = configuration;
            _platform = platform;

            ShouldBuild = new ShouldBuildIndexer(this);
            PropertyValue = new PropertyValueIndexer(this);
        }

        public Project Project => _project;

        public string Configuration => _configuration;

        public string Platform => _platform;

        public IIndexer<bool?> ShouldBuild { get; }

        public IIndexer<string> PropertyValue { get; }

        internal void SetProjectFile(ProjectFile projectFile)
        {
            var properties = projectFile
                .GetPropertyGroups(_configuration, _platform)
                .SelectMany(group => group.Properties)
                .Distinct(new DelegateEqualityComparer<IProjectProperty>(property => property.Name))
                .ToDictionary(property => property.Name);

            _properties = new Dictionary<string, IProjectProperty>(properties);

            OnPropertyChanged(nameof(PropertyValue));
        }

        internal IEnumerable<IProjectProperty> Properties => _properties.Values;

        private void InvalidateShouldBuild()
        {
            OnPropertyChanged(nameof(ShouldBuild));
        }

        private class ShouldBuildIndexer : IIndexer<bool?>
        {
            private readonly ProjectConfiguration _projectConfiguration;

            public ShouldBuildIndexer(ProjectConfiguration projectConfiguration)
            {
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
                        .Single(ctx => (ctx.SolutionConfiguration.UniqueName == solutionConfiguration));

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
        }

        private class PropertyValueIndexer : IIndexer<string>
        {
            private readonly ProjectConfiguration _projectConfiguration;

            public PropertyValueIndexer(ProjectConfiguration projectConfiguration)
            {
                _projectConfiguration = projectConfiguration;
            }

            public string this[string propertyName]
            {
                get
                {
                    return _projectConfiguration._properties.GetValueOrDefault(propertyName)?.Value;
                }
                set
                {
                    var property = _projectConfiguration._properties.ForceValue(propertyName, _projectConfiguration.CreateProperty);

                    if (property == null)
                        throw new ArgumentException("Unable to create property: " + propertyName, nameof(propertyName));

                    property.Value = value;
                }
            }
        }

        private IProjectProperty CreateProperty(string propertyName)
        {
            return _project.CreateProperty(propertyName, _configuration, _platform);
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
            return _project.GetHashCode() + (_configuration?.GetHashCode()).GetValueOrDefault() + (_platform?.GetHashCode()).GetValueOrDefault();
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

        private static bool InternalEquals(ProjectConfiguration left, ProjectConfiguration right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null))
                return false;
            if (ReferenceEquals(right, null))
                return false;

            return (left._project == right._project)
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
    }
}
