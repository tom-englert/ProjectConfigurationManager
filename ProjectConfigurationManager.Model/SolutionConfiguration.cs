namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;

    using Equatable;

    using JetBrains.Annotations;

    using TomsToolbox.Core;

    [ImplementsEquatable]
    public sealed class SolutionConfiguration : INotifyPropertyChanged
    {
        [NotNull, Equals]
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
            Contexts.SynchronizeWith(_solutionConfiguration.SolutionContexts
                .OfType<EnvDTE.SolutionContext>()
                .Select(ctx => new SolutionContext(_solution, this, ctx))
                .ToArray());
        }

        [CustomGetHashCode, UsedImplicitly]
        private int CustomGetHashCode()
        {
            return Contexts.Select(ctx => ctx.GetHashCode()).Aggregate(HashCode.Aggregate);
        }

        [CustomEquals, UsedImplicitly]
        [ContractVerification(false)]
        private bool CustomEquals([NotNull] SolutionConfiguration other)
        {
            return Contexts.SequenceEqual(other.Contexts);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [UsedImplicitly]
        private void OnPropertyChanged([NotNull] string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
