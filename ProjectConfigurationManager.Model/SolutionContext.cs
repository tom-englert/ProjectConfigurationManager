namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    using TomsToolbox.Desktop;

    public class SolutionContext : ObservableObject, IEquatable<SolutionContext>
    {
        [NotNull]
        private readonly Solution _solution;
        [NotNull]
        private readonly SolutionConfiguration _solutionConfiguration;
        [NotNull]
        private readonly EnvDTE.SolutionContext _context;
        private string _configurationName;
        private string _platformName;

        public SolutionContext([NotNull] Solution solution, [NotNull] SolutionConfiguration solutionConfiguration, [NotNull] EnvDTE.SolutionContext context)
        {
            Contract.Requires(solution != null);
            Contract.Requires(solutionConfiguration != null);
            Contract.Requires(context != null);

            _solution = solution;
            _solutionConfiguration = solutionConfiguration;
            _context = context;

            _configurationName = context.ConfigurationName;
            _platformName = context.PlatformName;

            ProjectName = context.ProjectName;
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

        public bool SetConfiguration([NotNull] ProjectConfiguration configuration)
        {
            Contract.Requires(configuration != null);

            if ((ConfigurationName == configuration.Configuration) && (PlatformName == configuration.Platform))
                return false;

            if (!ContextIsValid())
                return false;

            _context.ConfigurationName = configuration.Configuration + "|" + configuration.Platform;

            ConfigurationName = _context.ConfigurationName;
            PlatformName = _context.PlatformName;

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
                return ContextIsValid() && _context.ShouldBuild;
            }
            set
            {
                if (!ContextIsValid())
                    return;

                _context.ShouldBuild = value;

                OnPropertyChanged();
            }
        }

        [NotNull]
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
            if (_context.Collection?.Count != 0)
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
            return _context.GetHashCode();
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

            return left._context == right._context;
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
            Contract.Invariant(_context != null);
        }
    }
}
