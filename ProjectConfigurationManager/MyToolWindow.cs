namespace tomenglertde.ProjectConfigurationManager
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.ComponentModel.Composition.Registration;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Windows.Documents;

    using JetBrains.Annotations;

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
    public sealed class MyToolWindow : ToolWindowPane, IVsServiceProvider
    {
        private const string _introMessage = "Project Configuration Manager loaded."
            + "\nHome: https://github.com/tom-englert/ProjectConfigurationManager"
            + "\nReport issues: https://github.com/tom-englert/ProjectConfigurationManager/issues"
            + "\nSupport the project by adding a short review: https://marketplace.visualstudio.com/vsgallery/cf7efe17-ae87-40fe-a1e2-f2d61907f043#review-details";

        [NotNull]
        private readonly ICompositionHost _compositionHost = new CompositionHost();
        [NotNull]
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

            // ReSharper disable once AssignNullToNotNullAttribute
            _compositionHost.AddCatalog(new DirectoryCatalog(path, "ProjectConfigurationManager*.dll", context));
            _compositionHost.ComposeExportedValue((IVsServiceProvider)this);

            _tracer = _compositionHost.GetExportedValue<ITracer>();
        }

        protected override void OnCreate()
        {
            try
            {
                base.OnCreate();

                _tracer.WriteLine(_introMessage);

                var executingAssembly = Assembly.GetExecutingAssembly();
                var folder = Path.GetDirectoryName(executingAssembly.Location);

                _tracer.WriteLine(string.Format(CultureInfo.InvariantCulture, "Assembly location: {0}", folder));
                _tracer.WriteLine(string.Format(CultureInfo.InvariantCulture, "Version: {0}", new AssemblyName(executingAssembly.FullName).Version));


                var view = _compositionHost.GetExportedValue<ShellView>();
                view.Resources.MergedDictionaries.Add(DataTemplateManager.CreateDynamicDataTemplates(_compositionHost.Container));

                // ReSharper disable once AssignNullToNotNullAttribute
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

        private void Navigate_Click([NotNull] object sender, [NotNull] RoutedEventArgs e)
        {
            string url;

            if (e.OriginalSource is FrameworkElement source)
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
        private void CreateWebBrowser([NotNull] string url)
        {
            Contract.Requires(url != null);

            var webBrowsingService = (IVsWebBrowsingService)GetService(typeof(SVsWebBrowsingService));
            if (webBrowsingService != null)
            {
                var hr = webBrowsingService.Navigate(url, (uint)__VSWBNAVIGATEFLAGS.VSNWB_WebURLOnly, out var pFrame);
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
        [NotNull]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private static RegistrationBuilder CreateRegistrationContext()
        {
            Contract.Ensures(Contract.Result<RegistrationBuilder>() != null);

            var context = new RegistrationBuilder();
            context.ForTypesDerivedFrom<FrameworkElement>().SetCreationPolicy(CreationPolicy.NonShared);
            context.ForTypesDerivedFrom<object>().SelectConstructor(SelectConstructor);

            return context;
        }

        [CanBeNull]
        private static ConstructorInfo SelectConstructor([NotNull, ItemNotNull] ConstructorInfo[] constructors)
        {
            Contract.Requires(constructors != null);

            return constructors.SingleOrDefault(c => c.GetCustomAttributes<ImportingConstructorAttribute>().Any())
                   ?? constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_compositionHost != null);
            Contract.Invariant(_tracer != null);
        }
    }
}
