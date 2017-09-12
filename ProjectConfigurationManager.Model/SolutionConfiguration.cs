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

        internal SolutionConfiguration([NotNull] Solution solution, [NotNull] EnvDTE80.SolutionConfiguration2 solutionConfiguration)
        {
            Contract.Requires(solution != null);
            Contract.Requires(solutionConfiguration != null);
            Contract.Assume(solutionConfiguration.SolutionContexts != null);

            _solution = solution;
            _solutionConfiguration = solutionConfiguration;

            // ReSharper disable AssignNullToNotNullAttribute
            Name = _solutionConfiguration.Name;
            PlatformName = _solutionConfiguration.PlatformName;
            // ReSharper restore AssignNullToNotNullAttribute

            Update();
        }

        [NotNull, UsedImplicitly]
        public string Name { get; }

        [NotNull, UsedImplicitly]
        public string PlatformName { get; }

        [NotNull]
        public string UniqueName => Name + "|" + PlatformName;

        [NotNull, ItemNotNull]
        public ObservableCollection<SolutionContext> Contexts { get; } = new ObservableCollection<SolutionContext>();

        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        private void Update()
        {
            Contexts.SynchronizeWith(_solutionConfiguration
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
            return Contexts.Select(ctx => ctx.GetHashCode()).Aggregate(_solutionConfiguration.GetHashCode(), HashCode.Aggregate);
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

        [ContractVerification(false)]
        private static bool InternalEquals([CanBeNull] SolutionConfiguration left, [CanBeNull] SolutionConfiguration right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null))
                return false;
            if (ReferenceEquals(right, null))
                return false;

            return left._solutionConfiguration == right._solutionConfiguration
                && left.Contexts.SequenceEqual(right.Contexts);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==([CanBeNull] SolutionConfiguration left, [CanBeNull] SolutionConfiguration right)
        {
            return InternalEquals(left, right);
        }
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=([CanBeNull] SolutionConfiguration left, [CanBeNull] SolutionConfiguration right)
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
            Contract.Invariant(Name != null);
            Contract.Invariant(PlatformName != null);
            Contract.Invariant(Contexts != null);
            Contract.Invariant(Contexts != null);
            Contract.Invariant(_solutionConfiguration.SolutionContexts != null);
        }
    }
}
