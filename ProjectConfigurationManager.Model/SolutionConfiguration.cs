namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    public class SolutionConfiguration : ObservableObject, IEquatable<SolutionConfiguration>
    {
        private readonly Solution _solution;
        private readonly EnvDTE80.SolutionConfiguration2 _solutionConfiguration;
        private readonly ObservableCollection<SolutionContext> _contexts = new ObservableCollection<SolutionContext>();

        internal SolutionConfiguration(Solution solution, EnvDTE80.SolutionConfiguration2 solutionConfiguration)
        {
            Contract.Requires(solution != null);
            Contract.Requires(solutionConfiguration != null);

            _solution = solution;
            _solutionConfiguration = solutionConfiguration;

            Update();
        }

        public string Name => _solutionConfiguration.Name;

        public string PlatformName => _solutionConfiguration.PlatformName;

        public string UniqueName => Name + "|" + PlatformName;

        public ObservableCollection<SolutionContext> Contexts => _contexts;

        internal void Update()
        {
            _contexts.SynchronizeWith(_solutionConfiguration.SolutionContexts
                .OfType<EnvDTE.SolutionContext>()
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
        private void ObjectInvariant()
        {
            Contract.Invariant(_solution != null);
            Contract.Invariant(_solutionConfiguration != null);
            Contract.Invariant(Contexts != null);
        }
    }
}
