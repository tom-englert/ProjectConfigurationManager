namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.ComponentModel.Composition;
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
            _tracer.TraceError(e.GetException());
        }
    }
}
