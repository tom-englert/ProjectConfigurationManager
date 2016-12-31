using System.Diagnostics;

namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;

    using JetBrains.Annotations;

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Wpf.Composition;

    [DataTemplate(typeof(BuildConfigurationViewModel))]
    public partial class BuildConfigurationView
    {
        [NotNull]
        private readonly ITracer _tracer;

        [ImportingConstructor]
        public BuildConfigurationView([NotNull] ITracer tracer)
        {
            Contract.Requires(tracer != null);
            _tracer = tracer;

            InitializeComponent();
        }

        private void ConfirmedCommandConverter_Error(object sender, ErrorEventArgs e)
        {
            var exception = e.GetException();
            if (exception == null)
                return;

            _tracer.TraceError(exception);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_tracer != null);
        }
    }
}
