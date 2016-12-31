namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;

    using JetBrains.Annotations;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    public class SolutionConfiguration : ObservableObject, IEquatable<SolutionConfiguration>
    {
        [NotNull]
        private readonly Solution _solution;
        [NotNull]
        private readonly EnvDTE80.SolutionConfiguration2 _solutionConfiguration;
        [NotNull]
        private readonly ObservableCollection<SolutionContext> _contexts = new ObservableCollection<SolutionContext>();
        [NotNull]
        private readonly string _name;
        [NotNull]
        private readonly string _platformName;

        internal SolutionConfiguration([NotNull] Solution solution, [NotNull] EnvDTE80.SolutionConfiguration2 solutionConfiguration)
        {
            Contract.Requires(solution != null);
            Contract.Requires(solutionConfiguration != null);
            Contract.Assume(solutionConfiguration.SolutionContexts != null);

            _solution = solution;
            _solutionConfiguration = solutionConfiguration;
            _name = _solutionConfiguration.Name;
            _platformName = _solutionConfiguration.PlatformName;

            Update();
        }

        [NotNull]
        public string Name
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return _name;
            }
        }

        [NotNull]
        public string PlatformName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return _platformName;
            }
        }

        [NotNull]
        public string UniqueName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return Name + "|" + PlatformName;
            }
        }

        [NotNull]
        public ObservableCollection<SolutionContext> Contexts
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<SolutionContext>>() != null);
                return _contexts;
            }
        }

        internal void Update()
        {
            _contexts.SynchronizeWith(_solutionConfiguration
                .SolutionContexts.OfType<EnvDTE.SolutionContext>()
                .Select(ctx => new SolutionContext(_solution, this, ctx))
                .ToArray());
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
            return _solutionConfiguration.GetHashCode();

        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as SolutionConfiguration);
        }

        /// <summary>
        /// Determines whether the specified <see cref="SolutionConfiguration"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="SolutionConfiguration"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="SolutionConfiguration"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(SolutionConfiguration other)
        {
            return InternalEquals(this, other);
        }

        private static bool InternalEquals(SolutionConfiguration left, SolutionConfiguration right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null))
                return false;
            if (ReferenceEquals(right, null))
                return false;

            return left._solutionConfiguration == right._solutionConfiguration;
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(SolutionConfiguration left, SolutionConfiguration right)
        {
            return InternalEquals(left, right);
        }
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(SolutionConfiguration left, SolutionConfiguration right)
        {
            return !InternalEquals(left, right);
        }

        #endregion

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_solution != null);
            Contract.Invariant(_solutionConfiguration != null);
            Contract.Invariant(_name != null);
            Contract.Invariant(_platformName != null);
            Contract.Invariant(_contexts != null);
            Contract.Invariant(Contexts != null);
            Contract.Invariant(_solutionConfiguration.SolutionContexts != null);
        }
    }
}
