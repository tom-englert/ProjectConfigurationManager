namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Linq;

    using TomsToolbox.Desktop;

    public class SolutionContext : ObservableObject, IEquatable<SolutionContext>
    {
        private readonly Solution _solution;
        private readonly SolutionConfiguration _solutionConfiguration;
        private readonly EnvDTE.SolutionContext _ctx;

        public SolutionContext(Solution solution, SolutionConfiguration solutionConfiguration, EnvDTE.SolutionContext ctx)
        {
            _solution = solution;
            _solutionConfiguration = solutionConfiguration;
            _ctx = ctx;
        }

        public string ConfigurationName => _ctx.ConfigurationName;

        public string PlatformName => _ctx.PlatformName;

        public bool SetConfiguration(ProjectConfiguration configuration)
        {
            if ((_ctx.ConfigurationName == configuration.Configuration) && (_ctx.PlatformName == configuration.Platform))
                return false;

            _ctx.ConfigurationName = configuration.Configuration + "|" + configuration.Platform;

            OnPropertyChanged(() => ConfigurationName);
            OnPropertyChanged(() => PlatformName);

            return true;
        }

        public string ProjectName => _ctx.ProjectName;

        public bool ShouldBuild
        {
            get
            {
                return _ctx.ShouldBuild;
            }
            set
            {
                _ctx.ShouldBuild = value;
                OnPropertyChanged();
            }
        }

        public SolutionConfiguration SolutionConfiguration => _solutionConfiguration;

        public Project Project => _solution.Projects.FirstOrDefault(p => p.UniqueName == _ctx.ProjectName);

        #region IEquatable implementation

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return _ctx.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as SolutionContext);
        }

        /// <summary>
        /// Determines whether the specified <see cref="SolutionContext"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="SolutionContext"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="SolutionContext"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(SolutionContext other)
        {
            return InternalEquals(this, other);
        }

        private static bool InternalEquals(SolutionContext left, SolutionContext right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null))
                return false;
            if (ReferenceEquals(right, null))
                return false;

            return left._ctx == right._ctx;
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(SolutionContext left, SolutionContext right)
        {
            return InternalEquals(left, right);
        }
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(SolutionContext left, SolutionContext right)
        {
            return !InternalEquals(left, right);
        }

        #endregion
    }
}
