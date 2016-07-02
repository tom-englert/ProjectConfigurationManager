namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Wpf.Composition;

    [DataTemplate(typeof(BuildConfigurationViewModel))]
    public partial class BuildConfigurationView
    {
        private readonly ITracer _tracer;

        [ImportingConstructor]
        public BuildConfigurationView(ITracer tracer)
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
        private void ObjectInvariant()
        {
            Contract.Invariant(_tracer != null);
        }
    }
}
