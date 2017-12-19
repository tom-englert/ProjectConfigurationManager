namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;

    using JetBrains.Annotations;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for FodyView.xaml
    /// </summary>
    [DataTemplate(typeof(FodyViewModel))]
    public partial class FodyView
    {
        [ImportingConstructor]
        public FodyView([NotNull] ExportProvider exportProvider)
        {
            this.SetExportProvider(exportProvider);

            InitializeComponent();
        }
    }
}
