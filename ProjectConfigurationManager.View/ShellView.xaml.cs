namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for ShellView.xaml
    /// </summary>
    [Export]
    public partial class ShellView
    {
        [ImportingConstructor]
        public ShellView(ExportProvider exportProvider)
        {
            this.SetExportProvider(exportProvider);

            InitializeComponent();
        }
    }
}
