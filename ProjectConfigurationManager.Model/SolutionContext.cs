namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    using TomsToolbox.Desktop;

    public class SolutionContext : ObservableObject, IEquatable<SolutionContext>
    {
        [NotNull]
        private readonly Solution _solution;
        [NotNull]
        private readonly EnvDTE.SolutionContext _context;

        public SolutionContext([NotNull] Solution solution, [NotNull] SolutionConfiguration solutionConfiguration, [NotNull] EnvDTE.SolutionContext context)
        {
            Contract.Requires(solution != null);
            Contract.Requires(solutionConfiguration != null);
            Contract.Requires(context != null);

            _solution = solution;
            SolutionConfiguration = solutionConfiguration;
            _context = context;

            ConfigurationName = context.ConfigurationName;
            PlatformName = context.PlatformName;
            ProjectName = context.ProjectName;
        }

        [CanBeNull]
        public string ConfigurationName { get; set; }

        [CanBeNull]
        public string PlatformName { get; set; }

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

        public string ProjectName { get; }

        public bool ShouldBuild
        {
            get => ContextIsValid() && _context.ShouldBuild;
            set
            {
                if (!ContextIsValid())
                    return;

                _context.ShouldBuild = value;
            }
        }

        [NotNull]
        public SolutionConfiguration SolutionConfiguration { get; }

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
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_solution != null);
            Contract.Invariant(SolutionConfiguration != null);
            Contract.Invariant(_context != null);
        }
    }
}
