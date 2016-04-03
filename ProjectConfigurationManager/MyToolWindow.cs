namespace tomenglertde.ProjectConfigurationManager
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.ComponentModel.Composition.Registration;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Windows.Documents;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using tomenglertde.ProjectConfigurationManager.Model;
    using tomenglertde.ProjectConfigurationManager.View;

    using TomsToolbox.Desktop.Composition;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    ///
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane, 
    /// usually implemented by the package implementer.
    ///
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its 
    /// implementation of the IVsUIElementPane interface.
    /// </summary>
    [Guid("01a9a1a2-ea6f-4cb6-ae33-996b06435a62")]
    public class MyToolWindow : ToolWindowPane, IVsServiceProvider
    {
        private readonly ICompositionHost _compositionHost = new CompositionHost();
        private readonly ITracer _tracer;

        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public MyToolWindow() :
            base(null)
        {
            Caption = Resources.ToolWindowTitle;

            BitmapResourceID = 301;
            BitmapIndex = 1;

            var path = Path.GetDirectoryName(GetType().Assembly.Location);

            var context = CreateRegistrationContext();

            _compositionHost.AddCatalog(new DirectoryCatalog(path, "*.dll", context));
            _compositionHost.ComposeExportedValue((IVsServiceProvider)this);

            _tracer = _compositionHost.GetExportedValue<ITracer>();
        }

        protected override void OnCreate()
        {
            try
            {
                base.OnCreate();

                var view = _compositionHost.GetExportedValue<ShellView>();
                view.Resources.MergedDictionaries.Add(DataTemplateManager.CreateDynamicDataTemplates(_compositionHost.Container));

                view.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(Navigate_Click));

                Content = view;
            }
            catch (Exception ex)
            {
                _tracer.TraceError(ex);
            }
        }

        protected override void OnClose()
        {
            base.OnClose();

            _compositionHost.Dispose();
        }

        private void Navigate_Click(object sender, RoutedEventArgs e)
        {
            string url = null;

            var source = e.OriginalSource as FrameworkElement;
            if (source != null)
            {
                var button = source.TryFindAncestorOrSelf<ButtonBase>();
                if (button == null)
                    return;

                url = source.Tag as string;
                if (string.IsNullOrEmpty(url) || !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    return;
            }
            else
            {
                var link = e.OriginalSource as Hyperlink;

                var navigateUri = link?.NavigateUri;
                if (navigateUri == null)
                    return;

                url = navigateUri.ToString();
            }

            CreateWebBrowser(url);
        }

        [Localizable(false)]
        private void CreateWebBrowser(string url)
        {
            Contract.Requires(url != null);

            var webBrowsingService = (IVsWebBrowsingService)GetService(typeof(SVsWebBrowsingService));
            if (webBrowsingService != null)
            {
                IVsWindowFrame pFrame;
                var hr = webBrowsingService.Navigate(url, (uint)__VSWBNAVIGATEFLAGS.VSNWB_WebURLOnly, out pFrame);
                if (ErrorHandler.Succeeded(hr) && (pFrame != null))
                {
                    hr = pFrame.Show();
                    if (ErrorHandler.Succeeded(hr))
                        return;
                }
            }

            Process.Start(url);
        }

        [ContractVerification(false)]
        private static RegistrationBuilder CreateRegistrationContext()
        {
            Contract.Ensures(Contract.Result<RegistrationBuilder>() != null);

            var context = new RegistrationBuilder();
            context.ForTypesDerivedFrom<FrameworkElement>().SetCreationPolicy(CreationPolicy.NonShared);
            return context;
        }
    }
}
