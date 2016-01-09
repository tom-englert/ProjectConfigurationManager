namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Threading;
    using System.Xml.Linq;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    public class ProjectConfiguration : ObservableObject, IEquatable<ProjectConfiguration>
    {
        private readonly string _configuration;
        private readonly string _platform;
        private readonly Project _project;
        private readonly IDictionary<string, XElement> _propertyNodes;

        public ProjectConfiguration(Project project, IEnumerable<XElement> propertyGroupNodes, string configuration, string platform)
        {
            _project = project;

            var propertyElements = propertyGroupNodes
                .SelectMany(group => group.Elements())
                .Where(node => node.GetAttribute("Condition") == null)
                .ToArray();

            _propertyNodes = propertyElements
                .ToDictionary(node => node.Name.LocalName);

            _configuration = configuration;
            _platform = platform;


            ShouldBuild = new ShouldBuildIndexer(this);
            PropertyValue = new PropertyIndexer(this);
        }

        public Project Project => _project;

        public string Configuration => _configuration;

        public string Platform => _platform;

        public ShouldBuildIndexer ShouldBuild
        {
            get;
        }

        public PropertyIndexer PropertyValue
        {
            get;
        }

        public string GetPropertyValue(string propertyName)
        {
            return _propertyNodes.GetValueOrDefault(propertyName)?.Value;
        }

        internal void Invalidate()
        {
            OnPropertyChanged(() => ShouldBuild);
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

    public class ShouldBuildIndexer
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
                    _projectConfiguration.Project.SpecificProjectConfigurations.ForEach(pc => pc.Invalidate());
                });
            }
        }
    }

    public class PropertyIndexer
    {
        private readonly ProjectConfiguration _projectConfiguration;

        public PropertyIndexer(ProjectConfiguration projectConfiguration)
        {
            _projectConfiguration = projectConfiguration;
        }

        public string this[string propertyName]
        {
            get
            {
                return _projectConfiguration.GetPropertyValue(propertyName);
            }
            set
            {
            }
        }
    }
}
