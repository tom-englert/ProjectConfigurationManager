namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;

    using Microsoft.VisualStudio.Shell.Interop;

    [Export(typeof(ITracer))]
    public class OutputWindowTracer : ITracer
    {
        private readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        public OutputWindowTracer(IVsServiceProvider serviceProvider)
        {
            Contract.Requires(serviceProvider != null);
            _serviceProvider = serviceProvider;
        }

        private void LogMessageToOutputWindow(string value)
        {
            var outputWindow = _serviceProvider.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outputWindow == null)
                return;

            var outputPaneGuid = new Guid("{4FDC5538-066E-4942-A1FC-15BCB6602D30}");

            IVsOutputWindowPane pane;
            var errorCode = outputWindow.GetPane(ref outputPaneGuid, out pane);

            if ((errorCode < 0) || pane == null)
            {
                outputWindow.CreatePane(ref outputPaneGuid, "Project Configuration Manager", Convert.ToInt32(true), Convert.ToInt32(false));
                outputWindow.GetPane(ref outputPaneGuid, out pane);
            }

            pane?.OutputString(value);
        }

        public void TraceError(string value)
        {
            WriteLine("Error: " + value);
        }

        public void WriteLine(string value)
        {
            LogMessageToOutputWindow(value + Environment.NewLine);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_serviceProvider != null);
        }
    }
}
