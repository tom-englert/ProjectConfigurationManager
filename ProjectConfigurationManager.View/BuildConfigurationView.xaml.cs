namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Windows.Markup;

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
            Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
        }

        private void ConfirmedCommandConverter_Error([NotNull] object sender, [NotNull] ErrorEventArgs e)
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
