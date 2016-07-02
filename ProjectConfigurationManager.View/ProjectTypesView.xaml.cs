namespace tomenglertde.ProjectConfigurationManager.View
{
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for ProjectTypesView.xaml
    /// </summary>
    [DataTemplate(typeof(ProjectTypesViewModel))]
    public partial class ProjectTypesView
    {
        public ProjectTypesView()
        {
            InitializeComponent();
        }
    }
}
