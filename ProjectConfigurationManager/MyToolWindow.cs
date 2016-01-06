namespace tomenglertde.ProjectConfigurationManager
{
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.ComponentModel.Composition.Registration;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows;

    using Microsoft.VisualStudio.Shell;

    using tomenglertde.ProjectConfigurationManager.Model;
    using tomenglertde.ProjectConfigurationManager.View;

    using TomsToolbox.Desktop.Composition;
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
            Contract.Assume(path != null);

            var context = new RegistrationBuilder();
            context.ForTypesDerivedFrom<FrameworkElement>().SetCreationPolicy(CreationPolicy.NonShared);

            _compositionHost.AddCatalog(new DirectoryCatalog(path, "ProjectConfigurationManager*.dll", context));
            _compositionHost.ComposeExportedValue((IVsServiceProvider)this);
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            var view = _compositionHost.GetExportedValue<ShellView>();
            view.Resources.MergedDictionaries.Add(DataTemplateManager.CreateDynamicDataTemplates(_compositionHost.Container));

            Content = view;
        }

        protected override void OnClose()
        {
            base.OnClose();

            _compositionHost.Dispose();
        }
    }
}
