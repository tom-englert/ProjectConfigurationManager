namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Linq;
    using System.Windows.Threading;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    public class ProjectConfiguration : ObservableObject, IEquatable<ProjectConfiguration>
    {
        private readonly string _configuration;
        private readonly string _platform;
        private readonly Project _project;

        public ProjectConfiguration(Project project, string configuration, string platform)
        {
            _project = project;
            _configuration = configuration;
            _platform = platform;

            ShouldBuild = new ShouldBuildSelector(this);
        }

        public Project Project => _project;

        public string Configuration => _configuration;

        public string Platform => _platform;

        public ShouldBuildSelector ShouldBuild
        {
            get;
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
            return _project.GetHashCode() + _configuration.GetHashCode() + _platform.GetHashCode();
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

    public class ShouldBuildSelector
    {
        private readonly ProjectConfiguration _projectConfiguration;

        public ShouldBuildSelector(ProjectConfiguration projectConfiguration)
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
                    _projectConfiguration.Project.ProjectConfigurations.ForEach(pc => pc.Invalidate());
                });
            }
        }
    }
}
