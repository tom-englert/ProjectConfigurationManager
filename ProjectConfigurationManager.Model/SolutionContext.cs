namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Diagnostics.Contracts;

    using TomsToolbox.Desktop;

    public class SolutionContext : ObservableObject, IEquatable<SolutionContext>
    {
        private readonly Solution _solution;
        private readonly SolutionConfiguration _solutionConfiguration;
        private readonly EnvDTE.SolutionContext _ctx;
        private string _configurationName;
        private string _platformName;

        public SolutionContext(Solution solution, SolutionConfiguration solutionConfiguration, EnvDTE.SolutionContext ctx)
        {
            Contract.Requires(solution != null);
            Contract.Requires(solutionConfiguration != null);
            Contract.Requires(ctx != null);

            _solution = solution;
            _solutionConfiguration = solutionConfiguration;
            _ctx = ctx;

            _configurationName = ctx.ConfigurationName;
            _platformName = ctx.PlatformName;

            ProjectName = ctx.ProjectName;
        }

        public string ConfigurationName
        {
            get
            {
                return _configurationName;
            }
            set
            {
                SetProperty(ref _configurationName, value);
            }
        }

        public string PlatformName
        {
            get
            {
                return _platformName;
            }
            set
            {
                SetProperty(ref _platformName, value);
            }
        }

        public bool SetConfiguration(ProjectConfiguration configuration)
        {
            Contract.Requires(configuration != null);

            if ((ConfigurationName == configuration.Configuration) && (PlatformName == configuration.Platform))
                return false;

            if (!ContextIsValid())
                return false;

            _ctx.ConfigurationName = configuration.Configuration + "|" + configuration.Platform;

            ConfigurationName = _ctx.ConfigurationName;
            PlatformName = _ctx.PlatformName;

            return true;
        }

        public string ProjectName
        {
            get;
        }

        public bool ShouldBuild
        {
            get
            {
                return ContextIsValid() && _ctx.ShouldBuild;
            }
            set
            {
                if (!ContextIsValid())
                    return;

                _ctx.ShouldBuild = value;

                OnPropertyChanged();
            }
        }

        public SolutionConfiguration SolutionConfiguration
        {
            get
            {
                Contract.Ensures(Contract.Result<SolutionConfiguration>() != null);
                return _solutionConfiguration;
            }
        }

        private bool ContextIsValid()
        {
            // Check if the owning collection is valid - accessing other properites would throw an AccessViolationException!
            if (_ctx.Collection?.Count != 0)
                return true;

            // This context is no longer valid, schedule a solution update and return false...
            Dispatcher.BeginInvoke(() => _solution.Update());
            return false;
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

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_solution != null);
            Contract.Invariant(_solutionConfiguration != null);
            Contract.Invariant(_ctx != null);
        }
    }
}
