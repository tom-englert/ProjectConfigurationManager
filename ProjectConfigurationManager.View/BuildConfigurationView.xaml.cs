namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Windows;
    using System.Windows.Controls;

    using tomenglertde.ProjectConfigurationManager.Model;

    using TomsToolbox.Wpf.Composition;

    [DataTemplate(typeof(BuildConfigurationViewModel))]
    public partial class BuildConfigurationView
    {
        public BuildConfigurationView()
        {
            InitializeComponent();
        }

        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            SolutionConfigurationColumnsManager.Register((DataGrid)sender, this.GetExportProvider().GetExportedValue<Solution>());
        }
    }
}
