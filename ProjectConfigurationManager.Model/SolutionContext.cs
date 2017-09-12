namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Runtime.CompilerServices;

    using JetBrains.Annotations;

    [Equals]
    public sealed class SolutionContext : INotifyPropertyChanged
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

        [CanBeNull, IgnoreDuringEquals]
        public string ConfigurationName { get; private set; }

        [CanBeNull, IgnoreDuringEquals]
        public string PlatformName { get; private set; }

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

        [CanBeNull, IgnoreDuringEquals]
        public string ProjectName { get; }

        [IgnoreDuringEquals]
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

        [NotNull, IgnoreDuringEquals]
        public SolutionConfiguration SolutionConfiguration { get; }

        private bool ContextIsValid()
        {
            // Check if the owning collection is valid - accessing other properites would throw an AccessViolationException!
            if (_context.Collection?.Count != 0)
                return true;

            // This context is no longer valid, schedule a solution update and return false...
            _solution.Update();
            return false;
        }

        [CustomGetHashCode, UsedImplicitly]
        private int CustomGetHashCode()
        {
            return _context.GetHashCode();
        }

        [CustomEqualsInternal, UsedImplicitly]
        private bool CustomEquals([NotNull] SolutionContext other)
        {
            return _context == other._context;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator, UsedImplicitly]
        private void OnPropertyChanged([CallerMemberName, CanBeNull] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
